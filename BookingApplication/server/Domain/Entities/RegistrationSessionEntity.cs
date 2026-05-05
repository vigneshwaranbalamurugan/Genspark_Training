namespace server.Domain.Entities;

/// <summary>
/// Represents an active or completed user registration session.
/// </summary>
public sealed class RegistrationSessionEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime OtpExpiresAt { get; set; }
    public bool IsOtpVerified { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsPasswordSet => !string.IsNullOrWhiteSpace(PasswordHash);
    public bool IsProfileCompleted { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
