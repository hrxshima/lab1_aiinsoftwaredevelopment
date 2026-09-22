using TransactionService.Api.Dtos;
using TransactionService.Api.Models;
using TransactionService.Api.Repositories;

namespace TransactionService.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> CreateAsync(int userId, CreateCategoryRequest request)
    {
        ValidateUserId(userId);
        ValidateName(request.Name);

        var category = new Category
        {
            UserId = userId,
            Name = request.Name.Trim()
        };

        category = await _categoryRepository.AddAsync(category);
        return ToResponse(category);
    }

    public async Task<List<CategoryResponse>> GetByUserAsync(int userId)
    {
        ValidateUserId(userId);

        var categories = await _categoryRepository.GetByUserAsync(userId);
        return categories.Select(ToResponse).ToList();
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, int userId)
    {
        ValidateUserId(userId);

        var category = await _categoryRepository.GetByIdForUserAsync(id, userId);
        return category == null ? null : ToResponse(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, int userId, UpdateCategoryRequest request)
    {
        ValidateUserId(userId);
        ValidateName(request.Name);

        var category = await _categoryRepository.GetByIdForUserAsync(id, userId);
        if (category == null)
        {
            return null;
        }

        category.Name = request.Name.Trim();
        await _categoryRepository.SaveAsync();

        return ToResponse(category);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        ValidateUserId(userId);

        var category = await _categoryRepository.GetByIdForUserAsync(id, userId);
        if (category == null)
        {
            return false;
        }

        await _categoryRepository.DeleteAsync(category);
        return true;
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("UserId must be greater than zero.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.");
        }
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            UserId = category.UserId,
            Name = category.Name
        };
    }
}
