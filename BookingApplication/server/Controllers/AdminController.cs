using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAuthService _authService;

    public AdminController(IAdminService adminService, IAuthService authService)
    {
        _adminService = adminService;
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        return Ok(await _authService.LoginAdminAsync(request));
    }

    [HttpGet("operators")]
    public async Task<ActionResult<IEnumerable<OperatorResponse>>> Operators()
    {
        return Ok(await _adminService.GetOperatorsAsync());
    }

    [HttpPost("operators/{operatorId:guid}/approval")]
    public async Task<ActionResult<OperatorResponse>> ApproveOperator([FromRoute] Guid operatorId, [FromBody] ApprovalRequest request)
    {
        return Ok(await _adminService.ApproveOperatorAsync(operatorId, request));
    }

    [HttpPost("operators/{operatorId:guid}/disable")]
    public async Task<ActionResult<OperatorResponse>> DisableOperator([FromRoute] Guid operatorId, [FromBody] DisableOperatorRequest request)
    {
        return Ok(await _adminService.DisableOperatorAsync(operatorId, request));
    }

    [HttpPost("operators/{operatorId:guid}/enable")]
    public async Task<ActionResult<EnableOperatorResponse>> EnableOperator([FromRoute] Guid operatorId, [FromBody] EnableOperatorRequest request)
    {
        return Ok(await _adminService.EnableOperatorAsync(operatorId, request));
    }
    
    [HttpPost("routes")]
    public async Task<ActionResult<RouteResponse>> AddRoute([FromBody] RouteRequest request)
    {
        return Ok(await _adminService.CreateRouteAsync(request));
    }

    [HttpGet("routes")]
    public async Task<ActionResult<IEnumerable<RouteResponse>>> Routes()
    {
        return Ok(await _adminService.GetRoutesAsync());
    }

    [HttpGet("buses")]
    public async Task<ActionResult<IEnumerable<BusResponse>>> GetBuses()
    {
        return Ok(await _adminService.GetAllBusesAsync());
    }

    [HttpPost("buses/{busId:guid}/approval")]
    public async Task<ActionResult<BusResponse>> ApproveBus([FromRoute] Guid busId, [FromBody] ApprovalRequest request)
    {
        return Ok(await _adminService.ApproveBusAsync(busId, request));
    }

    [HttpPost("buses/{busId:guid}/disable")]
    public async Task<ActionResult<BusResponse>> DisableBus([FromRoute] Guid busId, [FromBody] DisableBusRequest request)
    {
        return Ok(await _adminService.DisableBusAsync(busId, request));
    }

    [HttpGet("notifications/{recipientEmail}")]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> Notifications([FromRoute] string recipientEmail)
    {
        return Ok(await _adminService.GetNotificationsAsync(recipientEmail));
    }

    [HttpPost("platform-fee")]
    public async Task<ActionResult<PlatformFeeResponse>> SetPlatformFee([FromBody] PlatformFeeRequest request)
    {
        return Ok(await _adminService.SetPlatformFeeAsync(request));
    }

    [HttpGet("platform-fee")]
    public async Task<ActionResult<PlatformFeeResponse>> GetPlatformFee()
    {
        return Ok(await _adminService.GetCurrentPlatformFeeAsync());
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<AdminRevenueResponse>> GetRevenue()
    {
        return Ok(await _adminService.GetAdminRevenueAsync());
    }
}