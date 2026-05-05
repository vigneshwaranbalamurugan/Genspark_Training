using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles user profile management.
/// </summary>
public interface IUserProfileService
{
    Task<UserProfileResponse> UpsertUserProfileAsync(UserProfileRequest request);
    Task<UserProfileResponse?> GetUserProfileAsync(string email);
}
