using TransactionService.Api.Dtos;

namespace TransactionService.Api.Services;

public interface ICategoryService
{
    Task<CategoryResponse> CreateAsync(int userId, CreateCategoryRequest request);

    Task<List<CategoryResponse>> GetByUserAsync(int userId);

    Task<CategoryResponse?> GetByIdAsync(int id, int userId);

    Task<CategoryResponse?> UpdateAsync(int id, int userId, UpdateCategoryRequest request);

    Task<bool> DeleteAsync(int id, int userId);
}
