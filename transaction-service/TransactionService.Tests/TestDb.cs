using Microsoft.EntityFrameworkCore;
using TransactionService.Api.Data;
using TransactionService.Api.Repositories;
using TransactionService.Api.Services;

namespace TransactionService.Tests;

public static class TestDb
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static CategoryService CreateCategoryService(AppDbContext dbContext)
    {
        return new CategoryService(new CategoryRepository(dbContext));
    }

    public static TransactionService.Api.Services.TransactionService CreateTransactionService(AppDbContext dbContext)
    {
        var categoryRepository = new CategoryRepository(dbContext);
        var transactionRepository = new TransactionRepository(dbContext);

        return new TransactionService.Api.Services.TransactionService(transactionRepository, categoryRepository);
    }
}
