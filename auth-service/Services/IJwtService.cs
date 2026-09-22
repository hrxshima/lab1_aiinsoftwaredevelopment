using AuthService.Models;

namespace AuthService.Services;

public interface IJwtService
{
    string GenerateToken(User user, string? sessionId = null);

    string? GetSessionId(System.Security.Claims.ClaimsPrincipal user);
}
