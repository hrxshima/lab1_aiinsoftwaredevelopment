namespace TransactionService.Api.Models;

public class Category
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Transaction> Transactions { get; set; } = new();
}
