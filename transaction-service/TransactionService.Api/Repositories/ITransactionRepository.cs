using TransactionService.Api.Models;

namespace TransactionService.Api.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> AddAsync(Transaction transaction);

    Task<Transaction?> GetByIdForUserAsync(int id, int userId);

    Task<List<Transaction>> GetByUserAsync(int userId, DateTime? from, DateTime? to, TransactionType? type, int? categoryId);

    Task<(List<Transaction> Items, int TotalCount)> GetByUserPaginatedAsync(int userId, DateTime? from, DateTime? to, TransactionType? type, int? categoryId, int page, int pageSize);

    Task<List<Transaction>> GetReportItemsAsync(int userId, DateTime startDate, DateTime endDate);

    Task DeleteAsync(Transaction transaction);

    Task SaveAsync();
}
