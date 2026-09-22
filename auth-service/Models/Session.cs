namespace AuthService.Models;

public class Session
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;
}