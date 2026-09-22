namespace AuthService.Models;

public class Session
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public User User { get; set; } = null!;
}