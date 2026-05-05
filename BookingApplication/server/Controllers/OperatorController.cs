using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/operator")]
[Authorize(Roles = "Operator")]
public sealed class OperatorController : ControllerBase
{
    private readonly IOperatorService _operatorService;
    private readonly IAdminService _adminService;

    public OperatorController(IOperatorService operatorService, IAdminService adminService)
    {
        _operatorService = operatorService;
        _adminService = adminService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<OperatorResponse>> Register([FromBody] OperatorRegisterRequest request)
    {
        return Ok(await _operatorService.RegisterOperatorAsync(request));
    }

    [HttpGet("{operatorId:guid}/buses")]
    public async Task<ActionResult<IEnumerable<BusResponse>>> Buses([FromRoute] Guid operatorId)
    {
        return Ok(await _operatorService.GetOperatorBusesAsync(operatorId));
    }

    [HttpGet("routes")]
    public async Task<ActionResult<IEnumerable<RouteResponse>>> Routes()
    {
        return Ok(await _adminService.GetRoutesAsync());
    }

    [HttpPost("buses")]
    public async Task<ActionResult<BusResponse>> AddBus([FromBody] BusRequest request)
    {
        return Ok(await _operatorService.AddBusAsync(request));
    }

    [HttpPost("{operatorId:guid}/buses/{busId:guid}/temporary-unavailable")]
    public async Task<ActionResult<BusResponse>> SetTemporaryUnavailable([FromRoute] Guid operatorId, [FromRoute] Guid busId, [FromQuery] bool unavailable)
    {
        return Ok(await _operatorService.SetBusTemporaryAvailabilityAsync(operatorId, busId, unavailable));
    }

    [HttpDelete("{operatorId:guid}/buses/{busId:guid}")]
    public async Task<IActionResult> RemoveBus([FromRoute] Guid operatorId, [FromRoute] Guid busId)
    {
        await _operatorService.RemoveBusAsync(operatorId, busId);
        return NoContent();
    }

    [HttpPost("trips")]
    public async Task<ActionResult<TripSummary>> AddTrip([FromBody] TripCreateRequest request)
    {
        return Ok(await _operatorService.AddTripAsync(request));
    }

    [HttpGet("{operatorId:guid}/dashboard")]
    public async Task<ActionResult<OperatorDashboardResponse>> Dashboard([FromRoute] Guid operatorId)
    {
        return Ok(await _operatorService.GetOperatorDashboardAsync(operatorId));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<OperatorLoginResponse>> Login([FromBody] OperatorLoginRequest request)
    {
        return Ok(await _operatorService.OperatorLoginAsync(request));
    }

    [HttpPost("{operatorId:guid}/buses/register")]
    public async Task<ActionResult<BusWithNumberResponse>> RegisterBus([FromRoute] Guid operatorId, [FromBody] BusRegistrationRequest request)
    {
        return Ok(await _operatorService.AddBusWithNumberAsync(operatorId, request));
    }

    [HttpGet("{operatorId:guid}/bookings")]
    public async Task<ActionResult<IEnumerable<OperatorBookingView>>> GetBookings([FromRoute] Guid operatorId, [FromQuery] Guid? busId)
    {
        return Ok(await _operatorService.GetOperatorBookingsAsync(operatorId, busId));
    }

    [HttpGet("{operatorId:guid}/revenue")]
    public async Task<ActionResult<OperatorRevenueResponse>> GetRevenue([FromRoute] Guid operatorId)
    {
        return Ok(await _operatorService.GetOperatorRevenueAsync(operatorId));
    }

    [HttpPost("{operatorId:guid}/preferred-routes")]
    public async Task<ActionResult<PreferredRouteResponse>> AddPreferredRoute([FromRoute] Guid operatorId, [FromBody] PreferredRouteRequest request)
    {
        return Ok(await _operatorService.AddPreferredRouteAsync(operatorId, request));
    }

    [HttpGet("{operatorId:guid}/preferred-routes")]
    public async Task<ActionResult<List<PreferredRouteResponse>>> GetPreferredRoutes([FromRoute] Guid operatorId)
    {
        return Ok(await _operatorService.GetOperatorPreferredRoutesAsync(operatorId));
    }

    [HttpPost("{operatorId:guid}/routes/{routeId:guid}/pickup-points")]
    public async Task<ActionResult<PickupDropPointResponse>> AddPickupPoint([FromRoute] Guid operatorId, [FromRoute] Guid routeId, [FromBody] PickupDropPointRequest request)
    {
        return Ok(await _operatorService.AddPickupDropPointAsync(operatorId, routeId, true, request));
    }

    [HttpPost("{operatorId:guid}/routes/{routeId:guid}/drop-points")]
    public async Task<ActionResult<PickupDropPointResponse>> AddDropPoint([FromRoute] Guid operatorId, [FromRoute] Guid routeId, [FromBody] PickupDropPointRequest request)
    {
        return Ok(await _operatorService.AddPickupDropPointAsync(operatorId, routeId, false, request));
    }

    [HttpGet("{operatorId:guid}/routes/{routeId:guid}/pickup-points")]
    public async Task<ActionResult<IEnumerable<PickupDropPointResponse>>> GetPickupPoints([FromRoute] Guid operatorId, [FromRoute] Guid routeId)
    {
        return Ok(await _operatorService.GetPickupDropPointsAsync(operatorId, routeId, true));
    }

    [HttpGet("{operatorId:guid}/routes/{routeId:guid}/drop-points")]
    public async Task<ActionResult<IEnumerable<PickupDropPointResponse>>> GetDropPoints([FromRoute] Guid operatorId, [FromRoute] Guid routeId)
    {
        return Ok(await _operatorService.GetPickupDropPointsAsync(operatorId, routeId, false));
    }

    [HttpPost("{operatorId:guid}/trips/create")]
    public async Task<ActionResult<TripSummary>> CreateTripWithDetails([FromRoute] Guid operatorId, [FromBody] TripCreateRequestWithDetails request)
    {
        request.OperatorId = operatorId;
        return Ok(await _operatorService.AddTripWithDetailsAsync(operatorId, request));
    }

    public class DisableReasonRequest { public string Reason { get; set; } = string.Empty; }

    [HttpPost("{operatorId:guid}/buses/{busId:guid}/request-disable")]
    public async Task<IActionResult> RequestBusDisable([FromRoute] Guid operatorId, [FromRoute] Guid busId, [FromBody] DisableReasonRequest request)
    {
        await _operatorService.RequestBusDisableAsync(operatorId, busId, request.Reason ?? "No reason provided");
        return Ok(new { message = "Request submitted to admin" });
    }

    [HttpGet("{operatorId:guid}/trips")]
    public async Task<ActionResult<IEnumerable<TripSummary>>> GetTrips([FromRoute] Guid operatorId)
    {
        return Ok(await _operatorService.GetOperatorTripsAsync(operatorId));
    }

    [HttpDelete("{operatorId:guid}/trips/{tripId:guid}")]
    public async Task<IActionResult> DeleteTrip([FromRoute] Guid operatorId, [FromRoute] Guid tripId)
    {
        await _operatorService.DeleteTripAsync(operatorId, tripId);
        return NoContent();
    }
}