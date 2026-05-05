namespace server.Domain.Entities;

/// <summary>
/// Domain model for trip search results including joined bus, route, and operator info.
/// </summary>
public sealed class TripDetail
{
    public TripEntity Trip { get; set; } = null!;
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public int BusCapacity { get; set; }
    public string LayoutName { get; set; } = string.Empty;
    public string? LayoutJson { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
