using System.Security.Claims;
using TransactionService.Api.Extensions;
using Xunit;

namespace TransactionService.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsNameIdentifierClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42")
        }));

        var result = principal.GetUserId();

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetUserId_ReturnsSubClaimWhenNameIdentifierIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "7")
        }));

        var result = principal.GetUserId();

        Assert.Equal(7, result);
    }

    [Fact]
    public void GetUserId_ReturnsNullWhenClaimIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = principal.GetUserId();

        Assert.Null(result);
    }
}
