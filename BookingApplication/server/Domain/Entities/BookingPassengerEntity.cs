namespace server.Domain.Entities;

/// <summary>
/// Represents a passenger on a specific booking.
/// </summary>
public sealed class BookingPassengerEntity
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public int SeatNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
}
