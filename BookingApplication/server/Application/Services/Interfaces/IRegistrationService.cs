using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles user registration flow: OTP, password, profile.
/// </summary>
public interface IRegistrationService
{
    Task<StartRegistrationResponse> StartRegistrationAsync(string email);
    Task<VerifyOtpResponse> VerifyOtpAsync(string email, string otpCode);
    Task<SetPasswordResponse> SetPasswordAsync(string email, string password);
    Task<PersonalDetailsResponse> CompleteProfileAsync(PersonalDetailsRequest request);
    Task<RegistrationStatusResponse?> GetStatusAsync(string email);
}
