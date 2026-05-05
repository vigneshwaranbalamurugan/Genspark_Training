namespace server.Domain.Entities;

/// <summary>
/// Represents a temporary seat lock held by a user before booking.
/// </summary>
public sealed class SeatLockEntity
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public DateOnly TravelDate { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int[] SeatNumbers { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
