using TransactionService.Api.Dtos;
using TransactionService.Api.Models;

namespace TransactionService.Api.Services;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(int userId, CreateTransactionRequest request);

    Task<List<TransactionResponse>> GetByUserAsync(int userId, DateTime? from, DateTime? to, TransactionType? type, int? categoryId);

    Task<TransactionResponse?> GetByIdAsync(int id, int userId);

    Task<TransactionResponse?> UpdateAsync(int id, int userId, UpdateTransactionRequest request);

    Task<bool> DeleteAsync(int id, int userId);

    Task<List<TransactionReportResponse>> GetReportAsync(int userId, DateTime startDate, DateTime endDate);
}
