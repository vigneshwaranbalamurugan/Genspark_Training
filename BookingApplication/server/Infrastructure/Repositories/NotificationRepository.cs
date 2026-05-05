using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly DbConnectionFactory _factory;

    public NotificationRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<NotificationEntity>> GetByRecipientAsync(string recipientEmail)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, recipient_email, subject, message, created_at
            FROM notifications
            WHERE recipient_email = @recipient_email
            ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("recipient_email", recipientEmail);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<NotificationEntity>();
        while (await reader.ReadAsync())
        {
            results.Add(new NotificationEntity
            {
                Id = reader.GetGuid(0),
                RecipientEmail = reader.GetString(1),
                Subject = reader.GetString(2),
                Message = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }
        return results;
    }

    public async Task CreateAsync(NotificationEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO notifications (id, recipient_email, subject, message, created_at)
            VALUES (@id, @recipient_email, @subject, @message, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("recipient_email", entity.RecipientEmail);
        command.Parameters.AddWithValue("subject", entity.Subject);
        command.Parameters.AddWithValue("message", entity.Message);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
    }
}
