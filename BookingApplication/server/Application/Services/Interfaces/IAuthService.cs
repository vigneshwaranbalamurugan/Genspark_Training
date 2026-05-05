using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles customer, operator, and admin authentication.
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginUserAsync(CustomerLoginRequest request);
    Task<OperatorLoginResponse> LoginOperatorAsync(OperatorLoginRequest request);
    Task<AdminLoginResponse> LoginAdminAsync(AdminLoginRequest request);
}
