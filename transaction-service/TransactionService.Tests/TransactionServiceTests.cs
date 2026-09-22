using TransactionService.Api.Dtos;
using TransactionService.Api.Data;
using TransactionService.Api.Models;
using Xunit;

namespace TransactionService.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesTransactionWithUserCategory()
    {
        using var dbContext = TestDb.CreateContext();
        var category = await AddCategory(dbContext, 1, "Food");
        var service = TestDb.CreateTransactionService(dbContext);

        var result = await service.CreateAsync(1, new CreateTransactionRequest
        {
            CategoryId = category.Id,
            Amount = 500,
            Type = TransactionType.Expense,
            Description = "Lunch",
            Date = new DateTime(2026, 5, 10)
        });

        Assert.True(result.Id > 0);
        Assert.Equal("Food", result.CategoryName);
        Assert.Equal(500, result.Amount);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenAmountIsZero()
    {
        using var dbContext = TestDb.CreateContext();
        var category = await AddCategory(dbContext, 1, "Food");
        var service = TestDb.CreateTransactionService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(1, new CreateTransactionRequest
        {
            CategoryId = category.Id,
            Amount = 0,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 5, 10)
        }));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryBelongsToAnotherUser()
    {
        using var dbContext = TestDb.CreateContext();
        var category = await AddCategory(dbContext, 2, "Food");
        var service = TestDb.CreateTransactionService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(1, new CreateTransactionRequest
        {
            CategoryId = category.Id,
            Amount = 500,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 5, 10)
        }));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryDoesNotExist()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateTransactionService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(1, new CreateTransactionRequest
        {
            CategoryId = 999,
            Amount = 500,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 5, 10)
        }));
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyUserTransactions()
    {
        using var dbContext = TestDb.CreateContext();
        var food = await AddCategory(dbContext, 1, "Food");
        var taxi = await AddCategory(dbContext, 2, "Taxi");
        await AddTransaction(dbContext, 1, food.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));
        await AddTransaction(dbContext, 2, taxi.Id, 300, TransactionType.Expense, new DateTime(2026, 5, 11));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.GetByUserAsync(1, null, null, null, null);

        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    [Fact]
    public async Task GetByUserAsync_FiltersByDateTypeAndCategory()
    {
        using var dbContext = TestDb.CreateContext();
        var food = await AddCategory(dbContext, 1, "Food");
        var salary = await AddCategory(dbContext, 1, "Salary");

        await AddTransaction(dbContext, 1, food.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));
        await AddTransaction(dbContext, 1, food.Id, 100, TransactionType.Expense, new DateTime(2026, 6, 1));
        await AddTransaction(dbContext, 1, salary.Id, 1000, TransactionType.Income, new DateTime(2026, 5, 15));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.GetByUserAsync(
            1,
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 31),
            TransactionType.Expense,
            food.Id);

        Assert.Single(result);
        Assert.Equal(500, result[0].Amount);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserTransaction()
    {
        using var dbContext = TestDb.CreateContext();
        var food = await AddCategory(dbContext, 1, "Food");
        var transport = await AddCategory(dbContext, 1, "Transport");
        var transaction = await AddTransaction(dbContext, 1, food.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.UpdateAsync(transaction.Id, 1, new UpdateTransactionRequest
        {
            CategoryId = transport.Id,
            Amount = 250,
            Type = TransactionType.Expense,
            Description = "Bus",
            Date = new DateTime(2026, 5, 12)
        });

        Assert.NotNull(result);
        Assert.Equal(250, result.Amount);
        Assert.Equal("Transport", result.CategoryName);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForAnotherUserTransaction()
    {
        using var dbContext = TestDb.CreateContext();
        var userCategory = await AddCategory(dbContext, 1, "Food");
        var anotherUserCategory = await AddCategory(dbContext, 2, "Food");
        var transaction = await AddTransaction(dbContext, 2, anotherUserCategory.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.UpdateAsync(transaction.Id, 1, new UpdateTransactionRequest
        {
            CategoryId = userCategory.Id,
            Amount = 250,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 5, 12)
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForAnotherUserTransaction()
    {
        using var dbContext = TestDb.CreateContext();
        var category = await AddCategory(dbContext, 2, "Food");
        var transaction = await AddTransaction(dbContext, 2, category.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.DeleteAsync(transaction.Id, 1);

        Assert.False(result);
        Assert.Single(dbContext.Transactions);
    }

    [Fact]
    public async Task DeleteAsync_DeletesUserTransaction()
    {
        using var dbContext = TestDb.CreateContext();
        var category = await AddCategory(dbContext, 1, "Food");
        var transaction = await AddTransaction(dbContext, 1, category.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.DeleteAsync(transaction.Id, 1);

        Assert.True(result);
        Assert.Empty(dbContext.Transactions);
    }

    [Fact]
    public async Task GetReportAsync_ReturnsTransactionsWithCategoryName()
    {
        using var dbContext = TestDb.CreateContext();
        var food = await AddCategory(dbContext, 1, "Food");
        await AddTransaction(dbContext, 1, food.Id, 500, TransactionType.Expense, new DateTime(2026, 5, 10));

        var service = TestDb.CreateTransactionService(dbContext);
        var result = await service.GetReportAsync(1, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Single(result);
        Assert.Equal("Food", result[0].CategoryName);
        Assert.Equal(TransactionType.Expense, result[0].Type);
    }

    private static async Task<Category> AddCategory(AppDbContext dbContext, int userId, string name)
    {
        var category = new Category { UserId = userId, Name = name };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private static async Task<Transaction> AddTransaction(AppDbContext dbContext, int userId, int categoryId, decimal amount, TransactionType type, DateTime date)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            Type = type,
            Date = date
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();
        return transaction;
    }
}
