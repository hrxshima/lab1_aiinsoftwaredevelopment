namespace AuthService.Models;

public class UserSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string SessionTokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}