using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class BusRepository : IBusRepository
{
    private readonly DbConnectionFactory _factory;

    public BusRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<BusEntity?> GetByIdAsync(Guid busId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, layout_name, layout_json,
                   is_temporarily_unavailable, is_approved, is_active
            FROM buses WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", busId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<IEnumerable<BusEntity>> GetAllAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, layout_name, layout_json,
                   is_temporarily_unavailable, is_approved, is_active
            FROM buses ORDER BY created_at DESC;", connection);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<BusEntity>();
        while (await reader.ReadAsync()) results.Add(MapEntity(reader));
        return results;
    }

    public async Task<IEnumerable<BusEntity>> GetByOperatorIdAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, layout_name, layout_json,
                   is_temporarily_unavailable, is_approved, is_active
            FROM buses WHERE operator_id = @operator_id ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<BusEntity>();
        while (await reader.ReadAsync()) results.Add(MapEntity(reader));
        return results;
    }

    public async Task<BusEntity> CreateAsync(BusEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO buses
            (id, operator_id, bus_name, bus_number, capacity, layout_name, layout_json,
             is_temporarily_unavailable, is_approved, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_name, @bus_number, @capacity, @layout_name, @layout_json,
             FALSE, FALSE, TRUE, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("operator_id", entity.OperatorId);
        command.Parameters.AddWithValue("bus_name", entity.BusName);
        command.Parameters.AddWithValue("bus_number", entity.BusNumber);
        command.Parameters.AddWithValue("capacity", entity.Capacity);
        command.Parameters.AddWithValue("layout_name", entity.LayoutName);
        command.Parameters.AddWithValue("layout_json", entity.LayoutJson ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task UpdateApprovalAsync(Guid busId, bool isApproved)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE buses SET is_approved = @is_approved WHERE id = @id;", connection);
        command.Parameters.AddWithValue("is_approved", isApproved);
        command.Parameters.AddWithValue("id", busId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateTemporaryAvailabilityAsync(Guid busId, bool isUnavailable)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE buses SET is_temporarily_unavailable = @unavailable WHERE id = @id;", connection);
        command.Parameters.AddWithValue("unavailable", isUnavailable);
        command.Parameters.AddWithValue("id", busId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeactivateAsync(Guid busId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE buses SET is_active = FALSE WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", busId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsByBusNumberAsync(string busNumber)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT 1 FROM buses WHERE bus_number = @bus_number;", connection);
        command.Parameters.AddWithValue("bus_number", busNumber);
        return await command.ExecuteScalarAsync() is not null;
    }

    private static BusEntity MapEntity(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        OperatorId = r.GetGuid(1),
        BusNumber = r.IsDBNull(2) ? string.Empty : r.GetString(2),
        BusName = r.GetString(3),
        Capacity = r.GetInt32(4),
        LayoutName = r.GetString(5),
        LayoutJson = r.IsDBNull(6) ? null : r.GetString(6),
        IsTemporarilyUnavailable = r.GetBoolean(7),
        IsApproved = r.GetBoolean(8),
        IsActive = r.GetBoolean(9)
    };
}
