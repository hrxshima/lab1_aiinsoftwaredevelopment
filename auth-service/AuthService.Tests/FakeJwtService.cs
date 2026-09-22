using AuthService.Models;
using AuthService.Services;

namespace AuthService.Tests;

public class FakeJwtService : IJwtService
{
    public string GenerateToken(User user, string? sessionId = null)
    {
        return $"test-token-for-user-{user.Id}";
    }

    public string? GetSessionId(System.Security.Claims.ClaimsPrincipal user)
    {
        return null;
    }
}
