using TransactionService.Api.Dtos;
using TransactionService.Api.Models;
using Xunit;

namespace TransactionService.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesCategory()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateCategoryService(dbContext);

        var result = await service.CreateAsync(1, new CreateCategoryRequest
        {
            Name = "Food"
        });

        Assert.Equal(1, result.UserId);
        Assert.Equal("Food", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenNameIsEmpty()
    {
        using var dbContext = TestDb.CreateContext();
        var service = TestDb.CreateCategoryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(1, new CreateCategoryRequest
        {
            Name = ""
        }));
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyUserCategories()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Categories.Add(new Category { UserId = 1, Name = "Food" });
        dbContext.Categories.Add(new Category { UserId = 2, Name = "Taxi" });
        await dbContext.SaveChangesAsync();

        var service = TestDb.CreateCategoryService(dbContext);
        var result = await service.GetByUserAsync(1);

        Assert.Single(result);
        Assert.Equal("Food", result[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserCategory()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Categories.Add(new Category { UserId = 1, Name = "Food" });
        await dbContext.SaveChangesAsync();

        var categoryId = dbContext.Categories.First().Id;
        var service = TestDb.CreateCategoryService(dbContext);

        var result = await service.UpdateAsync(categoryId, 1, new UpdateCategoryRequest
        {
            Name = "Products"
        });

        Assert.NotNull(result);
        Assert.Equal("Products", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForAnotherUserCategory()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Categories.Add(new Category { UserId = 2, Name = "Food" });
        await dbContext.SaveChangesAsync();

        var categoryId = dbContext.Categories.First().Id;
        var service = TestDb.CreateCategoryService(dbContext);

        var result = await service.UpdateAsync(categoryId, 1, new UpdateCategoryRequest
        {
            Name = "Products"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForAnotherUserCategory()
    {
        using var dbContext = TestDb.CreateContext();
        dbContext.Categories.Add(new Category { UserId = 2, Name = "Food" });
        await dbContext.SaveChangesAsync();

        var categoryId = dbContext.Categories.First().Id;
        var service = TestDb.CreateCategoryService(dbContext);

        var result = await service.DeleteAsync(categoryId, 1);

        Assert.False(result);
        Assert.Single(dbContext.Categories);
    }
}
