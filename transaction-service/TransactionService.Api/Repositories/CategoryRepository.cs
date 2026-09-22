using Microsoft.EntityFrameworkCore;
using TransactionService.Api.Data;
using TransactionService.Api.Models;

namespace TransactionService.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category> AddAsync(Category category)
    {
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();
        return category;
    }

    public Task<List<Category>> GetByUserAsync(int userId)
    {
        return _dbContext.Categories
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Id)
            .ToListAsync();
    }

    public Task<Category?> GetByIdForUserAsync(int id, int userId)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id && category.UserId == userId);
    }

    public async Task DeleteAsync(Category category)
    {
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();
    }

    public Task SaveAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
