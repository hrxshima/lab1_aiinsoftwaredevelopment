using AuthService.Dtos;

namespace AuthService.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterUserRequest request);

    Task<LoginResponse?> LoginAsync(LoginUserRequest request);

    Task<UserResponse?> GetByIdAsync(int id);
}
