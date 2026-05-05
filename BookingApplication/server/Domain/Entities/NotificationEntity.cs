namespace server.Domain.Entities;

/// <summary>
/// Represents an in-app notification sent to a user or operator.
/// </summary>
public sealed class NotificationEntity
{
    public Guid Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
