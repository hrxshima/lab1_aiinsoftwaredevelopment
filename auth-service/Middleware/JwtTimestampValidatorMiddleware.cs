using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Data;
using AuthService.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Middleware;

public class JwtTimestampValidatorMiddleware
{
    private readonly RequestDelegate _next;

    public JwtTimestampValidatorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.GetUserId();
        if (userId == null)
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        DateTime? tokenIssuedAt;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            tokenIssuedAt = jwtToken.ValidFrom;
        }
        catch
        {
            await _next(context);
            return;
        }

        if (tokenIssuedAt == null)
        {
            await _next(context);
            return;
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            await _next(context);
            return;
        }

        if (user.PasswordChangedAt.HasValue && tokenIssuedAt < user.PasswordChangedAt.Value)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("\"Password has been changed. Please login again.\"");
            return;
        }

        await _next(context);
    }
}

public static class JwtTimestampValidatorMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtTimestampValidator(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtTimestampValidatorMiddleware>();
    }
}