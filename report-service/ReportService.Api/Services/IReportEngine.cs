using System;
using System.Collections.Generic;
using report_service.DTO;

namespace report_service.Services
{
    public interface IReportEngine
    {
        ReportSummaryDto GenerateSummary(DateTime startDate, DateTime endDate, List<TransactionDto> transactions);
    }
}
