using System.Security.Cryptography;
using System.Text;

namespace AuthService.Models;

public class Session
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}