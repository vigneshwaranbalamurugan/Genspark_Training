using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for bus operators.
/// </summary>
public interface IOperatorRepository
{
    Task<OperatorEntity?> GetByIdAsync(Guid operatorId);
    Task<OperatorEntity?> GetByEmailAsync(string email);
    Task<IEnumerable<OperatorEntity>> GetAllAsync();
    Task<OperatorEntity> CreateAsync(OperatorEntity entity);
    Task UpdateApprovalStatusAsync(Guid operatorId, string status);
    Task UpdateDisabledStatusAsync(Guid operatorId, bool isDisabled);
    Task<bool> ExistsByEmailAsync(string email);
}
