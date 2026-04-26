using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController : ControllerBase
{
    private readonly IRegistrationService registrationService;
    private readonly IJwtService jwtService;

    public RegistrationController(IRegistrationService registrationService, IJwtService jwtService)
    {
        this.registrationService = registrationService;
        this.jwtService = jwtService;
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
            var response = registrationService.SetPassword(request.Email, request.Password);
            
            // Generate JWT token
            var token = jwtService.GenerateToken(request.Email, UserRole.User.ToString());
            response.AccessToken = token;
            response.Role = UserRole.User.ToString();
            
            return Ok(response);
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
    public ActionResult<LoginResponse> Login([FromBody] CustomerLoginRequest request, [FromServices] ITransportService transportService)
    {
        try
        {
            var response = transportService.Login(request);
            
            // Generate JWT token
            response.JwtToken = jwtService.GenerateToken(request.Email, UserRole.User.ToString());
            
            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}