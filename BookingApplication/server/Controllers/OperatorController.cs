using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/operator")]
public sealed class OperatorController : ControllerBase
{
    private readonly ITransportService transportService;

    public OperatorController(ITransportService transportService)
    {
        this.transportService = transportService;
    }

    [HttpPost("register")]
    public ActionResult<OperatorResponse> Register([FromBody] OperatorRegisterRequest request)
    {
        try
        {
            return Ok(transportService.RegisterOperator(request));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{operatorId:guid}/buses")]
    public ActionResult<IEnumerable<BusResponse>> Buses([FromRoute] Guid operatorId)
    {
        return Ok(transportService.GetOperatorBuses(operatorId));
    }

    [HttpPost("buses")]
    public ActionResult<BusResponse> AddBus([FromBody] BusRequest request)
    {
        try
        {
            return Ok(transportService.AddBus(request));
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

    [HttpPost("{operatorId:guid}/buses/{busId:guid}/temporary-unavailable")]
    public ActionResult<BusResponse> SetTemporaryUnavailable([FromRoute] Guid operatorId, [FromRoute] Guid busId, [FromQuery] bool unavailable)
    {
        try
        {
            return Ok(transportService.SetBusTemporaryAvailability(operatorId, busId, unavailable));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("{operatorId:guid}/buses/{busId:guid}")]
    public IActionResult RemoveBus([FromRoute] Guid operatorId, [FromRoute] Guid busId)
    {
        try
        {
            transportService.RemoveBus(operatorId, busId);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("trips")]
    public ActionResult<TripSummary> AddTrip([FromBody] TripCreateRequest request)
    {
        try
        {
            return Ok(transportService.AddTrip(request));
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

    [HttpGet("{operatorId:guid}/dashboard")]
    public ActionResult<OperatorDashboardResponse> Dashboard([FromRoute] Guid operatorId)
    {
        try
        {
            return Ok(transportService.GetOperatorDashboard(operatorId));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    public ActionResult<OperatorLoginResponse> Login([FromBody] OperatorLoginRequest request)
    {
        try
        {
            return Ok(transportService.OperatorLogin(request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpPost("{operatorId:guid}/buses/register")]
    public ActionResult<BusWithNumberResponse> RegisterBus([FromRoute] Guid operatorId, [FromBody] BusRegistrationRequest request)
    {
        try
        {
            return Ok(transportService.AddBusWithNumber(operatorId, request));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{operatorId:guid}/bookings")]
    public ActionResult<IEnumerable<OperatorBookingView>> GetBookings([FromRoute] Guid operatorId, [FromQuery] Guid? busId)
    {
        return Ok(transportService.GetOperatorBookings(operatorId, busId));
    }

    [HttpGet("{operatorId:guid}/revenue")]
    public ActionResult<OperatorRevenueResponse> GetRevenue([FromRoute] Guid operatorId)
    {
        try
        {
            return Ok(transportService.GetOperatorRevenue(operatorId));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{operatorId:guid}/preferred-routes")]
    public ActionResult<PreferredRouteResponse> AddPreferredRoute([FromRoute] Guid operatorId, [FromBody] PreferredRouteRequest request)
    {
        try
        {
            return Ok(transportService.AddPreferredRoute(operatorId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{operatorId:guid}/preferred-routes")]
    public ActionResult<IEnumerable<PreferredRouteResponse>> GetPreferredRoutes([FromRoute] Guid operatorId)
    {
        return Ok(transportService.GetOperatorPreferredRoutes(operatorId));
    }

    [HttpPost("{operatorId:guid}/routes/{routeId:guid}/pickup-points")]
    public ActionResult<PickupDropPointResponse> AddPickupPoint([FromRoute] Guid operatorId, [FromRoute] Guid routeId, [FromBody] PickupDropPointRequest request)
    {
        try
        {
            return Ok(transportService.AddPickupDropPoint(operatorId, routeId, true, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{operatorId:guid}/routes/{routeId:guid}/drop-points")]
    public ActionResult<PickupDropPointResponse> AddDropPoint([FromRoute] Guid operatorId, [FromRoute] Guid routeId, [FromBody] PickupDropPointRequest request)
    {
        try
        {
            return Ok(transportService.AddPickupDropPoint(operatorId, routeId, false, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{operatorId:guid}/routes/{routeId:guid}/pickup-points")]
    public ActionResult<IEnumerable<PickupDropPointResponse>> GetPickupPoints([FromRoute] Guid operatorId, [FromRoute] Guid routeId)
    {
        return Ok(transportService.GetPickupDropPoints(operatorId, routeId, true));
    }

    [HttpGet("{operatorId:guid}/routes/{routeId:guid}/drop-points")]
    public ActionResult<IEnumerable<PickupDropPointResponse>> GetDropPoints([FromRoute] Guid operatorId, [FromRoute] Guid routeId)
    {
        return Ok(transportService.GetPickupDropPoints(operatorId, routeId, false));
    }

    [HttpPost("{operatorId:guid}/trips/create")]
    public ActionResult<TripSummary> CreateTripWithDetails([FromRoute] Guid operatorId, [FromBody] TripCreateRequestWithDetails request)
    {
        try
        {
            request.OperatorId = operatorId;
            return Ok(transportService.AddTripWithDetails(operatorId, request));
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

    [HttpPost("{operatorId:guid}/buses/{busId:guid}/request-disable")]
    public IActionResult RequestBusDisable([FromRoute] Guid operatorId, [FromRoute] Guid busId, [FromBody] dynamic request)
    {
        try
        {
            var reason = request?.reason ?? "No reason provided";
            transportService.RequestBusDisable(operatorId, busId, reason.ToString());
            return Ok(new { message = "Request submitted to admin" });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}