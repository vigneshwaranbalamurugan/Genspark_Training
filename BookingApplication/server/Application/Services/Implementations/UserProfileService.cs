using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepo;

    public UserProfileService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<UserProfileResponse> UpsertUserProfileAsync(UserProfileRequest request)
    {
        var entity = new UserEntity
        {
            Id = Guid.NewGuid(), // Upsert logic will keep existing ID if conflict
            Email = request.Email,
            FullName = request.FullName,
            SsoProvider = request.SsoProvider,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var saved = await _userRepo.UpsertAsync(entity);

        return new UserProfileResponse
        {
            UserId = saved.Id,
            Email = saved.Email,
            FullName = saved.FullName,
            SsoProvider = saved.SsoProvider
        };
    }

    public async Task<UserProfileResponse?> GetUserProfileAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user is null) return null;

        return new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            SsoProvider = user.SsoProvider
        };
    }
}
