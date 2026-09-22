using TransactionService.Api.Models;

namespace TransactionService.Api.Repositories;

public interface ICategoryRepository
{
    Task<Category> AddAsync(Category category);

    Task<List<Category>> GetByUserAsync(int userId);

    Task<Category?> GetByIdForUserAsync(int id, int userId);

    Task DeleteAsync(Category category);

    Task SaveAsync();
}
