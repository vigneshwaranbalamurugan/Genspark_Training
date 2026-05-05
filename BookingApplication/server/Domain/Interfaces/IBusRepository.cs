using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for buses.
/// </summary>
public interface IBusRepository
{
    Task<BusEntity?> GetByIdAsync(Guid busId);
    Task<IEnumerable<BusEntity>> GetAllAsync();
    Task<IEnumerable<BusEntity>> GetByOperatorIdAsync(Guid operatorId);
    Task<BusEntity> CreateAsync(BusEntity entity);
    Task UpdateApprovalAsync(Guid busId, bool isApproved);
    Task UpdateTemporaryAvailabilityAsync(Guid busId, bool isUnavailable);
    Task DeactivateAsync(Guid busId);
    Task<bool> ExistsByBusNumberAsync(string busNumber);
}
