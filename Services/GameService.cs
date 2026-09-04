using Crypto_Hockey.Models;
using Crypto_Hockey.Data;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Hockey.Services;

public interface IGameService
{
    Task<GameSession> CreateGameSessionAsync(string playerAddress, string difficultyLevel);
    Task<GameSession> EndGameSessionAsync(int sessionId, int playerScore, int opponentScore);
    Task<PlayerProfile> GetOrCreatePlayerAsync(string walletAddress);
    Task<List<GameSession>> GetPlayerGameHistoryAsync(string walletAddress, int limit = 10);
    Task<List<PlayerProfile>> GetLeaderboardAsync(int limit = 10);
    Task<bool> ClaimRewardAsync(int sessionId);
}

public class GameService : IGameService
{
    private readonly GameDbContext _context;
    private readonly IBlockchainService _blockchainService;
    private readonly ILogger<GameService> _logger;

    public GameService(GameDbContext context, IBlockchainService blockchainService, ILogger<GameService> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _logger = logger;
    }

    public async Task<GameSession> CreateGameSessionAsync(string playerAddress, string difficultyLevel)
    {
        var session = new GameSession
        {
            PlayerAddress = playerAddress,
            DifficultyLevel = difficultyLevel,
            StartedAt = DateTime.UtcNow,
            PlayerScore = 0,
            OpponentScore = 0,
            RewardClaimed = false
        };

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<GameSession> EndGameSessionAsync(int sessionId, int playerScore, int opponentScore)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Game session {sessionId} not found");

        session.EndedAt = DateTime.UtcNow;
        session.PlayerScore = playerScore;
        session.OpponentScore = opponentScore;
        session.PlayerWon = playerScore > opponentScore;

        if (session.PlayerWon)
        {
            session.RewardAmount = _blockchainService.GetRewardAmount();
        }

        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();

        // Update player profile
        var player = await GetOrCreatePlayerAsync(session.PlayerAddress);
        player.TotalGames++;
        player.LastPlayedAt = DateTime.UtcNow;

        if (session.PlayerWon)
        {
            player.TotalWins++;
            player.TotalRewardsEarned += session.RewardAmount;
        }
        else
        {
            player.TotalLosses++;
        }

        _context.PlayerProfiles.Update(player);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<PlayerProfile> GetOrCreatePlayerAsync(string walletAddress)
    {
        var player = await _context.PlayerProfiles
            .FirstOrDefaultAsync(p => p.WalletAddress == walletAddress);

        if (player == null)
        {
            player = new PlayerProfile
            {
                WalletAddress = walletAddress,
                CreatedAt = DateTime.UtcNow,
                TotalGames = 0,
                TotalWins = 0,
                TotalLosses = 0,
                TotalRewardsEarned = 0
            };

            _context.PlayerProfiles.Add(player);
            await _context.SaveChangesAsync();
        }

        return player;
    }

    public async Task<List<GameSession>> GetPlayerGameHistoryAsync(string walletAddress, int limit = 10)
    {
        return await _context.GameSessions
            .Where(g => g.PlayerAddress == walletAddress)
            .OrderByDescending(g => g.EndedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<PlayerProfile>> GetLeaderboardAsync(int limit = 10)
    {
        return await _context.PlayerProfiles
            .OrderByDescending(p => p.TotalWins)
            .ThenByDescending(p => p.TotalRewardsEarned)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> ClaimRewardAsync(int sessionId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null || session.RewardClaimed || !session.PlayerWon)
            return false;

        // Attempt to send reward
        var success = await _blockchainService.SendRewardAsync(
            session.PlayerAddress,
            session.RewardAmount,
            _blockchainService.GetDefaultChainId());

        if (success)
        {
            session.RewardClaimed = true;
            _context.GameSessions.Update(session);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Reward claimed for session {sessionId}");
        }

        return success;
    }
}
