using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class RouteRepository : IRouteRepository
{
    private readonly DbConnectionFactory _factory;

    public RouteRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<RouteEntity?> GetByIdAsync(Guid routeId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, source, destination FROM routes WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", routeId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new RouteEntity { Id = reader.GetGuid(0), Source = reader.GetString(1), Destination = reader.GetString(2) };
    }

    public async Task<IEnumerable<RouteEntity>> GetAllAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, source, destination FROM routes ORDER BY source, destination;", connection);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<RouteEntity>();
        while (await reader.ReadAsync())
            results.Add(new RouteEntity { Id = reader.GetGuid(0), Source = reader.GetString(1), Destination = reader.GetString(2) });
        return results;
    }

    public async Task<RouteEntity> CreateAsync(RouteEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO routes (id, source, destination, created_at)
            VALUES (@id, @source, @destination, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("source", entity.Source);
        command.Parameters.AddWithValue("destination", entity.Destination);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task<bool> ExistsAsync(string source, string destination)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id FROM routes WHERE source = @source AND destination = @destination;", connection);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("destination", destination);
        return await command.ExecuteScalarAsync() is not null;
    }
}
