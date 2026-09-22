using report_service.DTO;
using report_service.Services;
using Xunit;

namespace ReportService.Tests;

public class ReportEngineTests
{
    private readonly ReportEngine _engine = new();

    [Fact]
    public void GenerateSummary_ReturnsZeros_WhenTransactionsAreEmpty()
    {
        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), []);

        Assert.Equal(new DateTime(2026, 5, 1), result.StartDate);
        Assert.Equal(new DateTime(2026, 5, 31), result.EndDate);
        Assert.Equal(0, result.TotalIncome);
        Assert.Equal(0, result.TotalExpense);
        Assert.Equal(0, result.Balance);
        Assert.Empty(result.ItemsByCategory);
    }

    [Fact]
    public void GenerateSummary_CalculatesIncomeExpenseAndBalance()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Amount = 1000, Type = "INCOME", CategoryId = 1, CategoryName = "Salary" },
            new() { Amount = 300, Type = "EXPENSE", CategoryId = 2, CategoryName = "Food" },
            new() { Amount = 200, Type = "EXPENSE", CategoryId = 3, CategoryName = "Transport" }
        };

        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), transactions);

        Assert.Equal(1000, result.TotalIncome);
        Assert.Equal(500, result.TotalExpense);
        Assert.Equal(500, result.Balance);
    }

    [Fact]
    public void GenerateSummary_IsCaseInsensitiveForTransactionType()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Amount = 100, Type = "income", CategoryId = 1, CategoryName = "Salary" },
            new() { Amount = 40, Type = "Expense", CategoryId = 2, CategoryName = "Food" }
        };

        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), transactions);

        Assert.Equal(100, result.TotalIncome);
        Assert.Equal(40, result.TotalExpense);
        Assert.Equal(60, result.Balance);
    }

    [Fact]
    public void GenerateSummary_IgnoresUnknownTransactionTypes()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Amount = 100, Type = "TRANSFER", CategoryId = 1, CategoryName = "Other" },
            new() { Amount = 50, Type = "INCOME", CategoryId = 2, CategoryName = "Salary" }
        };

        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), transactions);

        Assert.Equal(50, result.TotalIncome);
        Assert.Equal(0, result.TotalExpense);
        Assert.Equal(50, result.Balance);
    }

    [Fact]
    public void GenerateSummary_GroupsTransactionsByCategoryAndType()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Amount = 100, Type = "EXPENSE", CategoryId = 1, CategoryName = "Food" },
            new() { Amount = 50, Type = "EXPENSE", CategoryId = 1, CategoryName = "Food" },
            new() { Amount = 200, Type = "EXPENSE", CategoryId = 2, CategoryName = "Transport" },
            new() { Amount = 500, Type = "INCOME", CategoryId = 1, CategoryName = "Food" }
        };

        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), transactions);

        Assert.Equal(3, result.ItemsByCategory.Count);
        var foodExpense = result.ItemsByCategory.Single(item => item.CategoryName == "Food" && item.Type == "EXPENSE");
        Assert.Equal(1, foodExpense.CategoryId);
        Assert.Equal(150, foodExpense.Amount);

        var foodIncome = result.ItemsByCategory.Single(item => item.CategoryName == "Food" && item.Type == "INCOME");
        Assert.Equal(1, foodIncome.CategoryId);
        Assert.Equal(500, foodIncome.Amount);

        var transportExpense = result.ItemsByCategory.Single(item => item.CategoryName == "Transport");
        Assert.Equal("EXPENSE", transportExpense.Type);
        Assert.Equal(200, transportExpense.Amount);
    }

    [Fact]
    public void GenerateSummary_OrdersCategoriesByAmountDescending()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Amount = 50, Type = "EXPENSE", CategoryId = 1, CategoryName = "Food" },
            new() { Amount = 300, Type = "EXPENSE", CategoryId = 2, CategoryName = "Rent" },
            new() { Amount = 100, Type = "EXPENSE", CategoryId = 3, CategoryName = "Transport" }
        };

        var result = _engine.GenerateSummary(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), transactions);

        Assert.Equal([300, 100, 50], result.ItemsByCategory.Select(item => item.Amount));
        Assert.All(result.ItemsByCategory, item => Assert.Equal("EXPENSE", item.Type));
    }

    [Fact]
    public void GenerateSummary_PopulatesStartAndEndDates()
    {
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 31);

        var result = _engine.GenerateSummary(start, end, []);

        Assert.Equal(start, result.StartDate);
        Assert.Equal(end, result.EndDate);
    }
}
