using AuthService.Data;
using AuthService.Dtos;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly AuthDbContext _dbContext;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, AuthDbContext dbContext)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _dbContext = dbContext;
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

        var token = _jwtService.GenerateToken(user);
        await CreateSessionAsync(user.Id, token, DateTime.UtcNow.AddHours(12));

        return new LoginResponse
        {
            Token = token,
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
        ValidateCurrentPassword(request.CurrentPassword);
        ValidateNewPassword(request.NewPassword);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new InvalidOperationException("New password must be different from the current password.");
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordHashAsync(userId, newPasswordHash);
        await InvalidateUserSessionsAsync(userId);
    }

    public async Task CreateSessionAsync(int userId, string token, DateTime expiresAt)
    {
        var session = new Session
        {
            UserId = userId,
            TokenHash = Session.HashToken(token),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    public async Task InvalidateUserSessionsAsync(int userId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _dbContext.Sessions.RemoveRange(sessions);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ValidateSessionAsync(string token)
    {
        var tokenHash = Session.HashToken(token);
        var session = await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash && s.ExpiresAt > DateTime.UtcNow);

        return session != null;
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

    private static void ValidateCurrentPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Current password cannot be empty.");
        }
    }

    private static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new ArgumentException("New password must contain at least 6 characters.");
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
