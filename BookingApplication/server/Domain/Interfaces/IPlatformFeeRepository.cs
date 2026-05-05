using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for platform fees.
/// </summary>
public interface IPlatformFeeRepository
{
    Task<PlatformFeeEntity?> GetActiveAsync();
    Task<decimal> GetActiveAmountAsync();
    Task<PlatformFeeEntity> CreateAsync(PlatformFeeEntity entity);
    Task DeactivateAllAsync();
}
