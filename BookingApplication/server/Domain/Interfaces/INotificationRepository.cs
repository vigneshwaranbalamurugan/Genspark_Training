using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for notifications.
/// </summary>
public interface INotificationRepository
{
    Task<IEnumerable<NotificationEntity>> GetByRecipientAsync(string recipientEmail);
    Task CreateAsync(NotificationEntity entity);
}
