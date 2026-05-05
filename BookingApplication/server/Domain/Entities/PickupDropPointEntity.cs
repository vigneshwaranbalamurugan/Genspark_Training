namespace server.Domain.Entities;

/// <summary>
/// Represents a pickup or drop point for an operator's preferred route.
/// </summary>
public sealed class PickupDropPointEntity
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public Guid RouteId { get; set; }
    public bool IsPickup { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
