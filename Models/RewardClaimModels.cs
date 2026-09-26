namespace Crypto_Hockey.Models;

public class RewardGameProof
{
    public string GameId { get; set; } = string.Empty;
    public string Mode { get; set; } = "hockey";
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public string DifficultyLevel { get; set; } = "Medium";
    public DateTime CompletedAt { get; set; }
    public bool PlayerWon { get; set; }
}

public class RewardClaimPayload
{
    public string Amount { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public long Deadline { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string VaultAddress { get; set; } = string.Empty;
    public int ChainId { get; set; }
}

public class RewardClaimResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DiagnosticHint { get; set; }
    public RewardClaimPayload? Payload { get; set; }
}

public class RewardClaimTransactionRequest
{
    public string VaultAddress { get; set; } = string.Empty;
    public int ChainId { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class PreparedRewardClaimResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DiagnosticHint { get; set; }
    public RewardClaimPayload? Payload { get; set; }
    public RewardClaimTransactionRequest? Transaction { get; set; }
}

public class WalletTransactionResult
{
    public bool IsSuccessful { get; set; }
    public string? TransactionHash { get; set; }
    public string? ErrorMessage { get; set; }
}
