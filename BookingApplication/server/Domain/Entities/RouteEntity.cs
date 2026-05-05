namespace server.Domain.Entities;

/// <summary>
/// Represents a route (source → destination) in the system.
/// </summary>
public sealed class RouteEntity
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
