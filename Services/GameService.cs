using Crypto_Hockey.Models;
using Crypto_Hockey.Data;
using Microsoft.Extensions.Options;
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
    private readonly BlockchainConfig _blockchainConfig;
    private readonly ILogger<GameService> _logger;

    public GameService(
        GameDbContext context,
        IBlockchainService blockchainService,
        IOptions<BlockchainConfig> blockchainOptions,
        ILogger<GameService> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _blockchainConfig = blockchainOptions.Value;
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
            session.RewardAmount = ParseConfiguredRewardAmount();
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

        var rewardClaim = await _blockchainService.RequestRewardClaimAsync(
            session.PlayerAddress,
            new RewardGameProof
            {
                GameId = session.Id.ToString(),
                Mode = "hockey",
                PlayerScore = session.PlayerScore,
                OpponentScore = session.OpponentScore,
                DifficultyLevel = session.DifficultyLevel,
                CompletedAt = session.EndedAt,
                PlayerWon = session.PlayerWon
            });

        if (rewardClaim.IsSuccessful)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Entry(session).ReloadAsync();

            if (session.RewardClaimed)
            {
                await transaction.CommitAsync();
                return true;
            }

            session.RewardClaimed = true;
            session.TransactionHash = !string.IsNullOrWhiteSpace(rewardClaim.Payload?.Nonce)
                ? $"issuer-claim:{rewardClaim.Payload.Nonce}"
                : session.TransactionHash;
            var player = await _context.PlayerProfiles
                .FirstOrDefaultAsync(p => p.WalletAddress == session.PlayerAddress);

            if (player == null)
            {
                player = new PlayerProfile
                {
                    WalletAddress = session.PlayerAddress,
                    CreatedAt = DateTime.UtcNow,
                    LastPlayedAt = session.EndedAt,
                    TotalGames = 0,
                    TotalWins = 0,
                    TotalLosses = 0,
                    TotalRewardsEarned = 0
                };

                _context.PlayerProfiles.Add(player);
            }

            player.TotalRewardsEarned += session.RewardAmount;
            _context.GameSessions.Update(session);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Reward claim package issued for session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning(
                "Reward claim failed for session {SessionId}: {Reason}",
                sessionId,
                rewardClaim.ErrorMessage ?? "Unknown error");
        }

        return rewardClaim.IsSuccessful;
    }

    private decimal ParseConfiguredRewardAmount()
    {
        return decimal.TryParse(_blockchainConfig.RewardAmount, out var configuredAmount)
            ? configuredAmount
            : 10m;
    }
}
