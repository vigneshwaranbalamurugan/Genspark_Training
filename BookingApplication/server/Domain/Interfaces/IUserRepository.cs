using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for user profiles.
/// </summary>
public interface IUserRepository
{
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity> UpsertAsync(UserEntity entity);
}
