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
    public async Task ChangePasswordAsync_SuccessfullyChangesPassword()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456@"
        });

        Assert.Equal("Password changed successfully. All other sessions have been revoked.", result.Message);

        var user = dbContext.Users.Single();
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass456@", user.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("OldPass123!", user.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsOnInvalidCurrentPassword()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPass456@"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsOnSamePassword()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SamePass123!")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "SamePass123!",
            NewPassword = "SamePass123!"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsOnWeakNewPassword()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!")
        });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateAuthService(dbContext);

        var weakPasswords = new[]
        {
            "short1!",           // too short
            "nouppercase1!",      // no uppercase
            "NOLOWERCASE1!",      // no lowercase
            "NoDigits!!!!!"       // no digit
        };

        foreach (var weakPassword in weakPasswords)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(1, new ChangePasswordRequest
            {
                CurrentPassword = "OldPass123!",
                NewPassword = weakPassword
            }));
        }
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsForNonExistentUser()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateAuthService(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ChangePasswordAsync(999, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456@"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_RevokesAllUserSessions()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Users.Add(new User
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!")
        });
        await dbContext.SaveChangesAsync();

        var sessionRepositoryMock = new Moq.Mock<AuthService.Repositories.ISessionRepository>();
        sessionRepositoryMock.Setup(r => r.RevokeAllUserSessionsAsync(1))
            .Returns(Task.FromResult(3));

        var service = TestDb.CreateAuthServiceWithSessionRepo(dbContext, sessionRepositoryMock.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456@"
        });

        Assert.Equal(3, result.RevokedSessionsCount);
        sessionRepositoryMock.Verify(r => r.RevokeAllUserSessionsAsync(1), Moq.Times.Once);
    }
}
