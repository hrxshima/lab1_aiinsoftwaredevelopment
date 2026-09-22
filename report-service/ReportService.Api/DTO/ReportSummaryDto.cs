using System;
using System.Text.Json.Serialization;

namespace report_service.DTO
{
    public class ReportSummaryDto
    {
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("totalIncome")]
        public decimal TotalIncome { get; set; }

        [JsonPropertyName("totalExpense")]
        public decimal TotalExpense { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("itemsByCategory")]
        public List<CategorySummaryDto> ItemsByCategory { get; set; } = new();
    }
}
