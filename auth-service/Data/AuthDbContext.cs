using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(user => user.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(user => user.Email)
            .HasMaxLength(255);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Session>()
            .Property(session => session.TokenHash)
            .HasMaxLength(512);

        modelBuilder.Entity<Session>()
            .HasIndex(session => session.TokenHash);

        modelBuilder.Entity<Session>()
            .HasIndex(session => session.UserId);

        modelBuilder.Entity<Session>()
            .HasIndex(session => session.SessionId)
            .IsUnique();

        modelBuilder.Entity<Session>()
            .HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
