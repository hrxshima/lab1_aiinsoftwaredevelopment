using System.Security.Claims;

namespace report_service.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("userId")
            ?? user.FindFirstValue("id");

        return int.TryParse(value, out var userId) ? userId : null;
    }
}
