using Microsoft.EntityFrameworkCore;
using TransactionService.Api.Data;
using TransactionService.Api.Models;

namespace TransactionService.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _dbContext;

    public TransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(transaction)
            .Reference(item => item.Category)
            .LoadAsync();

        return transaction;
    }

    public Task<Transaction?> GetByIdForUserAsync(int id, int userId)
    {
        return _dbContext.Transactions
            .Include(transaction => transaction.Category)
            .FirstOrDefaultAsync(transaction => transaction.Id == id && transaction.UserId == userId);
    }

    public Task<List<Transaction>> GetByUserAsync(int userId, DateTime? from, DateTime? to, TransactionType? type, int? categoryId)
    {
        var query = _dbContext.Transactions
            .Include(transaction => transaction.Category)
            .Where(transaction => transaction.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(transaction => transaction.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(transaction => transaction.Date <= to.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == type.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }

        return query
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync();
    }

    public Task<List<Transaction>> GetReportItemsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return _dbContext.Transactions
            .Include(transaction => transaction.Category)
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Date >= startDate &&
                transaction.Date <= endDate)
            .OrderBy(transaction => transaction.Date)
            .ToListAsync();
    }

    public async Task DeleteAsync(Transaction transaction)
    {
        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync();
    }

    public Task SaveAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
