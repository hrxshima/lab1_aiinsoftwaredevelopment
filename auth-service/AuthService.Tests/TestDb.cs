using AuthService.Data;
using AuthService.Repositories;
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
        return new Services.AuthService(new UserRepository(dbContext), new FakeJwtService());
    }
}
