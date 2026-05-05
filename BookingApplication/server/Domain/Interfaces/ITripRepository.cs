using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for trips.
/// </summary>
public interface ITripRepository
{
    Task<TripEntity?> GetByIdAsync(Guid tripId);
    Task<IEnumerable<TripEntity>> GetByOperatorIdAsync(Guid operatorId);
    Task<IEnumerable<TripDetail>> GetTripsWithDetailsByOperatorAsync(Guid operatorId);
    Task<TripEntity> CreateAsync(TripEntity entity);
    Task DeactivateAsync(Guid tripId, Guid operatorId);
    Task DeactivateByOperatorAsync(Guid operatorId, DateTime cutoff);
    Task UpdatePlatformFeeForActiveTripsAsync(decimal newFee);
    Task<int> CountActiveByOperatorAsync(Guid operatorId);

    /// <summary>
    /// Find trips matching exact source/destination and date (supports OneTime and Daily).
    /// </summary>
    Task<IEnumerable<TripDetail>> SearchAsync(string source, string destination, DateOnly date);

    /// <summary>
    /// Find trips matching fuzzy source/destination and date (supports OneTime and Daily).
    /// </summary>
    Task<IEnumerable<TripDetail>> SearchFuzzyAsync(string source, string destination, DateOnly date);
}
