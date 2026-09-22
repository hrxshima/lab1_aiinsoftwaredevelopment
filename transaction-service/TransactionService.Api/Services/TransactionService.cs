using TransactionService.Api.Dtos;
using TransactionService.Api.Models;
using TransactionService.Api.Repositories;

namespace TransactionService.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public TransactionService(ITransactionRepository transactionRepository, ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<TransactionResponse> CreateAsync(int userId, CreateTransactionRequest request)
    {
        ValidateTransactionData(userId, request.CategoryId, request.Amount, request.Type);
        await CheckCategoryAsync(request.CategoryId, userId);

        var transaction = new Transaction
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Type = request.Type,
            Description = request.Description ?? string.Empty,
            Date = request.Date
        };

        transaction = await _transactionRepository.AddAsync(transaction);
        return ToResponse(transaction);
    }

    public async Task<List<TransactionResponse>> GetByUserAsync(int userId, DateTime? from, DateTime? to, TransactionType? type, int? categoryId)
    {
        ValidateUserId(userId);
        ValidateType(type);

        var transactions = await _transactionRepository.GetByUserAsync(userId, from, to, type, categoryId);
        return transactions.Select(ToResponse).ToList();
    }

    public async Task<TransactionResponse?> GetByIdAsync(int id, int userId)
    {
        ValidateUserId(userId);

        var transaction = await _transactionRepository.GetByIdForUserAsync(id, userId);
        return transaction == null ? null : ToResponse(transaction);
    }

    public async Task<TransactionResponse?> UpdateAsync(int id, int userId, UpdateTransactionRequest request)
    {
        ValidateTransactionData(userId, request.CategoryId, request.Amount, request.Type);
        await CheckCategoryAsync(request.CategoryId, userId);

        var transaction = await _transactionRepository.GetByIdForUserAsync(id, userId);
        if (transaction == null)
        {
            return null;
        }

        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Description = request.Description ?? string.Empty;
        transaction.Date = request.Date;

        await _transactionRepository.SaveAsync();

        var updatedTransaction = await _transactionRepository.GetByIdForUserAsync(id, userId);
        return updatedTransaction == null ? null : ToResponse(updatedTransaction);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        ValidateUserId(userId);

        var transaction = await _transactionRepository.GetByIdForUserAsync(id, userId);
        if (transaction == null)
        {
            return false;
        }

        await _transactionRepository.DeleteAsync(transaction);
        return true;
    }

    public async Task<List<TransactionReportResponse>> GetReportAsync(int userId, DateTime startDate, DateTime endDate)
    {
        ValidateUserId(userId);

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be later than end date.");
        }

        var transactions = await _transactionRepository.GetReportItemsAsync(userId, startDate, endDate);
        return transactions.Select(ToReportResponse).ToList();
    }

    private async Task CheckCategoryAsync(int categoryId, int userId)
    {
        var category = await _categoryRepository.GetByIdForUserAsync(categoryId, userId);
        if (category == null)
        {
            throw new ArgumentException("Category was not found for this user.");
        }
    }

    private static void ValidateTransactionData(int userId, int categoryId, decimal amount, TransactionType type)
    {
        ValidateUserId(userId);
        ValidateType(type);

        if (categoryId <= 0)
        {
            throw new ArgumentException("CategoryId must be greater than zero.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }
    }

    private static void ValidateType(TransactionType? type)
    {
        if (type.HasValue && !Enum.IsDefined(typeof(TransactionType), type.Value))
        {
            throw new ArgumentException("Transaction type must be Income or Expense.");
        }
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("UserId must be greater than zero.");
        }
    }

    private static TransactionResponse ToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name ?? string.Empty,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Description = transaction.Description,
            Date = transaction.Date
        };
    }

    private static TransactionReportResponse ToReportResponse(Transaction transaction)
    {
        return new TransactionReportResponse
        {
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name ?? string.Empty,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Date = transaction.Date
        };
    }
}
