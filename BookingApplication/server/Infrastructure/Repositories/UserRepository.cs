using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _factory;

    public UserRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, email, full_name, sso_provider
            FROM users
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new UserEntity
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            FullName = reader.GetString(2),
            SsoProvider = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }

    public async Task<UserEntity> UpsertAsync(UserEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO users (id, email, full_name, sso_provider, created_at, updated_at)
            VALUES (@id, @email, @full_name, @sso_provider, @created_at, @updated_at)
            ON CONFLICT (email)
            DO UPDATE SET
                full_name    = EXCLUDED.full_name,
                sso_provider = EXCLUDED.sso_provider,
                updated_at   = EXCLUDED.updated_at
            RETURNING id, email, full_name, sso_provider;", connection);

        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("email", entity.Email);
        command.Parameters.AddWithValue("full_name", entity.FullName);
        command.Parameters.AddWithValue("sso_provider", entity.SsoProvider ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        command.Parameters.AddWithValue("updated_at", entity.UpdatedAt);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new UserEntity
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            FullName = reader.GetString(2),
            SsoProvider = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }
}
