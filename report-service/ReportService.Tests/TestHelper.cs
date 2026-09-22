using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReportService.Tests;

public static class TestHelper
{
    public static ControllerContext CreateControllerContextWithUserId(int userId, string claimType = ClaimTypes.NameIdentifier)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(claimType, userId.ToString()) }, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    public static ControllerContext CreateControllerContextWithInvalidUserId()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-number") }, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    public static ControllerContext CreateControllerContextWithoutUserId()
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }
}
