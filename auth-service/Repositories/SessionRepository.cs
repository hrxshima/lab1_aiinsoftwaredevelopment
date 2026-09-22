using System.Security.Cryptography;
using System.Text;
using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AuthDbContext _dbContext;

    public SessionRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Session> CreateAsync(Session session)
    {
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }

    public Task<Session?> GetByIdAsync(int id)
    {
        return _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task<Session?> GetByTokenHashAsync(string tokenHash)
    {
        return _dbContext.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);
    }

    public Task<IReadOnlyList<Session>> GetActiveSessionsByUserIdAsync(int userId)
    {
        return _dbContext.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Session>)t.Result);
    }

    public async Task<int> RevokeAllUserSessionsAsync(int userId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
        }

        await _dbContext.SaveChangesAsync();
        return sessions.Count;
    }

    public async Task<int> RevokeSessionAsync(int sessionId)
    {
        var session = await _dbContext.Sessions.FindAsync(sessionId);
        if (session == null || session.IsRevoked)
        {
            return 0;
        }

        session.IsRevoked = true;
        await _dbContext.SaveChangesAsync();
        return 1;
    }

    public Task<bool> ValidateSessionAsync(string tokenHash)
    {
        return _dbContext.Sessions
            .AnyAsync(s => s.TokenHash == tokenHash
                && !s.IsRevoked
                && s.ExpiresAt > DateTime.UtcNow);
    }

    public async Task DeleteExpiredSessionsAsync()
    {
        var expiredSessions = await _dbContext.Sessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        _dbContext.Sessions.RemoveRange(expiredSessions);
        await _dbContext.SaveChangesAsync();
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}