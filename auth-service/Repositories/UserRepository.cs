using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _dbContext.Users.AnyAsync(user => user.Email == email);
    }

    public async Task AddSessionAsync(Session session)
    {
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeUserSessionsAsync(int userId)
    {
        var activeSessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.IsRevoked = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    public Task<int> GetActiveSessionCountAsync(int userId)
    {
        return _dbContext.Sessions
            .CountAsync(s => s.UserId == userId && !s.IsRevoked);
    }

    public async Task<User> UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public Task<Session?> GetSessionByIdAsync(string sessionId)
    {
        return _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }
}
