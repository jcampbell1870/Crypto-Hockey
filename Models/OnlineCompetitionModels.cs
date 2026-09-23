namespace Crypto_Hockey.Models;

public enum OnlineHeadsUpStatus
{
    WaitingForOpponent,
    InProgress,
    Completed
}

public enum OnlineTournamentStatus
{
    Registration,
    InProgress,
    Completed
}

public class OnlinePlayerAlias
{
    public string WalletAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; }
}

public class OnlineHeadsUpMatch
{
    public string Id { get; set; } = string.Empty;
    public string HostWalletAddress { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string? ChallengerWalletAddress { get; set; }
    public string? ChallengerName { get; set; }
    public string? WinnerWalletAddress { get; set; }
    public string? WinnerName { get; set; }
    public OnlineHeadsUpStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OnlineTournamentMatch
{
    public string Id { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public int Slot { get; set; }
    public string? PlayerOneWalletAddress { get; set; }
    public string? PlayerOneName { get; set; }
    public string? PlayerTwoWalletAddress { get; set; }
    public string? PlayerTwoName { get; set; }
    public string? WinnerWalletAddress { get; set; }
    public string? WinnerName { get; set; }
    public bool IsComplete { get; set; }
}

public class OnlineTournament
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HostWalletAddress { get; set; } = string.Empty;
    public List<OnlinePlayerAlias> Entrants { get; set; } = [];
    public int SeatLimit { get; set; } = 8;
    public OnlineTournamentStatus Status { get; set; }
    public string? ChampionWalletAddress { get; set; }
    public string? ChampionName { get; set; }
    public List<List<OnlineTournamentMatch>> Rounds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OnlineLobbySnapshot
{
    public List<OnlineHeadsUpMatch> HeadsUpMatches { get; set; } = [];
    public List<OnlineTournament> Tournaments { get; set; } = [];
}
