using System;
using System.Collections.Generic;
using System.Linq;
using report_service.DTO;

namespace report_service.Services
{
    public class ReportEngine : IReportEngine
    {
        public ReportSummaryDto GenerateSummary(DateTime startDate, DateTime endDate, List<TransactionDto> transactions)
        {
            decimal totalIncome = 0;
            decimal totalExpense = 0;

            foreach (var transaction in transactions)
            {
                if (string.Equals(transaction.Type, "INCOME", StringComparison.OrdinalIgnoreCase))
                {
                    totalIncome += transaction.Amount;
                }
                else if (string.Equals(transaction.Type, "EXPENSE", StringComparison.OrdinalIgnoreCase))
                {
                    totalExpense += transaction.Amount;
                }
            }

            decimal balance = totalIncome - totalExpense;

            var itemsByCategory = transactions
                .GroupBy(t => new { t.CategoryId, t.CategoryName, NormalizedType = NormalizeType(t.Type) })
                .Select(group => new CategorySummaryDto
                {
                    CategoryId = group.Key.CategoryId,
                    CategoryName = group.Key.CategoryName,
                    Type = group.Key.NormalizedType,
                    Amount = group.Sum(t => t.Amount)
                })
                .OrderByDescending(item => item.Amount)
                .ToList();
//
            return new ReportSummaryDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = balance,
                ItemsByCategory = itemsByCategory
            };
        }

        private static string NormalizeType(string type)
        {
            return string.Equals(type, "INCOME", StringComparison.OrdinalIgnoreCase) ? "INCOME" : "EXPENSE";
        }
    }
}
