using Microsoft.EntityFrameworkCore;
using Crypto_Hockey.Models;

namespace Crypto_Hockey.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerProfile> PlayerProfiles { get; set; } = null!;
    public DbSet<GameSession> GameSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PlayerProfile
        modelBuilder.Entity<PlayerProfile>()
            .HasMany(p => p.GameSessions)
            .WithOne()
            .HasForeignKey("PlayerProfileId");

        // Configure GameSession
        modelBuilder.Entity<GameSession>()
            .Property(g => g.RewardAmount)
            .HasPrecision(18, 8);

        modelBuilder.Entity<PlayerProfile>()
            .Property(p => p.TotalRewardsEarned)
            .HasPrecision(18, 8);

        // Indexes for performance
        modelBuilder.Entity<PlayerProfile>()
            .HasIndex(p => p.WalletAddress)
            .IsUnique();

        modelBuilder.Entity<GameSession>()
            .HasIndex(g => g.PlayerAddress);
    }
}
