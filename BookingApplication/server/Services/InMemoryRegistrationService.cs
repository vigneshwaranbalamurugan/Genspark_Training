using Microsoft.AspNetCore.Identity;
using Npgsql;
using server.Models;

namespace server.Services;

public sealed class InMemoryRegistrationService : IRegistrationService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private readonly PasswordHasher<string> passwordHasher = new();
    private readonly string connectionString;

    public InMemoryRegistrationService(string connectionString)
    {
        this.connectionString = connectionString;
        EnsureSchema();
    }

    public StartRegistrationResponse StartRegistration(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var otpCode = Random.Shared.Next(0, 1_000_000).ToString("D6");

        var existingSession = GetSessionByEmail(normalizedEmail);
        if (existingSession is not null && existingSession.IsProfileCompleted)
        {
            throw new InvalidOperationException("An account already exists for this email.");
        }

        var now = DateTime.UtcNow;
        var otpExpiry = now.Add(OtpLifetime);

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(@"
            INSERT INTO registration_sessions
            (id, email, otp_code, otp_expires_at, is_otp_verified, password_hash, is_profile_completed,
             first_name, last_name, phone_number, gender, age, date_of_birth, created_at, updated_at)
            VALUES
            (@id, @email, @otp_code, @otp_expires_at, FALSE, NULL, FALSE,
             NULL, NULL, NULL, NULL, NULL, NULL, @created_at, @updated_at)
            ON CONFLICT (email)
            DO UPDATE SET
                otp_code = EXCLUDED.otp_code,
                otp_expires_at = EXCLUDED.otp_expires_at,
                is_otp_verified = FALSE,
                password_hash = NULL,
                is_profile_completed = FALSE,
                first_name = NULL,
                last_name = NULL,
                phone_number = NULL,
                gender = NULL,
                age = NULL,
                date_of_birth = NULL,
                updated_at = EXCLUDED.updated_at
            RETURNING id, email, otp_expires_at;", connection);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("email", normalizedEmail);
        command.Parameters.AddWithValue("otp_code", otpCode);
        command.Parameters.AddWithValue("otp_expires_at", otpExpiry);
        command.Parameters.AddWithValue("created_at", now);
        command.Parameters.AddWithValue("updated_at", now);

        using var reader = command.ExecuteReader();
        reader.Read();

        var savedEmail = reader.GetString(reader.GetOrdinal("email"));
        var savedExpiry = reader.GetDateTime(reader.GetOrdinal("otp_expires_at"));

        return new StartRegistrationResponse
        {
            Email = savedEmail,
            OtpExpiresAt = savedExpiry,
            Message = "OTP generated successfully. Verify it before setting a password.",
            DevelopmentOtp = otpCode
        };
    }

    public VerifyOtpResponse VerifyOtp(string email, string otpCode)
    {
        var session = GetExistingSession(email);

        if (session.OtpExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The OTP has expired. Request a new one.");
        }

        if (!string.Equals(session.OtpCode, otpCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The OTP is invalid.");
        }

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var updateCommand = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET is_otp_verified = TRUE,
                updated_at = @updated_at
            WHERE email = @email;", connection);

        updateCommand.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        updateCommand.Parameters.AddWithValue("email", session.Email);
        updateCommand.ExecuteNonQuery();

        return new VerifyOtpResponse
        {
            Email = session.Email,
            Verified = true,
            Message = "OTP verified successfully. You can now set your password."
        };
    }

    public SetPasswordResponse SetPassword(string email, string password)
    {
        var session = GetExistingSession(email);

        if (!session.IsOtpVerified)
        {
            throw new InvalidOperationException("Verify the OTP before setting a password.");
        }

        var passwordHash = passwordHasher.HashPassword(session.Email, password);

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var updateCommand = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET password_hash = @password_hash,
                updated_at = @updated_at
            WHERE email = @email;", connection);

        updateCommand.Parameters.AddWithValue("password_hash", passwordHash);
        updateCommand.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        updateCommand.Parameters.AddWithValue("email", session.Email);
        updateCommand.ExecuteNonQuery();

        return new SetPasswordResponse
        {
            Email = session.Email,
            PasswordSet = true,
            Message = "Password saved successfully. You can now submit personal details."
        };
    }

    public PersonalDetailsResponse CompleteProfile(PersonalDetailsRequest request)
    {
        var session = GetExistingSession(request.Email);

        if (!session.IsOtpVerified || !session.IsPasswordSet)
        {
            throw new InvalidOperationException("Complete OTP verification and password setup before personal details.");
        }

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var updateCommand = new NpgsqlCommand(@"
            UPDATE registration_sessions
            SET first_name = @first_name,
                last_name = @last_name,
                phone_number = @phone_number,
                gender = @gender,
                age = @age,
                date_of_birth = @date_of_birth,
                is_profile_completed = TRUE,
                updated_at = @updated_at
            WHERE email = @email;", connection);

        updateCommand.Parameters.AddWithValue("first_name", request.FirstName.Trim());
        updateCommand.Parameters.AddWithValue("last_name", request.LastName.Trim());
        updateCommand.Parameters.AddWithValue("phone_number", request.PhoneNumber.Trim());
        updateCommand.Parameters.AddWithValue("gender", request.Gender.Trim());
        updateCommand.Parameters.AddWithValue("age", request.Age);
        updateCommand.Parameters.AddWithValue("date_of_birth", request.DateOfBirth is null ? DBNull.Value : request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue));
        updateCommand.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        updateCommand.Parameters.AddWithValue("email", session.Email);
        updateCommand.ExecuteNonQuery();

        return new PersonalDetailsResponse
        {
            Email = session.Email,
            ProfileCompleted = true,
            Message = "Registration completed successfully."
        };
    }

    public RegistrationStatusResponse? GetStatus(string email)
    {
        var session = GetSessionByEmail(NormalizeEmail(email));
        if (session is null)
        {
            return null;
        }

        return new RegistrationStatusResponse
        {
            Email = session.Email,
            OtpVerified = session.IsOtpVerified,
            PasswordSet = session.IsPasswordSet,
            ProfileCompleted = session.IsProfileCompleted,
            FirstName = session.FirstName,
            LastName = session.LastName,
            PhoneNumber = session.PhoneNumber,
            Gender = session.Gender,
            Age = session.Age,
            DateOfBirth = session.DateOfBirth
        };
    }

    private RegistrationSession GetExistingSession(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        var session = GetSessionByEmail(normalizedEmail);
        if (session is null)
        {
            throw new KeyNotFoundException("No registration session exists for the specified email.");
        }

        return session;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private RegistrationSession? GetSessionByEmail(string normalizedEmail)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(@"
            SELECT id, email, otp_code, otp_expires_at, is_otp_verified, password_hash,
                   is_profile_completed, first_name, last_name, phone_number, gender, age,
                   date_of_birth, created_at, updated_at
            FROM registration_sessions
            WHERE email = @email;", connection);

        command.Parameters.AddWithValue("email", normalizedEmail);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var dateOfBirthIndex = reader.GetOrdinal("date_of_birth");
        var ageIndex = reader.GetOrdinal("age");
        return new RegistrationSession
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            OtpCode = reader.GetString(reader.GetOrdinal("otp_code")),
            OtpExpiresAt = reader.GetDateTime(reader.GetOrdinal("otp_expires_at")),
            IsOtpVerified = reader.GetBoolean(reader.GetOrdinal("is_otp_verified")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("password_hash")) ? null : reader.GetString(reader.GetOrdinal("password_hash")),
            IsProfileCompleted = reader.GetBoolean(reader.GetOrdinal("is_profile_completed")),
            FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phone_number")) ? null : reader.GetString(reader.GetOrdinal("phone_number")),
            Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? null : reader.GetString(reader.GetOrdinal("gender")),
            Age = reader.IsDBNull(ageIndex) ? null : reader.GetInt32(ageIndex),
            DateOfBirth = reader.IsDBNull(dateOfBirthIndex) ? null : DateOnly.FromDateTime(reader.GetDateTime(dateOfBirthIndex)),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private void EnsureSchema()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS registration_sessions (
                id UUID PRIMARY KEY,
                email TEXT NOT NULL UNIQUE,
                otp_code TEXT NOT NULL,
                otp_expires_at TIMESTAMPTZ NOT NULL,
                is_otp_verified BOOLEAN NOT NULL,
                password_hash TEXT NULL,
                is_profile_completed BOOLEAN NOT NULL,
                first_name TEXT NULL,
                last_name TEXT NULL,
                phone_number TEXT NULL,
                gender TEXT NULL,
                age INTEGER NULL,
                date_of_birth TIMESTAMPTZ NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            ALTER TABLE registration_sessions ADD COLUMN IF NOT EXISTS gender TEXT NULL;
            ALTER TABLE registration_sessions ADD COLUMN IF NOT EXISTS age INTEGER NULL;
            ALTER TABLE registration_sessions ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ NULL;", connection);

        command.ExecuteNonQuery();
    }
}