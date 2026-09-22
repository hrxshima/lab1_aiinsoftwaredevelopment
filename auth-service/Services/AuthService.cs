using System.Security.Cryptography;
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

        var sessionId = GenerateSessionId();
        var token = _jwtService.GenerateToken(user, sessionId);
        var tokenHash = HashToken(token);

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            IsRevoked = false
        };

        await _userRepository.AddSessionAsync(session);

        return new LoginResponse
        {
            Token = token,
            User = ToResponse(user)
        };
    }

    private static string GenerateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
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

    public async Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        ValidateCurrentPassword(request.CurrentPassword);
        ValidateNewPassword(request.NewPassword);

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new InvalidOperationException("New password must be different from current password.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        var revokedSessionsCount = await _userRepository.GetActiveSessionCountAsync(userId);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        var updatedUser = await _userRepository.UpdateAsync(user);

        await _userRepository.RevokeUserSessionsAsync(userId);

        return new ChangePasswordResponse
        {
            Message = "Password changed successfully. All other sessions have been revoked.",
            RevokedSessionsCount = revokedSessionsCount
        };
    }

    private static void ValidateCurrentPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Current password cannot be empty.");
        }
    }

    private static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("New password cannot be empty.");
        }

        if (password.Length < 8)
        {
            throw new ArgumentException("New password must contain at least 8 characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new ArgumentException("New password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new ArgumentException("New password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new ArgumentException("New password must contain at least one digit.");
        }
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
