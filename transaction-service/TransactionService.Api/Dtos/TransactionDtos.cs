using TransactionService.Api.Models;

namespace TransactionService.Api.Dtos;

public class CreateTransactionRequest
{
    public int CategoryId { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

public class UpdateTransactionRequest
{
    public int CategoryId { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

public class TransactionResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

public class TransactionReportResponse
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public DateTime Date { get; set; }
}
