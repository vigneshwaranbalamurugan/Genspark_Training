using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class SeatLockRepository : ISeatLockRepository
{
    private readonly DbConnectionFactory _factory;

    public SeatLockRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<SeatLockEntity?> GetByIdAsync(Guid lockId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, trip_id, travel_date, user_email, seat_numbers, expires_at
            FROM seat_locks WHERE id = @lock_id;", connection);
        command.Parameters.AddWithValue("lock_id", lockId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new SeatLockEntity
        {
            Id = reader.GetGuid(0),
            TripId = reader.GetGuid(1),
            TravelDate = reader.GetFieldValue<DateOnly>(2),
            UserEmail = reader.GetString(3),
            SeatNumbers = reader.GetFieldValue<int[]>(4),
            ExpiresAt = reader.GetDateTime(5)
        };
    }

    public async Task<SeatLockEntity> CreateAsync(SeatLockEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO seat_locks (id, trip_id, travel_date, user_email, seat_numbers, expires_at, created_at)
            VALUES (@id, @trip_id, @travel_date, @user_email, @seat_numbers, @expires_at, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("trip_id", entity.TripId);
        command.Parameters.AddWithValue("travel_date", entity.TravelDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("user_email", entity.UserEmail);
        command.Parameters.AddWithValue("seat_numbers", entity.SeatNumbers);
        command.Parameters.AddWithValue("expires_at", entity.ExpiresAt);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task DeleteAsync(Guid lockId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            DELETE FROM seat_locks WHERE id = @lock_id;", connection);
        command.Parameters.AddWithValue("lock_id", lockId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            DELETE FROM seat_locks WHERE expires_at <= @now;", connection);
        command.Parameters.AddWithValue("now", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<HashSet<int>> GetLockedSeatNumbersAsync(Guid tripId, DateOnly travelDate)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT seat_numbers FROM seat_locks
            WHERE trip_id = @trip_id AND travel_date = @travel_date AND expires_at > @now;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);
        command.Parameters.AddWithValue("travel_date", travelDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("now", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();
        var locked = new HashSet<int>();
        while (await reader.ReadAsync())
            foreach (var seat in reader.GetFieldValue<int[]>(0))
                locked.Add(seat);
        return locked;
    }
}
