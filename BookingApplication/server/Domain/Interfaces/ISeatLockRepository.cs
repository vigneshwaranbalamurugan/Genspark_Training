using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for seat locks.
/// </summary>
public interface ISeatLockRepository
{
    Task<SeatLockEntity?> GetByIdAsync(Guid lockId);
    Task<SeatLockEntity> CreateAsync(SeatLockEntity entity);
    Task DeleteAsync(Guid lockId);
    Task DeleteExpiredAsync();
    Task<HashSet<int>> GetLockedSeatNumbersAsync(Guid tripId, DateOnly travelDate);
}
