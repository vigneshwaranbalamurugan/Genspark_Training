using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class PreferredRouteRepository : IPreferredRouteRepository
{
    private readonly DbConnectionFactory _factory;

    public PreferredRouteRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> ExistsAsync(Guid operatorId, Guid routeId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT 1 FROM operator_preferred_routes
            WHERE operator_id = @operator_id AND route_id = @route_id;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("route_id", routeId);
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task CreateAsync(OperatorPreferredRouteEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO operator_preferred_routes (id, operator_id, route_id, created_at)
            VALUES (@id, @operator_id, @route_id, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("operator_id", entity.OperatorId);
        command.Parameters.AddWithValue("route_id", entity.RouteId);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<RouteEntity>> GetPreferredRoutesAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT r.id, r.source, r.destination
            FROM routes r
            JOIN operator_preferred_routes opr ON opr.route_id = r.id
            WHERE opr.operator_id = @operator_id;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        await using var reader = await command.ExecuteReaderAsync();
        var routes = new List<RouteEntity>();
        while (await reader.ReadAsync())
            routes.Add(new RouteEntity { Id = reader.GetGuid(0), Source = reader.GetString(1), Destination = reader.GetString(2) });
        return routes;
    }

    public async Task<PickupDropPointEntity> UpsertPointAsync(PickupDropPointEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO pickup_drop_points (id, operator_id, route_id, is_pickup, location, address, is_default, created_at)
            VALUES (@id, @operator_id, @route_id, @is_pickup, @location, @address, @is_default, @created_at)
            ON CONFLICT (operator_id, route_id, is_pickup, location)
            DO UPDATE SET address = EXCLUDED.address, created_at = EXCLUDED.created_at
            RETURNING id;", connection);

        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("operator_id", entity.OperatorId);
        command.Parameters.AddWithValue("route_id", entity.RouteId);
        command.Parameters.AddWithValue("is_pickup", entity.IsPickup);
        command.Parameters.AddWithValue("location", entity.Location);
        command.Parameters.AddWithValue("address", entity.Address ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("is_default", entity.IsDefault);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);

        var result = await command.ExecuteScalarAsync();
        entity.Id = result is not null ? (Guid)result : entity.Id;
        return entity;
    }

    public async Task<IEnumerable<PickupDropPointEntity>> GetPointsAsync(Guid operatorId, Guid routeId, bool isPickup)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, route_id, is_pickup, location, address, is_default
            FROM pickup_drop_points
            WHERE operator_id = @operator_id AND route_id = @route_id AND is_pickup = @is_pickup
            ORDER BY created_at ASC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("route_id", routeId);
        command.Parameters.AddWithValue("is_pickup", isPickup);

        return await ReadPointsAsync(command);
    }

    public async Task<IEnumerable<PickupDropPointEntity>> GetAllPointsByOperatorAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, route_id, is_pickup, location, address, is_default
            FROM pickup_drop_points
            WHERE operator_id = @operator_id;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        return await ReadPointsAsync(command);
    }

    private static async Task<List<PickupDropPointEntity>> ReadPointsAsync(NpgsqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var points = new List<PickupDropPointEntity>();
        while (await reader.ReadAsync())
        {
            points.Add(new PickupDropPointEntity
            {
                Id = reader.GetGuid(0),
                OperatorId = reader.GetGuid(1),
                RouteId = reader.GetGuid(2),
                IsPickup = reader.GetBoolean(3),
                Location = reader.GetString(4),
                Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsDefault = reader.GetBoolean(6)
            });
        }
        return points;
    }
}
