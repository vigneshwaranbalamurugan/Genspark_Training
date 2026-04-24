using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly ITransportService transportService;

    public BookingsController(ITransportService transportService)
    {
        this.transportService = transportService;
    }

    [HttpPost("lock-seats")]
    public ActionResult<SeatLockResponse> LockSeats([FromBody] LockSeatsRequest request)
    {
        try
        {
            return Ok(transportService.LockSeats(request));
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

    [HttpPost]
    public ActionResult<BookingResponse> Create([FromBody] CreateBookingRequest request)
    {
        try
        {
            return Ok(transportService.CreateBooking(request));
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

    [HttpGet("history/{userEmail}")]
    public ActionResult<IEnumerable<BookingResponse>> History([FromRoute] string userEmail)
    {
        return Ok(transportService.GetBookingsByUser(userEmail));
    }

    [HttpPost("{bookingId:guid}/cancel")]
    public ActionResult<CancelBookingResponse> Cancel([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        try
        {
            return Ok(transportService.CancelBooking(bookingId, userEmail));
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

    [HttpGet("{userEmail}/enhanced-history")]
    public ActionResult<IEnumerable<EnhancedBookingResponse>> GetEnhancedHistory([FromRoute] string userEmail, [FromQuery] BookingsHistoryFilter.HistoryType type = BookingsHistoryFilter.HistoryType.All)
    {
        var filter = new BookingsHistoryFilter { UserEmail = userEmail, Type = type };
        var bookings = transportService.GetBookingsHistory(filter);
        return Ok(bookings);
    }

    [HttpGet("{bookingId:guid}/enhanced")]
    public ActionResult<EnhancedBookingResponse> GetEnhanced([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        try
        {
            return Ok(transportService.GetEnhancedBooking(bookingId, userEmail));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{bookingId:guid}/ticket")]
    public ActionResult<TicketResponse> GetTicket([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        try
        {
            return Ok(transportService.GetTicket(bookingId, userEmail));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("payment/initiate")]
    public ActionResult<PaymentInitiateResponse> InitiatePayment([FromBody] PaymentInitiateRequest request)
    {
        try
        {
            return Ok(transportService.InitiatePayment(request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("payment/verify")]
    public ActionResult<PaymentVerifyResponse> VerifyPayment([FromBody] PaymentVerifyRequest request)
    {
        try
        {
            return Ok(transportService.VerifyPayment(request));
        }
        catch (Exception exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}