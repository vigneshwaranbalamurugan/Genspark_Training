using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IAuthService _authService;

    public RegistrationController(IRegistrationService registrationService, IAuthService authService)
    {
        _registrationService = registrationService;
        _authService = authService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<StartRegistrationResponse>> Start([FromBody] StartRegistrationRequest request)
    {
        return Ok(await _registrationService.StartRegistrationAsync(request.Email));
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<VerifyOtpResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        return Ok(await _registrationService.VerifyOtpAsync(request.Email, request.OtpCode));
    }

    [HttpPost("set-password")]
    public async Task<ActionResult<SetPasswordResponse>> SetPassword([FromBody] SetPasswordRequest request)
    {
        var response = await _registrationService.SetPasswordAsync(request.Email, request.Password);
        return Ok(response);
    }

    [HttpPost("personal-details")]
    public async Task<ActionResult<PersonalDetailsResponse>> CompleteProfile([FromBody] PersonalDetailsRequest request)
    {
        return Ok(await _registrationService.CompleteProfileAsync(request));
    }

    [HttpGet("status/{email}")]
    public async Task<ActionResult<RegistrationStatusResponse>> GetStatus([FromRoute] string email)
    {
        var status = await _registrationService.GetStatusAsync(email);
        return Ok(status);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] CustomerLoginRequest request)
    {
        return Ok(await _authService.LoginUserAsync(request));
    }
}