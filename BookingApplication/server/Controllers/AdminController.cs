using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ITransportService transportService;

    public AdminController(ITransportService transportService)
    {
        this.transportService = transportService;
    }

    [HttpGet("operators")]
    public ActionResult<IEnumerable<OperatorResponse>> Operators()
    {
        return Ok(transportService.GetOperators());
    }

    [HttpPost("operators/{operatorId:guid}/approval")]
    public ActionResult<OperatorResponse> ApproveOperator([FromRoute] Guid operatorId, [FromBody] ApprovalRequest request)
    {
        try
        {
            return Ok(transportService.ApproveOperator(operatorId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("operators/{operatorId:guid}/disable")]
    public ActionResult<OperatorResponse> DisableOperator([FromRoute] Guid operatorId, [FromBody] DisableOperatorRequest request)
    {
        try
        {
            return Ok(transportService.DisableOperator(operatorId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("operators/{operatorId:guid}/enable")]
    public ActionResult<EnableOperatorResponse> EnableOperator([FromRoute] Guid operatorId, [FromBody] EnableOperatorRequest request)
    {
        try
        {
            return Ok(transportService.EnableOperator(operatorId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
    
    [HttpPost("routes")]
    public ActionResult<RouteResponse> AddRoute([FromBody] RouteRequest request)
    {
        try
        {
            return Ok(transportService.CreateRoute(request));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("routes")]
    public ActionResult<IEnumerable<RouteResponse>> Routes()
    {
        return Ok(transportService.GetRoutes());
    }

    [HttpPost("buses/{busId:guid}/approval")]
    public ActionResult<BusResponse> ApproveBus([FromRoute] Guid busId, [FromBody] ApprovalRequest request)
    {
        try
        {
            return Ok(transportService.ApproveBus(busId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("notifications/{recipientEmail}")]
    public ActionResult<IEnumerable<NotificationResponse>> Notifications([FromRoute] string recipientEmail)
    {
        return Ok(transportService.GetNotifications(recipientEmail));
    }

    [HttpPost("platform-fee")]
    public ActionResult<PlatformFeeResponse> SetPlatformFee([FromBody] PlatformFeeRequest request)
    {
        try
        {
            return Ok(transportService.SetPlatformFee(request));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("platform-fee")]
    public ActionResult<PlatformFeeResponse> GetPlatformFee()
    {
        return Ok(transportService.GetCurrentPlatformFee());
    }
}