using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using report_service.DTO;

namespace report_service.Services
{
    public class FinanceServiceClient : IFinanceServiceClient
    {
        private readonly HttpClient _httpClient;

        public FinanceServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TransactionDto>> GetTransactionsForReportAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var formattedStartDate = startDate.ToString("yyyy-MM-dd");
            var formattedEndDate = endDate.ToString("yyyy-MM-dd");
            var url = $"/internal/transactions/report?userId={userId}&startDate={formattedStartDate}&endDate={formattedEndDate}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to fetch data from Finance Service: {response.StatusCode}. Details: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
            return result ?? new List<TransactionDto>();
        }
    }
}
