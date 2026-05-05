using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ISeatLockRepository _seatLockRepo;
    private readonly ITripRepository _tripRepo;

    public BookingService(IBookingRepository bookingRepo, ISeatLockRepository seatLockRepo, ITripRepository tripRepo)
    {
        _bookingRepo = bookingRepo;
        _seatLockRepo = seatLockRepo;
        _tripRepo = tripRepo;
    }

    public async Task<SeatLockResponse> LockSeatsAsync(LockSeatsRequest request)
    {
        await _seatLockRepo.DeleteExpiredAsync();
        
        var entity = new SeatLockEntity
        {
            Id = Guid.NewGuid(),
            TripId = request.TripId,
            TravelDate = request.TravelDate,
            UserEmail = request.UserEmail,
            SeatNumbers = request.SeatNumbers.ToArray(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        await _seatLockRepo.CreateAsync(entity);

        return new SeatLockResponse
        {
            LockId = entity.Id,
            TripId = entity.TripId,
            SeatNumbers = entity.SeatNumbers.ToList(),
            LockExpiresAt = entity.ExpiresAt,
            Message = "Seats locked successfully for 10 minutes."
        };
    }

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
    {
        // Fetch the seat lock to get TripId and the reserved seat numbers
        var seatLock = await _seatLockRepo.GetByIdAsync(request.LockId);
        if (seatLock is null)
            throw new InvalidOperationException("Seat lock not found or has expired. Please lock seats again.");

        // Fetch the trip to calculate pricing
        var trip = await _tripRepo.GetByIdAsync(seatLock.TripId);
        if (trip is null || !trip.IsActive)
            throw new KeyNotFoundException("Trip not found or is no longer active.");

        var seatCount = seatLock.SeatNumbers.Length;
        var pricePerSeat = trip.BasePrice + trip.PlatformFee;
        var totalAmount = pricePerSeat * seatCount;

        var entity = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Pnr = "PNR" + Random.Shared.Next(10000, 99999),
            TripId = seatLock.TripId,
            TravelDate = request.TravelDate,
            UserEmail = request.UserEmail,
            SeatNumbers = seatLock.SeatNumbers.ToArray(),
            TotalAmount = totalAmount,
            PaymentMode = request.PaymentMode.ToString(),
            PaymentStatus = request.PaymentMode == Models.PaymentMode.Dummy ? "PAID" : "PENDING",
            TicketDownloadUrl = string.Empty,
            MailStatus = "PENDING",
            IsCancelled = false,
            RefundAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        entity.TicketDownloadUrl = $"/api/bookings/{entity.Id}/ticket";

        await _bookingRepo.CreateAsync(entity);

        // Assign seat numbers to passengers in order
        var seatsInOrder = seatLock.SeatNumbers.OrderBy(s => s).ToList();
        var passengers = request.Passengers.Select((p, i) => new BookingPassengerEntity
        {
            Id = Guid.NewGuid(),
            BookingId = entity.Id,
            SeatNumber = i < seatsInOrder.Count ? seatsInOrder[i] : 0,
            Name = p.Name,
            Age = p.Age,
            Gender = p.Gender
        });

        await _bookingRepo.AddPassengersAsync(passengers);
        await _seatLockRepo.DeleteAsync(request.LockId);

        return new BookingResponse
        {
            BookingId = entity.Id,
            Pnr = entity.Pnr,
            TripId = entity.TripId,
            SeatNumbers = entity.SeatNumbers.ToList(),
            TotalAmount = entity.TotalAmount,
            IsCancelled = false,
            TicketDownloadUrl = entity.TicketDownloadUrl,
            MailStatus = entity.MailStatus,
            PaymentStatus = entity.PaymentStatus
        };
    }

    public async Task<CancelBookingResponse> CancelBookingAsync(Guid bookingId, string userEmail)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null || !booking.UserEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            throw new KeyNotFoundException("Booking not found.");

        if (booking.IsCancelled)
            throw new InvalidOperationException("Booking is already cancelled.");

        await _bookingRepo.CancelAsync(bookingId, booking.TotalAmount);

        return new CancelBookingResponse
        {
            BookingId = bookingId,
            Cancelled = true,
            RefundAmount = booking.TotalAmount,
            Message = "Booking cancelled successfully."
        };
    }

    public async Task<IEnumerable<BookingResponse>> GetBookingsByUserAsync(string userEmail)
    {
        var bookings = await _bookingRepo.GetByUserEmailAsync(userEmail);
        return bookings.Select(b => new BookingResponse
        {
            BookingId = b.Id,
            Pnr = b.Pnr,
            TripId = b.TripId,
            SeatNumbers = b.SeatNumbers.ToList(),
            TotalAmount = b.TotalAmount,
            RefundAmount = b.RefundAmount,
            IsCancelled = b.IsCancelled,
            TicketDownloadUrl = b.TicketDownloadUrl,
            MailStatus = b.MailStatus,
            PaymentStatus = b.PaymentStatus
        });
    }

    public async Task<EnhancedBookingResponse> GetEnhancedBookingAsync(Guid bookingId, string userEmail)
    {
        var detail = await _bookingRepo.GetEnhancedByIdAsync(bookingId);
        if (detail == null || !detail.Booking.UserEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            throw new KeyNotFoundException("Booking not found.");

        var passengers = await _bookingRepo.GetPassengersByBookingIdAsync(bookingId);
        return MapToEnhancedResponse(detail, passengers);
    }

    public async Task<IEnumerable<EnhancedBookingResponse>> GetBookingsHistoryAsync(BookingsHistoryFilter filter)
    {
        var statusString = filter.Type switch
        {
            BookingsHistoryFilter.HistoryType.Past => "Past",
            BookingsHistoryFilter.HistoryType.Present => "Present",
            BookingsHistoryFilter.HistoryType.Future => "Future",
            BookingsHistoryFilter.HistoryType.Cancelled => "Cancelled",
            _ => null
        };

        var history = await _bookingRepo.GetHistoryAsync(filter.UserEmail, statusString, DateTime.UtcNow);
        var bookingIds = history.Select(h => h.Booking.Id).ToArray();
        var allPassengers = await _bookingRepo.GetPassengersByBookingIdsAsync(bookingIds);
        var passengerGroups = allPassengers.GroupBy(p => p.BookingId).ToDictionary(g => g.Key, g => g.ToList());

        return history.Select(h => MapToEnhancedResponse(h, passengerGroups.GetValueOrDefault(h.Booking.Id, new List<BookingPassengerEntity>())));
    }

    public async Task<TicketResponse> GetTicketAsync(Guid bookingId, string userEmail)
    {
        var detail = await _bookingRepo.GetEnhancedByIdAsync(bookingId);
        if (detail == null || !detail.Booking.UserEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            throw new KeyNotFoundException("Booking not found.");

        var passengers = await _bookingRepo.GetPassengersByBookingIdAsync(bookingId);

        return new TicketResponse
        {
            BookingId = detail.Booking.Id,
            Pnr = detail.Booking.Pnr,
            BusName = detail.BusName,
            Source = detail.Source,
            Destination = detail.Destination,
            DepartureTime = detail.Trip.DepartureTime,
            ArrivalTime = detail.Trip.ArrivalTime,
            Passengers = passengers.Select(p => new BookingPassenger
            {
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatNumber = p.SeatNumber
            }).ToList(),
            TotalAmount = detail.Booking.TotalAmount,
            PaymentStatus = detail.Booking.PaymentStatus,
            TicketUrl = detail.Booking.TicketDownloadUrl
        };
    }

    public Task<(byte[] Content, string FileName)> GenerateTicketFileAsync(Guid bookingId, string userEmail)
    {
        // PDF generation would use a library like QuestPDF.
        return Task.FromResult((Array.Empty<byte>(), $"Ticket_{bookingId}.pdf"));
    }

    public async Task<PaymentInitiateResponse> InitiatePaymentAsync(PaymentInitiateRequest request)
    {
        var booking = await _bookingRepo.GetByIdAsync(request.BookingId);
        if (booking == null) throw new KeyNotFoundException("Booking not found.");

        return new PaymentInitiateResponse
        {
            PaymentId = Guid.NewGuid(),
            BookingId = request.BookingId,
            Amount = booking.TotalAmount,
            PaymentMode = request.PaymentMode,
            RazorpayOrderId = request.PaymentMode == Models.PaymentMode.Razorpay ? "order_" + Guid.NewGuid() : null,
            Message = "Payment initiated successfully."
        };
    }

    public async Task<PaymentVerifyResponse> VerifyPaymentAsync(PaymentVerifyRequest request)
    {
        // In a real app, verify signature with Razorpay here.
        await _bookingRepo.UpdatePaymentStatusAsync(request.PaymentId, "PAID");
        return new PaymentVerifyResponse
        {
            PaymentId = request.PaymentId,
            BookingId = Guid.Empty, // Would be resolved from a payments table in production
            Verified = true,
            Message = "Payment verified successfully."
        };
    }

    private static EnhancedBookingResponse MapToEnhancedResponse(EnhancedBookingDetail detail, IEnumerable<BookingPassengerEntity> passengers)
    {
        return new EnhancedBookingResponse
        {
            BookingId = detail.Booking.Id,
            Pnr = detail.Booking.Pnr,
            TripId = detail.Booking.TripId,
            TravelDate = detail.Booking.TravelDate,
            BusName = detail.BusName,
            Source = detail.Source,
            Destination = detail.Destination,
            DepartureTime = detail.Trip.DepartureTime,
            ArrivalTime = detail.Trip.ArrivalTime,
            SeatNumbers = detail.Booking.SeatNumbers.ToList(),
            Passengers = passengers.Select(p => new BookingPassenger
            {
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatNumber = p.SeatNumber
            }).ToList(),
            TotalAmount = detail.Booking.TotalAmount,
            RefundAmount = detail.Booking.RefundAmount,
            IsCancelled = detail.Booking.IsCancelled,
            PaymentStatus = detail.Booking.PaymentStatus,
            TicketUrl = detail.Booking.TicketDownloadUrl,
            BookedAt = detail.Booking.CreatedAt,
            Status = detail.Booking.IsCancelled ? "Cancelled" : "Confirmed"
        };
    }
}
