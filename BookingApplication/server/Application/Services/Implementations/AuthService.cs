using Microsoft.AspNetCore.Identity;
using server.Application.Services.Interfaces;
using server.Domain.Interfaces;
using server.Models;
using server.Services;

namespace server.Application.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IOperatorRepository _operatorRepo;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<string> _passwordHasher = new();

    public AuthService(
        IRegistrationRepository registrationRepo,
        IOperatorRepository operatorRepo,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _registrationRepo = registrationRepo;
        _operatorRepo = operatorRepo;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginUserAsync(CustomerLoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var session = await _registrationRepo.GetByEmailAsync(email);

        if (session is null)
            throw new KeyNotFoundException("User not found. Please register first.");

        if (string.IsNullOrWhiteSpace(session.PasswordHash))
            throw new InvalidOperationException("Password is not set. Complete registration first.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(email, session.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid password.");

        var fullName = BuildFullName(session.FirstName, session.LastName, email);
        
        var claims = new Dictionary<string, string>
        {
            { "userId", session.Id.ToString() }
        };

        var jwtToken = _jwtService.GenerateToken(email, UserRole.User.ToString(), claims);

        return new LoginResponse
        {
            UserId = session.Id,
            Email = email,
            FullName = fullName,
            JwtToken = jwtToken,
            Message = "Login successful"
        };
    }

    public async Task<OperatorLoginResponse> LoginOperatorAsync(OperatorLoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var operatorEntity = await _operatorRepo.GetByEmailAsync(email);

        if (operatorEntity is null)
            throw new KeyNotFoundException("Operator not found. Please register first.");

        if (operatorEntity.ApprovalStatus != OperatorApprovalStatus.Approved.ToString())
            throw new InvalidOperationException("Operator account not approved by admin yet.");

        if (operatorEntity.IsDisabled)
            throw new InvalidOperationException("Operator account is disabled.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(email, operatorEntity.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid password.");

        var claims = new Dictionary<string, string>
        {
            { "operatorId", operatorEntity.Id.ToString() }
        };

        var jwtToken = _jwtService.GenerateToken(email, UserRole.Operator.ToString(), claims);

        return new OperatorLoginResponse
        {
            OperatorId = operatorEntity.Id,
            CompanyName = operatorEntity.CompanyName,
            Email = email,
            JwtToken = jwtToken,
            ApprovalStatus = Enum.Parse<OperatorApprovalStatus>(operatorEntity.ApprovalStatus),
            IsDisabled = operatorEntity.IsDisabled,
            Message = "Login successful"
        };
    }

    public Task<AdminLoginResponse> LoginAdminAsync(AdminLoginRequest request)
    {
        var configuredEmail = _configuration["AdminCredentials:Email"] ?? "admin@system.com";
        var configuredPassword = _configuration["AdminCredentials:Password"] ?? "Admin@123";

        var normalizedInputEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedConfiguredEmail = configuredEmail.Trim().ToLowerInvariant();

        if (!string.Equals(normalizedInputEmail, normalizedConfiguredEmail, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Password, configuredPassword, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid admin credentials.");
        }

        var jwtToken = _jwtService.GenerateToken(normalizedConfiguredEmail, UserRole.Admin.ToString());

        return Task.FromResult(new AdminLoginResponse
        {
            Email = normalizedConfiguredEmail,
            JwtToken = jwtToken,
            Role = UserRole.Admin.ToString(),
            Message = "Admin login successful"
        });
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        return email.Trim().ToLowerInvariant();
    }

    private static string BuildFullName(string? firstName, string? lastName, string fallbackEmail)
    {
        var fullName = string.Join(" ", new[] { firstName, lastName }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? fallbackEmail : fullName;
    }
}
