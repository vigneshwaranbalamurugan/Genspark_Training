using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for registration sessions.
/// </summary>
public interface IRegistrationRepository
{
    Task<RegistrationSessionEntity?> GetByEmailAsync(string email);
    Task<RegistrationSessionEntity> UpsertAsync(RegistrationSessionEntity entity);
    Task UpdateOtpVerifiedAsync(string email);
    Task UpdatePasswordHashAsync(string email, string passwordHash);
    Task UpdateProfileAsync(RegistrationSessionEntity entity);
}
