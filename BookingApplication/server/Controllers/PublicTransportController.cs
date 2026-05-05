using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicTransportController : ControllerBase
{
    private readonly ITripService _tripService;

    public PublicTransportController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet("buses/search")]
    public async Task<ActionResult<TripSearchResponse>> Search(
        [FromQuery] string source,
        [FromQuery] string destination,
        [FromQuery] DateOnly date,
        [FromQuery] DateOnly? returnDate)
    {
        var response = await _tripService.SearchTripsAsync(new TripSearchRequest
        {
            Source = source,
            Destination = destination,
            Date = date,
            ReturnDate = returnDate
        });

        return Ok(response);
    }

    [HttpGet("trips/{tripId:guid}/seats")]
    public async Task<ActionResult<SeatAvailabilityResponse>> Seats([FromRoute] Guid tripId, [FromQuery] DateOnly travelDate)
    {
        return Ok(await _tripService.GetSeatAvailabilityAsync(tripId, travelDate));
    }

    [HttpGet("buses/search-fuzzy")]
    public async Task<ActionResult<TripSearchResponse>> SearchFuzzy(
        [FromQuery] string source,
        [FromQuery] string destination,
        [FromQuery] DateOnly date,
        [FromQuery] DateOnly? returnDate)
    {
        var response = await _tripService.SearchTripsFuzzyAsync(source, destination, date, returnDate);
        return Ok(response);
    }

    [HttpGet("trips/{tripId:guid}/layout")]
    public async Task<ActionResult<SeatLayoutResponse>> GetSeatLayout([FromRoute] Guid tripId, [FromQuery] DateOnly travelDate)
    {
        return Ok(await _tripService.GetSeatLayoutAsync(tripId, travelDate));
    }
}