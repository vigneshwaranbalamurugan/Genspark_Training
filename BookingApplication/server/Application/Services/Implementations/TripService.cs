using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;
using System.Text.Json;

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

        // Get trip details with bus, route, and operator info
        var tripDetail = await _tripRepo.GetByIdWithDetailsAsync(tripId);
        if (tripDetail is null || !tripDetail.Trip.IsActive)
            throw new KeyNotFoundException("Trip not found or inactive.");

        // Get booked seats with passenger gender and name info
        var bookedSeatsWithGender = await _bookingRepo.GetBookedSeatsWithGenderAsync(tripId, travelDate);

        // Get locked seats
        var lockedSeats = await _seatLockRepo.GetLockedSeatNumbersAsync(tripId, travelDate);

        // Get pickup and drop points for the operator's route
        var pickupPoints = await _pointsRepo.GetPointsAsync(tripDetail.Trip.OperatorId, tripDetail.Trip.RouteId, true);
        var dropPoints = await _pointsRepo.GetPointsAsync(tripDetail.Trip.OperatorId, tripDetail.Trip.RouteId, false);

        // Build seat information dictionary
        var seatsDict = new Dictionary<int, SeatWithGenderInfo>();
        
        for (int seat = 1; seat <= tripDetail.BusCapacity; seat++)
        {
            bool isBooked = bookedSeatsWithGender.ContainsKey(seat);
            bool isLocked = lockedSeats.Contains(seat);
            
            if (isBooked)
            {
                var (gender, name) = bookedSeatsWithGender[seat];
                seatsDict[seat] = new SeatWithGenderInfo
                {
                    SeatNumber = seat,
                    IsAvailable = false,
                    BookedByGender = gender,
                    BookedByName = name
                };
            }
            else if (isLocked)
            {
                seatsDict[seat] = new SeatWithGenderInfo
                {
                    SeatNumber = seat,
                    IsAvailable = false,
                    BookedByGender = null,
                    BookedByName = null
                };
            }
            else
            {
                seatsDict[seat] = new SeatWithGenderInfo
                {
                    SeatNumber = seat,
                    IsAvailable = true,
                    BookedByGender = null,
                    BookedByName = null
                };
            }
        }

        // Calculate available seats
        int availableCount = seatsDict.Count(s => s.Value.IsAvailable);

        // Parse ladies seats from layout (if layout JSON contains ladies seats info)
        var ladiesSeats = ExtractLadiesSeats(tripDetail.LayoutJson, tripDetail.BusCapacity);

        return new SeatLayoutResponse
        {
            TripId = tripId,
            TravelDate = travelDate,
            BusName = tripDetail.BusName,
            LayoutName = tripDetail.LayoutName,
            Capacity = tripDetail.BusCapacity,
            SeatsAvailableLeft = availableCount,
            Seats = seatsDict,
            LadiesSeatsAvailable = ladiesSeats.Where(s => seatsDict[s].IsAvailable).ToList(),
            PickupPoints = pickupPoints.Select(p => new PickupDropPointResponse
            {
                PointId = p.Id,
                Location = p.Location,
                Address = p.Address,
                IsDefault = p.IsDefault,
                IsPickup = true
            }).ToList(),
            DropPoints = dropPoints.Select(p => new PickupDropPointResponse
            {
                PointId = p.Id,
                Location = p.Location,
                Address = p.Address,
                IsDefault = p.IsDefault,
                IsPickup = false
            }).ToList()
        };
    }

    private static List<int> ExtractLadiesSeats(string? layoutJson, int capacity)
    {
        // Default ladies seats if no layout info or parsing fails
        // Assuming first few seats in first row are ladies seats (common pattern)
        if (string.IsNullOrWhiteSpace(layoutJson))
            return new List<int>();

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var root = doc.RootElement;

            // Check if ladies_seats array exists in layout JSON
            if (root.TryGetProperty("ladiesSeats", out var ladiesSeatsElement))
            {
                var seats = new List<int>();
                if (ladiesSeatsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in ladiesSeatsElement.EnumerateArray())
                    {
                        if (item.TryGetInt32(out int seatNum))
                            seats.Add(seatNum);
                    }
                }
                return seats;
            }

            // Alternative: if layout has ladies_seats_count, mark first N seats as ladies
            if (root.TryGetProperty("ladiesSeatsCount", out var ladiesCountElement))
            {
                if (ladiesCountElement.TryGetInt32(out int count))
                {
                    return Enumerable.Range(1, Math.Min(count, capacity)).ToList();
                }
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return new List<int>();
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
