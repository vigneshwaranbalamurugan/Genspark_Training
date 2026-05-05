using Microsoft.AspNetCore.Identity;
using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class RegistrationService : IRegistrationService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private readonly IRegistrationRepository _registrationRepo;
    private readonly PasswordHasher<string> _passwordHasher = new();

    public RegistrationService(IRegistrationRepository registrationRepo)
    {
        _registrationRepo = registrationRepo;
    }

    public async Task<StartRegistrationResponse> StartRegistrationAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var otpCode = Random.Shared.Next(0, 1_000_000).ToString("D6");

        var existingSession = await _registrationRepo.GetByEmailAsync(normalizedEmail);
        if (existingSession is not null && existingSession.IsProfileCompleted)
        {
            throw new InvalidOperationException("An account already exists for this email.");
        }

        var now = DateTime.UtcNow;
        var entity = new RegistrationSessionEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            OtpCode = otpCode,
            OtpExpiresAt = now.Add(OtpLifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

        var saved = await _registrationRepo.UpsertAsync(entity);

        return new StartRegistrationResponse
        {
            Email = saved.Email,
            OtpExpiresAt = saved.OtpExpiresAt,
            Message = "OTP generated successfully. Verify it before setting a password.",
            DevelopmentOtp = otpCode
        };
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(string email, string otpCode)
    {
        var session = await GetExistingSessionAsync(email);

        if (session.OtpExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The OTP has expired. Request a new one.");
        }

        if (!string.Equals(session.OtpCode, otpCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The OTP is invalid.");
        }

        await _registrationRepo.UpdateOtpVerifiedAsync(session.Email);

        return new VerifyOtpResponse
        {
            Email = session.Email,
            Verified = true,
            Message = "OTP verified successfully. You can now set your password."
        };
    }

    public async Task<SetPasswordResponse> SetPasswordAsync(string email, string password)
    {
        var session = await GetExistingSessionAsync(email);

        if (!session.IsOtpVerified)
        {
            throw new InvalidOperationException("Verify the OTP before setting a password.");
        }

        var passwordHash = _passwordHasher.HashPassword(session.Email, password);
        await _registrationRepo.UpdatePasswordHashAsync(session.Email, passwordHash);

        return new SetPasswordResponse
        {
            Email = session.Email,
            PasswordSet = true,
            Message = "Password saved successfully. You can now submit personal details."
        };
    }

    public async Task<PersonalDetailsResponse> CompleteProfileAsync(PersonalDetailsRequest request)
    {
        var session = await GetExistingSessionAsync(request.Email);

        if (!session.IsOtpVerified || !session.IsPasswordSet)
        {
            throw new InvalidOperationException("Complete OTP verification and password setup before personal details.");
        }

        session.FirstName = request.FirstName.Trim();
        session.LastName = request.LastName.Trim();
        session.PhoneNumber = request.PhoneNumber.Trim();
        session.Gender = request.Gender.Trim();
        session.Age = request.Age;
        session.DateOfBirth = request.DateOfBirth;
        session.UpdatedAt = DateTime.UtcNow;

        await _registrationRepo.UpdateProfileAsync(session);

        return new PersonalDetailsResponse
        {
            Email = session.Email,
            ProfileCompleted = true,
            Message = "Registration completed successfully."
        };
    }

    public async Task<RegistrationStatusResponse?> GetStatusAsync(string email)
    {
        var session = await _registrationRepo.GetByEmailAsync(NormalizeEmail(email));
        if (session is null) return null;

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

    private async Task<RegistrationSessionEntity> GetExistingSessionAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var session = await _registrationRepo.GetByEmailAsync(normalizedEmail);
        if (session is null)
        {
            throw new KeyNotFoundException("No registration session exists for the specified email.");
        }
        return session;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        return email.Trim().ToLowerInvariant();
    }
}
