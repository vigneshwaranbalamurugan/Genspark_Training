using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles booking lifecycle: lock seats, create, cancel, history, tickets.
/// </summary>
public interface IBookingService
{
    Task<SeatLockResponse> LockSeatsAsync(LockSeatsRequest request);
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request);
    Task<CancelBookingResponse> CancelBookingAsync(Guid bookingId, string userEmail);
    Task<IEnumerable<BookingResponse>> GetBookingsByUserAsync(string userEmail);
    Task<EnhancedBookingResponse> GetEnhancedBookingAsync(Guid bookingId, string userEmail);
    Task<IEnumerable<EnhancedBookingResponse>> GetBookingsHistoryAsync(BookingsHistoryFilter filter);
    Task<TicketResponse> GetTicketAsync(Guid bookingId, string userEmail);
    Task<(byte[] Content, string FileName)> GenerateTicketFileAsync(Guid bookingId, string userEmail);
    Task<PaymentInitiateResponse> InitiatePaymentAsync(PaymentInitiateRequest request);
    Task<PaymentVerifyResponse> VerifyPaymentAsync(PaymentVerifyRequest request);
}
