using AuthService.Data;
using AuthService.Repositories;
using Moq;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests;

public static class TestDb
{
    public static AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }

    public static Services.AuthService CreateAuthService(AuthDbContext dbContext)
    {
        var sessionRepositoryMock = new Mock<ISessionRepository>();
        sessionRepositoryMock.Setup(r => r.RevokeAllUserSessionsAsync(It.IsAny<int>()))
            .ReturnsAsync(0);

        return new Services.AuthService(
            new UserRepository(dbContext),
            sessionRepositoryMock.Object,
            new FakeJwtService());
    }

    public static Services.AuthService CreateAuthServiceWithSessionRepo(AuthDbContext dbContext, ISessionRepository sessionRepository)
    {
        return new Services.AuthService(
            new UserRepository(dbContext),
            sessionRepository,
            new FakeJwtService());
    }
}
