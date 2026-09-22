namespace AuthService.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Incremented each time the password is changed.
    /// Used to invalidate old JWT tokens when password changes.
    /// </summary>
    public int PasswordVersion { get; set; } = 1;
}
