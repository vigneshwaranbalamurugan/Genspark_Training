namespace server.Domain.Entities;

/// <summary>
/// Represents a bus owned by an operator.
/// </summary>
public sealed class BusEntity
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string LayoutName { get; set; } = string.Empty;
    public string? LayoutJson { get; set; }
    public bool IsTemporarilyUnavailable { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
