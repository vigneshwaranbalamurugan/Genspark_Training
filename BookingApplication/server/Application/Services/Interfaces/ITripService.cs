using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles trip search and seat availability for public consumers.
/// </summary>
public interface ITripService
{
    Task<TripSearchResponse> SearchTripsAsync(TripSearchRequest request);
    Task<TripSearchResponse> SearchTripsFuzzyAsync(string source, string destination, DateOnly date, DateOnly? returnDate);
    Task<SeatAvailabilityResponse> GetSeatAvailabilityAsync(Guid tripId, DateOnly travelDate);
    Task<SeatLayoutResponse> GetSeatLayoutAsync(Guid tripId, DateOnly travelDate);
}
