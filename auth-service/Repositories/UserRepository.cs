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

    public async Task UpdatePasswordHashAsync(int userId, string newPasswordHash)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddSessionAsync(UserSession session)
    {
        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteSessionsByUserIdAsync(int userId)
    {
        var sessions = await _dbContext.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _dbContext.UserSessions.RemoveRange(sessions);
        await _dbContext.SaveChangesAsync();
    }

    public Task<UserSession?> GetSessionByTokenHashAsync(string tokenHash)
    {
        return _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionTokenHash == tokenHash);
    }
}
