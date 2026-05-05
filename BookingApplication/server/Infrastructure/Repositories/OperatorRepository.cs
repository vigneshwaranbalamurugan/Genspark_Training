using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class OperatorRepository : IOperatorRepository
{
    private readonly DbConnectionFactory _factory;

    public OperatorRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<OperatorEntity?> GetByIdAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, password_hash, approval_status, is_disabled
            FROM operators
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", operatorId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<OperatorEntity?> GetByEmailAsync(string email)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, password_hash, approval_status, is_disabled
            FROM operators
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<IEnumerable<OperatorEntity>> GetAllAsync()
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, password_hash, approval_status, is_disabled
            FROM operators
            ORDER BY created_at DESC;", connection);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<OperatorEntity>();
        while (await reader.ReadAsync())
            results.Add(MapEntity(reader));
        return results;
    }

    public async Task<OperatorEntity> CreateAsync(OperatorEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO operators (id, company_name, email, password_hash, approval_status, is_disabled, created_at)
            VALUES (@id, @company_name, @email, @password_hash, @approval_status, FALSE, @created_at);", connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("company_name", entity.CompanyName);
        command.Parameters.AddWithValue("email", entity.Email);
        command.Parameters.AddWithValue("password_hash", entity.PasswordHash);
        command.Parameters.AddWithValue("approval_status", entity.ApprovalStatus);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task UpdateApprovalStatusAsync(Guid operatorId, string status)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE operators SET approval_status = @approval_status WHERE id = @id;", connection);
        command.Parameters.AddWithValue("approval_status", status);
        command.Parameters.AddWithValue("id", operatorId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateDisabledStatusAsync(Guid operatorId, bool isDisabled)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE operators SET is_disabled = @is_disabled WHERE id = @id;", connection);
        command.Parameters.AddWithValue("is_disabled", isDisabled);
        command.Parameters.AddWithValue("id", operatorId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT 1 FROM operators WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);
        return await command.ExecuteScalarAsync() is not null;
    }

    private static OperatorEntity MapEntity(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        CompanyName = r.GetString(1),
        Email = r.GetString(2),
        PasswordHash = r.GetString(3),
        ApprovalStatus = r.GetString(4),
        IsDisabled = r.GetBoolean(5)
    };
}
