namespace Crypto_Hockey.Models;

public class GameSession
{
    public int Id { get; set; }
    public string PlayerAddress { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public string DifficultyLevel { get; set; } = "Medium";
    public bool PlayerWon { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public decimal RewardAmount { get; set; }
    public string? TransactionHash { get; set; }
    public bool RewardClaimed { get; set; }
}
