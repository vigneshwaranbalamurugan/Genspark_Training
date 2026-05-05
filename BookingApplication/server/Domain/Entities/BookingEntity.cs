namespace server.Domain.Entities;

/// <summary>
/// Represents a booking made by a customer for a trip.
/// </summary>
public sealed class BookingEntity
{
    public Guid Id { get; set; }
    public string Pnr { get; set; } = string.Empty;
    public Guid TripId { get; set; }
    public DateOnly TravelDate { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int[] SeatNumbers { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string TicketDownloadUrl { get; set; } = string.Empty;
    public string MailStatus { get; set; } = string.Empty;
    public bool IsCancelled { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
