using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public ProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileResponse>> Upsert([FromBody] UserProfileRequest request)
    {
        return Ok(await _userProfileService.UpsertUserProfileAsync(request));
    }

    [HttpGet("{email}")]
    public async Task<ActionResult<UserProfileResponse>> Get([FromRoute] string email)
    {
        var profile = await _userProfileService.GetUserProfileAsync(email);
        if (profile is null)
        {
            return NotFound(new { message = "Profile not found." });
        }

        return Ok(profile);
    }
}