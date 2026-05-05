namespace server.Domain.Entities;

/// <summary>
/// Represents a bus operator company in the system.
/// </summary>
public sealed class OperatorEntity
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Pending";
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
