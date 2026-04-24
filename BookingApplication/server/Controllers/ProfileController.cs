using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly ITransportService transportService;

    public ProfileController(ITransportService transportService)
    {
        this.transportService = transportService;
    }

    [HttpPost]
    public ActionResult<UserProfileResponse> Upsert([FromBody] UserProfileRequest request)
    {
        return Ok(transportService.UpsertUserProfile(request));
    }

    [HttpGet("{email}")]
    public ActionResult<UserProfileResponse> Get([FromRoute] string email)
    {
        var profile = transportService.GetUserProfile(email);
        if (profile is null)
        {
            return NotFound(new { message = "Profile not found." });
        }

        return Ok(profile);
    }
}