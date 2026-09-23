using System.IdentityModel.Tokens.Jwt;
using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace AuthService.Tests;

public class JwtServiceTests
{
    private const string TestSecret = "test-secret-key-at-least-32-characters-long-for-hs256";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private JwtService CreateJwtService()
    {
        var config = new FakeConfiguration();
        return new JwtService(config);
    }

    [Fact]
    public void GenerateToken_IncludesSessionVersion()
    {
        var jwtService = CreateJwtService();

        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            SessionVersion = 5
        };

        var token = jwtService.GenerateToken(user);

        Assert.Equal(5, jwtService.GetSessionVersionFromToken(token));
    }

    [Fact]
    public void GenerateToken_DefaultsSessionVersionToZero()
    {
        var jwtService = CreateJwtService();

        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        var token = jwtService.GenerateToken(user);

        Assert.Equal(0, jwtService.GetSessionVersionFromToken(token));
    }

    [Fact]
    public void GetSessionVersionFromToken_ReturnsZeroForInvalidToken()
    {
        var jwtService = CreateJwtService();

        var version = jwtService.GetSessionVersionFromToken("invalid-token");
        Assert.Equal(0, version);
    }

    [Fact]
    public void GetSessionVersionFromToken_ParsesOldTokenWithoutClaim()
    {
        var jwtService = CreateJwtService();
        var tokenWithoutVersion = CreateTokenWithoutVersion();

        var version = jwtService.GetSessionVersionFromToken(tokenWithoutVersion);
        Assert.Equal(0, version);
    }

    private static string CreateTokenWithoutVersion()
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: new[]
            {
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, "1"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private class FakeConfiguration : IConfiguration
    {
        public string? this[string key]
        {
            get => key switch
            {
                "Jwt:Secret" => TestSecret,
                "Jwt:Issuer" => TestIssuer,
                "Jwt:Audience" => TestAudience,
                _ => null
            };
            set { }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => null!;

        public IConfigurationSection GetSection(string key) => null!;
    }
}