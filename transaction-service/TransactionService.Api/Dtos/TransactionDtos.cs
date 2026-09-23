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

public record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public int Page { get; init; } = Page > 0 ? Page : 1;
    public int PageSize { get; init; } = PageSize > 0 && PageSize <= 100 ? PageSize : 20;
}

public record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
