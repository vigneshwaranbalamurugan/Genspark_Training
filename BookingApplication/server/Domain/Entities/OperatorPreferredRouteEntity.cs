namespace server.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between operators and their preferred routes.
/// </summary>
public sealed class OperatorPreferredRouteEntity
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public Guid RouteId { get; set; }
    public DateTime CreatedAt { get; set; }
}
