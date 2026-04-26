using System.Security.Claims;

namespace server.Services;

public interface IJwtService
{
    string GenerateToken(string email, string role, Dictionary<string, string>? additionalClaims = null);
    ClaimsPrincipal? ValidateToken(string token);
}
