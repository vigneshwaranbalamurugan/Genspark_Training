using System.ComponentModel.DataAnnotations;

namespace server.Models;

public enum UserRole
{
    Guest = 0,
    User = 1,
    Operator = 2,
    Admin = 3
}

public sealed class RegistrationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Email { get; set; }

    public required string OtpCode { get; set; }

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

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class StartRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class StartRegistrationResponse
{
    public required string Email { get; set; }

    public required DateTime OtpExpiresAt { get; set; }

    public required string Message { get; set; }

    public string? DevelopmentOtp { get; set; }
}

public sealed class VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}

public sealed class VerifyOtpResponse
{
    public required string Email { get; set; }

    public required bool Verified { get; set; }

    public required string Message { get; set; }
}

public sealed class SetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class SetPasswordResponse
{
    public required string Email { get; set; }

    public required bool PasswordSet { get; set; }

    public required string Message { get; set; }

    public string? AccessToken { get; set; }

    public string? TokenType { get; set; } = "Bearer";

    public int? ExpiresIn { get; set; } = 86400; // 24 hours

    public string? Role { get; set; }
}

public sealed class PersonalDetailsRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [Range(1, 120)]
    public int Age { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}

public sealed class PersonalDetailsResponse
{
    public required string Email { get; set; }

    public required bool ProfileCompleted { get; set; }

    public required string Message { get; set; }
}

public sealed class RegistrationStatusResponse
{
    public required string Email { get; set; }

    public required bool OtpVerified { get; set; }

    public required bool PasswordSet { get; set; }

    public required bool ProfileCompleted { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    public int? Age { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}

public sealed class AuthResponse
{
    public required string Email { get; set; }

    public required string AccessToken { get; set; }

    public required string TokenType { get; set; } = "Bearer";

    public required int ExpiresIn { get; set; } = 86400; // 24 hours

    public required string Role { get; set; }

    public required bool ProfileCompleted { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}