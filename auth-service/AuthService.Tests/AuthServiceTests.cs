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
        Assert.Single(dbContext.Sessions);
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
    public async Task ChangePasswordAsync_UpdatesPasswordAndInvalidatesSessions()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Id = 1,
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password")
        });
        dbContext.Sessions.Add(new Session
        {
            UserId = 1,
            TokenHash = "hash1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
        dbContext.Sessions.Add(new Session
        {
            UserId = 1,
            TokenHash = "hash2",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });

        var user = dbContext.Users.Single();
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", user.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("old-password", user.PasswordHash));
        Assert.Empty(dbContext.Sessions);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenCurrentPasswordIsIncorrect()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "new-password"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordEqualsCurrent()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("same-password")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "same-password",
            NewPassword = "same-password"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordIsTooShort()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "password123",
            NewPassword = "short"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenCurrentPasswordIsEmpty()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "new-password"
        }));
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrueForValidSession()
    {
        using var dbContext = TestDb.CreateContext();
        var token = "test-token";
        var tokenHash = Session.HashToken(token);

        dbContext.Sessions.Add(new Session
        {
            UserId = 1,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.ValidateSessionAsync(token);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalseForExpiredSession()
    {
        using var dbContext = TestDb.CreateContext();
        var token = "test-token";
        var tokenHash = Session.HashToken(token);

        dbContext.Sessions.Add(new Session
        {
            UserId = 1,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-24),
            ExpiresAt = DateTime.UtcNow.AddHours(-12)
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.ValidateSessionAsync(token);

        Assert.False(result);
    }
}
