using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly DbConnectionFactory _factory;

    public RegistrationRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<RegistrationSessionEntity?> GetByEmailAsync(string email)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, email, otp_code, otp_expires_at, is_otp_verified, password_hash,
                   is_profile_completed, first_name, last_name, phone_number, gender, age,
                   date_of_birth, created_at, updated_at
            FROM registration_sessions
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<RegistrationSessionEntity> UpsertAsync(RegistrationSessionEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO registration_sessions
            (id, email, otp_code, otp_expires_at, is_otp_verified, password_hash, is_profile_completed,
             first_name, last_name, phone_number, gender, age, date_of_birth, created_at, updated_at)
            VALUES
            (@id, @email, @otp_code, @otp_expires_at, FALSE, NULL, FALSE,
             NULL, NULL, NULL, NULL, NULL, NULL, @created_at, @updated_at)
            ON CONFLICT (email)
            DO UPDATE SET
                otp_code           = EXCLUDED.otp_code,
                otp_expires_at     = EXCLUDED.otp_expires_at,
                is_otp_verified    = FALSE,
                password_hash      = NULL,
                is_profile_completed = FALSE,
                first_name         = NULL,
                last_name          = NULL,
                phone_number       = NULL,
                gender             = NULL,
                age                = NULL,
                date_of_birth      = NULL,
                updated_at         = EXCLUDED.updated_at
            RETURNING id, email, otp_expires_at;", connection);

        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("email", entity.Email);
        command.Parameters.AddWithValue("otp_code", entity.OtpCode);
        command.Parameters.AddWithValue("otp_expires_at", entity.OtpExpiresAt);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        command.Parameters.AddWithValue("updated_at", entity.UpdatedAt);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        entity.Email = reader.GetString(reader.GetOrdinal("email"));
        entity.OtpExpiresAt = reader.GetDateTime(reader.GetOrdinal("otp_expires_at"));
        return entity;
    }

    public async Task UpdateOtpVerifiedAsync(string email)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET is_otp_verified = TRUE, updated_at = @updated_at
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("email", email);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePasswordHashAsync(string email, string passwordHash)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET password_hash = @password_hash, updated_at = @updated_at
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("password_hash", passwordHash);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("email", email);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateProfileAsync(RegistrationSessionEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET first_name           = @first_name,
                last_name            = @last_name,
                phone_number         = @phone_number,
                gender               = @gender,
                age                  = @age,
                date_of_birth        = @date_of_birth,
                is_profile_completed = TRUE,
                updated_at           = @updated_at
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("first_name", entity.FirstName!);
        command.Parameters.AddWithValue("last_name", entity.LastName!);
        command.Parameters.AddWithValue("phone_number", entity.PhoneNumber!);
        command.Parameters.AddWithValue("gender", entity.Gender!);
        command.Parameters.AddWithValue("age", entity.Age!);
        command.Parameters.AddWithValue("date_of_birth",
            entity.DateOfBirth is null ? DBNull.Value : entity.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("updated_at", entity.UpdatedAt);
        command.Parameters.AddWithValue("email", entity.Email);
        await command.ExecuteNonQueryAsync();
    }

    private static RegistrationSessionEntity MapEntity(NpgsqlDataReader r)
    {
        var dobIdx = r.GetOrdinal("date_of_birth");
        var ageIdx = r.GetOrdinal("age");
        return new RegistrationSessionEntity
        {
            Id = r.GetGuid(r.GetOrdinal("id")),
            Email = r.GetString(r.GetOrdinal("email")),
            OtpCode = r.GetString(r.GetOrdinal("otp_code")),
            OtpExpiresAt = r.GetDateTime(r.GetOrdinal("otp_expires_at")),
            IsOtpVerified = r.GetBoolean(r.GetOrdinal("is_otp_verified")),
            PasswordHash = r.IsDBNull(r.GetOrdinal("password_hash")) ? null : r.GetString(r.GetOrdinal("password_hash")),
            IsProfileCompleted = r.GetBoolean(r.GetOrdinal("is_profile_completed")),
            FirstName = r.IsDBNull(r.GetOrdinal("first_name")) ? null : r.GetString(r.GetOrdinal("first_name")),
            LastName = r.IsDBNull(r.GetOrdinal("last_name")) ? null : r.GetString(r.GetOrdinal("last_name")),
            PhoneNumber = r.IsDBNull(r.GetOrdinal("phone_number")) ? null : r.GetString(r.GetOrdinal("phone_number")),
            Gender = r.IsDBNull(r.GetOrdinal("gender")) ? null : r.GetString(r.GetOrdinal("gender")),
            Age = r.IsDBNull(ageIdx) ? null : r.GetInt32(ageIdx),
            DateOfBirth = r.IsDBNull(dobIdx) ? null : DateOnly.FromDateTime(r.GetDateTime(dobIdx)),
            CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
            UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at"))
        };
    }
}
