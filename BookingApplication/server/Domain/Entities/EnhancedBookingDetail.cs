namespace server.Domain.Entities;

/// <summary>
/// Domain model for enhanced booking details including joined trip, route, and bus info.
/// </summary>
public sealed class EnhancedBookingDetail
{
    public BookingEntity Booking { get; set; } = null!;
    public TripEntity Trip { get; set; } = null!;
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public List<BookingPassengerEntity> Passengers { get; set; } = new();
}
