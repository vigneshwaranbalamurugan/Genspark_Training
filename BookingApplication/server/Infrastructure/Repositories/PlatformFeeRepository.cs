using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class PlatformFeeRepository : IPlatformFeeRepository
{
    private readonly DbConnectionFactory _factory;

    public PlatformFeeRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<PlatformFeeEntity?> GetActiveAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, amount, description, is_active, created_at
            FROM platform_fees
            WHERE is_active = TRUE
            ORDER BY created_at DESC
            LIMIT 1;", connection);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new PlatformFeeEntity
        {
            Id = reader.GetGuid(0),
            Amount = reader.GetDecimal(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            IsActive = reader.GetBoolean(3),
            CreatedAt = reader.GetDateTime(4)
        };
    }

    public async Task<decimal> GetActiveAmountAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT amount FROM platform_fees
            WHERE is_active = TRUE
            ORDER BY created_at DESC
            LIMIT 1;", connection);

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    public async Task<PlatformFeeEntity> CreateAsync(PlatformFeeEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO platform_fees (id, amount, description, is_active, created_at)
            VALUES (@id, @amount, @description, TRUE, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("amount", entity.Amount);
        command.Parameters.AddWithValue("description", entity.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task DeactivateAllAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE platform_fees SET is_active = FALSE WHERE is_active = TRUE;", connection);
        await command.ExecuteNonQueryAsync();
    }
}
