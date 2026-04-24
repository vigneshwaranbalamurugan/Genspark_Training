using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicTransportController : ControllerBase
{
    private readonly ITransportService transportService;

    public PublicTransportController(ITransportService transportService)
    {
        this.transportService = transportService;
    }

    [HttpGet("buses/search")]
    public ActionResult<TripSearchResponse> Search(
        [FromQuery] string source,
        [FromQuery] string destination,
        [FromQuery] DateOnly date,
        [FromQuery] DateOnly? returnDate)
    {
        var response = transportService.SearchTrips(new TripSearchRequest
        {
            Source = source,
            Destination = destination,
            Date = date,
            ReturnDate = returnDate
        });

        return Ok(response);
    }

    [HttpGet("trips/{tripId:guid}/seats")]
    public ActionResult<SeatAvailabilityResponse> Seats([FromRoute] Guid tripId)
    {
        try
        {
            return Ok(transportService.GetSeatAvailability(tripId));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("buses/search-fuzzy")]
    public ActionResult<TripSearchResponse> SearchFuzzy(
        [FromQuery] string source,
        [FromQuery] string destination,
        [FromQuery] DateOnly date,
        [FromQuery] DateOnly? returnDate)
    {
        var response = transportService.SearchTripsFuzzy(source, destination, date, returnDate);
        return Ok(response);
    }

    [HttpGet("trips/{tripId:guid}/layout")]
    public ActionResult<SeatLayoutResponse> GetSeatLayout([FromRoute] Guid tripId)
    {
        try
        {
            return Ok(transportService.GetSeatLayout(tripId));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}