using AuthService.Models;

namespace AuthService.Repositories;

public interface IUserRepository
{
    Task<User> AddAsync(User user);

    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email);

    Task UpdatePasswordHashAsync(int userId, string newPasswordHash);

    Task AddSessionAsync(UserSession session);

    Task DeleteSessionsByUserIdAsync(int userId);

    Task<UserSession?> GetSessionByTokenHashAsync(string tokenHash);
}
