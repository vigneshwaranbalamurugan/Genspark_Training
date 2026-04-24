using server.Models;

namespace server.Services;

public interface IRegistrationService
{
    StartRegistrationResponse StartRegistration(string email);

    VerifyOtpResponse VerifyOtp(string email, string otpCode);

    SetPasswordResponse SetPassword(string email, string password);

    PersonalDetailsResponse CompleteProfile(PersonalDetailsRequest request);

    RegistrationStatusResponse? GetStatus(string email);
}