using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using report_service.DTO;

namespace report_service.Services
{
    public interface IFinanceServiceClient
    {
        Task<List<TransactionDto>> GetTransactionsForReportAsync(int userId, DateTime startDate, DateTime endDate);
    }
}
