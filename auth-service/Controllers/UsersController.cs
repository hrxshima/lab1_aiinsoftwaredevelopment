using System.Security.Claims;
using AuthService.Dtos;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterUserRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(InternalUsersController.GetById), "InternalUsers", new { id = user.Id }, user);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginUserRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return response == null ? Unauthorized("Invalid email or password.") : response;
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _authService.GetByIdAsync(userId.Value);
        return user == null ? Unauthorized() : user;
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var success = await _authService.ChangePasswordAsync(userId.Value, request);
            if (!success)
            {
                return BadRequest("Current password is incorrect.");
            }

            return Ok(new { message = "Password changed successfully. Please login again." });
        }
        catch (KeyNotFoundException)
        {
            return Unauthorized();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(value, out var userId) ? userId : null;
    }
}
