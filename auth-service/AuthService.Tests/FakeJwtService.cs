using AuthService.Models;
using AuthService.Services;

namespace AuthService.Tests;

public class FakeJwtService : IJwtService
{
    public string GenerateToken(User user)
    {
        return $"test-token-for-user-{user.Id}";
    }
}
