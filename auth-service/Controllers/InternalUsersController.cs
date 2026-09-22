using AuthService.Dtos;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("internal/users")]
public class InternalUsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public InternalUsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        try
        {
            var user = await _authService.GetByIdAsync(id);
            return user == null ? NotFound() : user;
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
