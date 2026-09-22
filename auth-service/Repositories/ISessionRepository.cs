using AuthService.Models;

namespace AuthService.Repositories;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);

    Task<Session?> GetByIdAsync(int id);

    Task<Session?> GetByTokenHashAsync(string tokenHash);

    Task<IReadOnlyList<Session>> GetActiveSessionsByUserIdAsync(int userId);

    Task<int> RevokeAllUserSessionsAsync(int userId);

    Task<int> RevokeSessionAsync(int sessionId);

    Task<bool> ValidateSessionAsync(string tokenHash);

    Task DeleteExpiredSessionsAsync();
}