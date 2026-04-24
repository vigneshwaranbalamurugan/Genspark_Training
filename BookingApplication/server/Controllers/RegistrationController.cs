using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController : ControllerBase
{
    private readonly IRegistrationService registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        this.registrationService = registrationService;
    }

    [HttpPost("start")]
    public ActionResult<StartRegistrationResponse> Start([FromBody] StartRegistrationRequest request)
    {
        try
        {
            return Ok(registrationService.StartRegistration(request.Email));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("verify-otp")]
    public ActionResult<VerifyOtpResponse> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            return Ok(registrationService.VerifyOtp(request.Email, request.OtpCode));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("set-password")]
    public ActionResult<SetPasswordResponse> SetPassword([FromBody] SetPasswordRequest request)
    {
        try
        {
            return Ok(registrationService.SetPassword(request.Email, request.Password));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("personal-details")]
    public ActionResult<PersonalDetailsResponse> CompleteProfile([FromBody] PersonalDetailsRequest request)
    {
        try
        {
            return Ok(registrationService.CompleteProfile(request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("status/{email}")]
    public ActionResult<RegistrationStatusResponse> GetStatus([FromRoute] string email)
    {
        var status = registrationService.GetStatus(email);

        if (status is null)
        {
            return NotFound(new { message = "No registration session found for the specified email." });
        }

        return Ok(status);
    }

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request, [FromServices] ITransportService transportService)
    {
        try
        {
            return Ok(transportService.Login(request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}