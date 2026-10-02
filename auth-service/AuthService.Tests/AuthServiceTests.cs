using AuthService.Dtos;
using AuthService.Models;
using Xunit;

namespace AuthService.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPassword()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.RegisterAsync(new RegisterUserRequest
        {
            Name = "User",
            Email = "USER@example.com",
            Password = "123456"
        });

        var user = dbContext.Users.Single();

        Assert.True(result.Id > 0);
        Assert.Equal("User", result.Name);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal(result.Id, user.Id);
        Assert.NotEqual("123456", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("123456", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenEmailAlreadyExists()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "Existing User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Name = "New User",
            Email = "USER@example.com",
            Password = "123456"
        }));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenPasswordIsTooShort()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(new RegisterUserRequest
        {
            Name = "User",
            Email = "user@example.com",
            Password = "12345"
        }));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenAndUserForValidCredentials()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.LoginAsync(new LoginUserRequest
        {
            Email = "USER@example.com",
            Password = "123456"
        });

        Assert.NotNull(result);
        Assert.Equal("test-token-for-user-1", result.Token);
        Assert.Equal("User", result.User.Name);
        Assert.Equal("user@example.com", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForInvalidPassword()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.LoginAsync(new LoginUserRequest
        {
            Email = "user@example.com",
            Password = "wrong-password"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUserWithoutPasswordHash()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("User", result.Name);
        Assert.Equal("user@example.com", result.Email);
    }

    [Fact]
    public async Task ChangePasswordAsync_UpdatesPasswordHashAndInvalidatesSessions()
    {
        using var dbContext = TestDb.CreateContext();
        var userId = 1;
        var originalPasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword");

        dbContext.Users.Add(new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            PasswordHash = originalPasswordHash
        });

        dbContext.UserSessions.Add(new UserSession
        {
            UserId = userId,
            SessionTokenHash = "old-session-hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });

        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await service.ChangePasswordAsync(userId, new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        });

        var user = await dbContext.Users.FindAsync(userId);
        Assert.NotNull(user);
        Assert.NotEqual(originalPasswordHash, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword123", user.PasswordHash));

        var sessions = dbContext.UserSessions.Where(s => s.UserId == userId);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenCurrentPasswordIsIncorrect()
    {
        using var dbContext = TestDb.CreateContext();
        var userId = 1;

        dbContext.Users.Add(new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(userId, new ChangePasswordRequest
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordEqualsCurrent()
    {
        using var dbContext = TestDb.CreateContext();
        var userId = 1;
        var currentPassword = "samepassword";

        dbContext.Users.Add(new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword)
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(userId, new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = currentPassword
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenUserNotFound()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(999, new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        }));
    }
}
