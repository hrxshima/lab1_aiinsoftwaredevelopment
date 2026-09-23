using AuthService.Dtos;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<UserResponse> RegisterAsync(RegisterUserRequest request)
    {
        ValidateName(request.Name);
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);

        var email = NormalizeEmail(request.Email);
        if (await _userRepository.EmailExistsAsync(email))
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        user = await _userRepository.AddAsync(user);
        return ToResponse(user);
    }

    public async Task<LoginResponse?> LoginAsync(LoginUserRequest request)
    {
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);

        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email));
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new LoginResponse
        {
            Token = _jwtService.GenerateToken(user),
            User = ToResponse(user)
        };
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("UserId must be greater than zero.");
        }

        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : ToResponse(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        ValidatePassword(request.NewPassword);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new InvalidOperationException("New password must be different from current password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("Email is invalid.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new ArgumentException("Password must contain at least 6 characters.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}
