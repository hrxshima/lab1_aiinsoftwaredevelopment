using System.Security.Claims;
using AuthService.Repositories;
using AuthService.Services;

namespace AuthService.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, IJwtService jwtService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = jwtService.GetSessionId(context.User);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var session = await userRepository.GetSessionByIdAsync(sessionId);
                if (session == null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Session has been revoked or expired." });
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class SessionValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionValidationMiddleware>();
    }
}