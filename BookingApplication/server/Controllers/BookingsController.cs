using Microsoft.AspNetCore.Mvc;
using server.Application.Services.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("lock-seats")]
    public async Task<ActionResult<SeatLockResponse>> LockSeats([FromBody] LockSeatsRequest request)
    {
        return Ok(await _bookingService.LockSeatsAsync(request));
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create([FromBody] CreateBookingRequest request)
    {
        return Ok(await _bookingService.CreateBookingAsync(request));
    }

    [HttpGet("history/{userEmail}")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> History([FromRoute] string userEmail)
    {
        return Ok(await _bookingService.GetBookingsByUserAsync(userEmail));
    }

    [HttpPost("{bookingId:guid}/cancel")]
    public async Task<ActionResult<CancelBookingResponse>> Cancel([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        return Ok(await _bookingService.CancelBookingAsync(bookingId, userEmail));
    }

    [HttpGet("{userEmail}/enhanced-history")]
    public async Task<ActionResult<IEnumerable<EnhancedBookingResponse>>> GetEnhancedHistory([FromRoute] string userEmail, [FromQuery] BookingsHistoryFilter.HistoryType type = BookingsHistoryFilter.HistoryType.All)
    {
        var filter = new BookingsHistoryFilter { UserEmail = userEmail, Type = type };
        var bookings = await _bookingService.GetBookingsHistoryAsync(filter);
        return Ok(bookings);
    }

    [HttpGet("{bookingId:guid}/enhanced")]
    public async Task<ActionResult<EnhancedBookingResponse>> GetEnhanced([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        return Ok(await _bookingService.GetEnhancedBookingAsync(bookingId, userEmail));
    }

    [HttpGet("{bookingId:guid}/ticket")]
    public async Task<ActionResult<TicketResponse>> GetTicket([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        return Ok(await _bookingService.GetTicketAsync(bookingId, userEmail));
    }

    [HttpGet("{bookingId:guid}/ticket/download")]
    public async Task<IActionResult> DownloadTicket([FromRoute] Guid bookingId, [FromQuery] string userEmail)
    {
        var (content, fileName) = await _bookingService.GenerateTicketFileAsync(bookingId, userEmail);
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        return File(content, "application/pdf");
    }

    [HttpPost("payment/initiate")]
    public async Task<ActionResult<PaymentInitiateResponse>> InitiatePayment([FromBody] PaymentInitiateRequest request)
    {
        return Ok(await _bookingService.InitiatePaymentAsync(request));
    }

    [HttpPost("payment/verify")]
    public async Task<ActionResult<PaymentVerifyResponse>> VerifyPayment([FromBody] PaymentVerifyRequest request)
    {
        return Ok(await _bookingService.VerifyPaymentAsync(request));
    }
}