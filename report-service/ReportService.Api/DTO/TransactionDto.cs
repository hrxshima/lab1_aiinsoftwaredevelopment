using System;
using System.Text.Json.Serialization;

namespace report_service.DTO
{
    public class TransactionDto
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; 

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }
}
