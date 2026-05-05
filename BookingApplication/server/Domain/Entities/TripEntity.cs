namespace server.Domain.Entities;

/// <summary>
/// Represents a scheduled trip (a bus running on a route at a specific time).
/// </summary>
public sealed class TripEntity
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public Guid BusId { get; set; }
    public Guid RouteId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PlatformFee { get; set; }
    public bool IsVariablePrice { get; set; }
    public string? PickupPoints { get; set; }
    public string? DropPoints { get; set; }
    public string TripType { get; set; } = "OneTime";
    public string? DaysOfWeek { get; set; }
    public bool IsActive { get; set; }
    public int ArrivalDayOffset { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? DepartureTimeOnly { get; set; }
    public TimeOnly? ArrivalTimeOnly { get; set; }
    public DateTime CreatedAt { get; set; }
}
