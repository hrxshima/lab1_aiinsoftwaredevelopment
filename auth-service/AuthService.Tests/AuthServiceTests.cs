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
    public async Task ChangePasswordAsync_SuccessfullyChangesPasswordAndIncrementsVersion()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123"),
            PasswordTokenVersion = 0
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass456!"
        });

        var user = dbContext.Users.Single();
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass456!", user.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("OldPass123", user.PasswordHash));
        Assert.Equal(1, user.PasswordTokenVersion);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenCurrentPasswordIsIncorrect()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPass456!"
        }));

        Assert.Equal("Current password is incorrect.", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordSameAsCurrent()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SamePass123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "SamePass123",
            NewPassword = "SamePass123"
        }));

        Assert.Equal("New password must be different from the current password.", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordTooShort()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "Short1A"
        }));

        Assert.Contains("at least 8 characters", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordWithoutUppercase()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "newpass123"
        }));

        Assert.Contains("uppercase letter", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenNewPasswordWithoutDigit()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPasswordA"
        }));

        Assert.Contains("at least one digit", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenUserNotFound()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(999, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass456!"
        }));

        Assert.Equal("User not found.", exception.Message);
    }
}
