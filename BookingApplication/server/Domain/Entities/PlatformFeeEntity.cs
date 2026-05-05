namespace server.Domain.Entities;

/// <summary>
/// Represents a platform fee configuration set by admin.
/// </summary>
public sealed class PlatformFeeEntity
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
