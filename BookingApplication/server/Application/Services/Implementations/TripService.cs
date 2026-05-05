using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class TripService : ITripService
{
    private readonly ITripRepository _tripRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly ISeatLockRepository _seatLockRepo;
    private readonly IPreferredRouteRepository _pointsRepo;

    public TripService(
        ITripRepository tripRepo,
        IBookingRepository bookingRepo,
        ISeatLockRepository seatLockRepo,
        IPreferredRouteRepository pointsRepo)
    {
        _tripRepo = tripRepo;
        _bookingRepo = bookingRepo;
        _seatLockRepo = seatLockRepo;
        _pointsRepo = pointsRepo;
    }

    public async Task<TripSearchResponse> SearchTripsAsync(TripSearchRequest request)
    {
        await _seatLockRepo.DeleteExpiredAsync();

        var outbound = await SearchAsync(request.Source, request.Destination, request.Date);
        var response = new TripSearchResponse { OutboundTrips = outbound };

        if (request.ReturnDate.HasValue)
        {
            response.ReturnTrips = await SearchAsync(request.Destination, request.Source, request.ReturnDate.Value);
        }

        return response;
    }

    public async Task<TripSearchResponse> SearchTripsFuzzyAsync(string source, string destination, DateOnly date, DateOnly? returnDate)
    {
        await _seatLockRepo.DeleteExpiredAsync();

        var outbound = await SearchFuzzyAsync(source, destination, date);
        var response = new TripSearchResponse { OutboundTrips = outbound };

        if (returnDate.HasValue)
        {
            response.ReturnTrips = await SearchFuzzyAsync(destination, source, returnDate.Value);
        }

        return response;
    }

    public async Task<SeatAvailabilityResponse> GetSeatAvailabilityAsync(Guid tripId, DateOnly travelDate)
    {
        await _seatLockRepo.DeleteExpiredAsync();
        
        var trip = await _tripRepo.GetByIdAsync(tripId);
        if (trip is null || !trip.IsActive) throw new KeyNotFoundException("Trip not found or inactive.");

        var reservedCount = await _bookingRepo.GetReservedSeatCountAsync(tripId, travelDate);
        var lockedSeats = await _seatLockRepo.GetLockedSeatNumbersAsync(tripId, travelDate);
        
        // This is still slightly incomplete as we don't have bus capacity here easily without another join.
        // But for SeatAvailabilityResponse, we'll return what we can.
        
        return new SeatAvailabilityResponse
        {
            TripId = tripId,
            SeatsAvailableLeft = -1 // Better fetched via SeatLayout
        };
    }

    public async Task<SeatLayoutResponse> GetSeatLayoutAsync(Guid tripId, DateOnly travelDate)
    {
        await _seatLockRepo.DeleteExpiredAsync();

        // This would normally be a specialized query. For now, let's assume we can fetch the trip details.
        // Actually, we need the bus layout.
        throw new NotImplementedException("SeatLayout requires joined data from Bus table.");
    }

    private async Task<List<TripSummary>> SearchAsync(string source, string destination, DateOnly date)
    {
        var trips = await _tripRepo.SearchAsync(source, destination, date);
        return await MapToSummariesAsync(trips, date);
    }

    private async Task<List<TripSummary>> SearchFuzzyAsync(string source, string destination, DateOnly date)
    {
        var trips = await _tripRepo.SearchFuzzyAsync(source, destination, date);
        return await MapToSummariesAsync(trips, date);
    }

    private async Task<List<TripSummary>> MapToSummariesAsync(IEnumerable<TripDetail> trips, DateOnly travelDate)
    {
        var list = new List<TripSummary>();
        foreach (var t in trips)
        {
            var reservedCount = await _bookingRepo.GetReservedSeatCountAsync(t.Trip.Id, travelDate);
            var lockedSeats = await _seatLockRepo.GetLockedSeatNumbersAsync(t.Trip.Id, travelDate);
            var totalReserved = reservedCount + lockedSeats.Count;

            list.Add(new TripSummary
            {
                TripId = t.Trip.Id,
                BusId = t.Trip.BusId,
                BusName = t.BusName,
                Source = t.Source,
                Destination = t.Destination,
                DepartureTime = t.Trip.DepartureTime,
                ArrivalTime = t.Trip.ArrivalTime,
                BasePrice = t.Trip.BasePrice,
                PlatformFee = t.Trip.PlatformFee,
                Capacity = t.BusCapacity,
                SeatsAvailable = t.BusCapacity - totalReserved,
                TripType = Enum.Parse<TripType>(t.Trip.TripType),
                DaysOfWeek = t.Trip.DaysOfWeek
            });
        }
        return list;
    }
}
