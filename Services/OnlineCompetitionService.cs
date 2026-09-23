using Crypto_Hockey.Models;

namespace Crypto_Hockey.Services;

public interface IOnlineCompetitionService
{
    Task<OnlinePlayerAlias> UpsertPlayerAsync(string walletAddress, string displayName);
    Task<OnlineLobbySnapshot> GetLobbySnapshotAsync();
    Task<OnlineHeadsUpMatch> CreateHeadsUpMatchAsync(string walletAddress, string displayName);
    Task<OnlineHeadsUpMatch> JoinHeadsUpMatchAsync(string matchId, string walletAddress, string displayName);
    Task<OnlineHeadsUpMatch> ReportHeadsUpWinnerAsync(string matchId, string winnerWalletAddress, string reporterWalletAddress);
    Task<OnlineTournament> CreateTournamentAsync(string walletAddress, string displayName);
    Task<OnlineTournament> JoinTournamentAsync(string tournamentId, string walletAddress, string displayName);
    Task<OnlineTournament> ReportTournamentWinnerAsync(string tournamentId, string matchId, string winnerWalletAddress, string reporterWalletAddress);
}

public class OnlineCompetitionService : IOnlineCompetitionService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, OnlinePlayerAlias> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OnlineHeadsUpMatch> _headsUpMatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OnlineTournament> _tournaments = new(StringComparer.OrdinalIgnoreCase);

    public Task<OnlinePlayerAlias> UpsertPlayerAsync(string walletAddress, string displayName)
    {
        lock (_syncRoot)
        {
            var normalizedWallet = NormalizeWallet(walletAddress);
            var normalizedName = NormalizeName(displayName, normalizedWallet);

            if (!_players.TryGetValue(normalizedWallet, out var alias))
            {
                alias = new OnlinePlayerAlias
                {
                    WalletAddress = normalizedWallet,
                    DisplayName = normalizedName,
                    LastSeenAt = DateTime.UtcNow
                };
                _players[normalizedWallet] = alias;
            }
            else
            {
                alias.DisplayName = normalizedName;
                alias.LastSeenAt = DateTime.UtcNow;
            }

            return Task.FromResult(CloneAlias(alias));
        }
    }

    public Task<OnlineLobbySnapshot> GetLobbySnapshotAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(new OnlineLobbySnapshot
            {
                HeadsUpMatches = _headsUpMatches.Values
                    .OrderByDescending(m => m.UpdatedAt)
                    .Take(20)
                    .Select(CloneHeadsUpMatch)
                    .ToList(),
                Tournaments = _tournaments.Values
                    .OrderByDescending(t => t.UpdatedAt)
                    .Take(10)
                    .Select(CloneTournament)
                    .ToList()
            });
        }
    }

    public Task<OnlineHeadsUpMatch> CreateHeadsUpMatchAsync(string walletAddress, string displayName)
    {
        lock (_syncRoot)
        {
            var alias = UpsertPlayerInternal(walletAddress, displayName);

            var match = new OnlineHeadsUpMatch
            {
                Id = $"hu-{Guid.NewGuid():N}"[..11],
                HostWalletAddress = alias.WalletAddress,
                HostName = alias.DisplayName,
                Status = OnlineHeadsUpStatus.WaitingForOpponent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _headsUpMatches[match.Id] = match;
            return Task.FromResult(CloneHeadsUpMatch(match));
        }
    }

    public Task<OnlineHeadsUpMatch> JoinHeadsUpMatchAsync(string matchId, string walletAddress, string displayName)
    {
        lock (_syncRoot)
        {
            if (!_headsUpMatches.TryGetValue(matchId, out var match))
            {
                throw new InvalidOperationException("Heads-up table not found.");
            }

            if (match.Status != OnlineHeadsUpStatus.WaitingForOpponent)
            {
                throw new InvalidOperationException("This heads-up table is no longer open.");
            }

            var alias = UpsertPlayerInternal(walletAddress, displayName);
            if (string.Equals(alias.WalletAddress, match.HostWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You cannot join your own heads-up table.");
            }

            match.ChallengerWalletAddress = alias.WalletAddress;
            match.ChallengerName = alias.DisplayName;
            match.Status = OnlineHeadsUpStatus.InProgress;
            match.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(CloneHeadsUpMatch(match));
        }
    }

    public Task<OnlineHeadsUpMatch> ReportHeadsUpWinnerAsync(string matchId, string winnerWalletAddress, string reporterWalletAddress)
    {
        lock (_syncRoot)
        {
            if (!_headsUpMatches.TryGetValue(matchId, out var match))
            {
                throw new InvalidOperationException("Heads-up table not found.");
            }

            if (match.Status != OnlineHeadsUpStatus.InProgress)
            {
                throw new InvalidOperationException("Winner can only be reported for active heads-up tables.");
            }

            var normalizedReporter = NormalizeWallet(reporterWalletAddress);
            var participants = new[] { match.HostWalletAddress, match.ChallengerWalletAddress }
                .Where(wallet => !string.IsNullOrWhiteSpace(wallet))
                .Select(wallet => NormalizeWallet(wallet!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!participants.Contains(normalizedReporter))
            {
                throw new InvalidOperationException("Only seated players can report a heads-up result.");
            }

            var normalizedWinner = NormalizeWallet(winnerWalletAddress);
            if (!participants.Contains(normalizedWinner))
            {
                throw new InvalidOperationException("Winner must be one of the seated players.");
            }

            match.WinnerWalletAddress = normalizedWinner;
            match.WinnerName = ResolveDisplayName(normalizedWinner);
            match.Status = OnlineHeadsUpStatus.Completed;
            match.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(CloneHeadsUpMatch(match));
        }
    }

    public Task<OnlineTournament> CreateTournamentAsync(string walletAddress, string displayName)
    {
        lock (_syncRoot)
        {
            var alias = UpsertPlayerInternal(walletAddress, displayName);
            var tournament = new OnlineTournament
            {
                Id = $"trn-{Guid.NewGuid():N}"[..12],
                Name = $"{alias.DisplayName}'s 8-Max",
                HostWalletAddress = alias.WalletAddress,
                Status = OnlineTournamentStatus.Registration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Entrants = [CloneAlias(alias)]
            };

            _tournaments[tournament.Id] = tournament;
            return Task.FromResult(CloneTournament(tournament));
        }
    }

    public Task<OnlineTournament> JoinTournamentAsync(string tournamentId, string walletAddress, string displayName)
    {
        lock (_syncRoot)
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                throw new InvalidOperationException("Tournament not found.");
            }

            var alias = UpsertPlayerInternal(walletAddress, displayName);

            if (tournament.Status != OnlineTournamentStatus.Registration)
            {
                throw new InvalidOperationException("This tournament has already started.");
            }

            if (tournament.Entrants.Any(e => string.Equals(e.WalletAddress, alias.WalletAddress, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(CloneTournament(tournament));
            }

            if (tournament.Entrants.Count >= tournament.SeatLimit)
            {
                throw new InvalidOperationException("Tournament is full.");
            }

            tournament.Entrants.Add(CloneAlias(alias));
            tournament.UpdatedAt = DateTime.UtcNow;

            if (tournament.Entrants.Count == tournament.SeatLimit)
            {
                StartTournamentInternal(tournament);
            }

            return Task.FromResult(CloneTournament(tournament));
        }
    }

    public Task<OnlineTournament> ReportTournamentWinnerAsync(string tournamentId, string matchId, string winnerWalletAddress, string reporterWalletAddress)
    {
        lock (_syncRoot)
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                throw new InvalidOperationException("Tournament not found.");
            }

            if (tournament.Status != OnlineTournamentStatus.InProgress)
            {
                throw new InvalidOperationException("Tournament is not currently in play.");
            }

            var match = tournament.Rounds
                .SelectMany(round => round)
                .FirstOrDefault(m => string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                throw new InvalidOperationException("Tournament match not found.");
            }

            if (match.IsComplete)
            {
                throw new InvalidOperationException("This match is already complete.");
            }

            var reporter = NormalizeWallet(reporterWalletAddress);
            var matchPlayers = new[] { match.PlayerOneWalletAddress, match.PlayerTwoWalletAddress }
                .Where(wallet => !string.IsNullOrWhiteSpace(wallet))
                .Select(wallet => NormalizeWallet(wallet!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!matchPlayers.Contains(reporter))
            {
                throw new InvalidOperationException("Only seated tournament players can report match results.");
            }

            var normalizedWinner = NormalizeWallet(winnerWalletAddress);
            if (!matchPlayers.Contains(normalizedWinner))
            {
                throw new InvalidOperationException("Winner must be seated in the selected tournament match.");
            }

            match.WinnerWalletAddress = normalizedWinner;
            match.WinnerName = ResolveDisplayName(normalizedWinner);
            match.IsComplete = true;
            tournament.UpdatedAt = DateTime.UtcNow;

            AdvanceTournamentInternal(tournament, match.RoundNumber);
            return Task.FromResult(CloneTournament(tournament));
        }
    }

    private void StartTournamentInternal(OnlineTournament tournament)
    {
        var shuffled = tournament.Entrants
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var firstRound = new List<OnlineTournamentMatch>();
        for (var index = 0; index < shuffled.Count; index += 2)
        {
            firstRound.Add(new OnlineTournamentMatch
            {
                Id = $"tm-{Guid.NewGuid():N}"[..11],
                RoundNumber = 1,
                Slot = (index / 2) + 1,
                PlayerOneWalletAddress = shuffled[index].WalletAddress,
                PlayerOneName = shuffled[index].DisplayName,
                PlayerTwoWalletAddress = shuffled[index + 1].WalletAddress,
                PlayerTwoName = shuffled[index + 1].DisplayName
            });
        }

        tournament.Rounds = [firstRound];
        tournament.Status = OnlineTournamentStatus.InProgress;
        tournament.UpdatedAt = DateTime.UtcNow;
    }

    private void AdvanceTournamentInternal(OnlineTournament tournament, int completedRoundNumber)
    {
        var completedRound = tournament.Rounds.FirstOrDefault(round => round.FirstOrDefault()?.RoundNumber == completedRoundNumber);
        if (completedRound == null || completedRound.Any(match => !match.IsComplete))
        {
            return;
        }

        var winners = completedRound
            .Select(match => match.WinnerWalletAddress)
            .Where(wallet => !string.IsNullOrWhiteSpace(wallet))
            .Select(wallet => NormalizeWallet(wallet!))
            .ToList();

        if (winners.Count == 1)
        {
            tournament.ChampionWalletAddress = winners[0];
            tournament.ChampionName = ResolveDisplayName(winners[0]);
            tournament.Status = OnlineTournamentStatus.Completed;
            return;
        }

        if (winners.Count < 2 || winners.Count % 2 != 0)
        {
            return;
        }

        var nextRoundNumber = completedRoundNumber + 1;
        if (tournament.Rounds.Any(round => round.FirstOrDefault()?.RoundNumber == nextRoundNumber))
        {
            return;
        }

        var nextRound = new List<OnlineTournamentMatch>();
        for (var index = 0; index < winners.Count; index += 2)
        {
            var playerOne = winners[index];
            var playerTwo = winners[index + 1];

            nextRound.Add(new OnlineTournamentMatch
            {
                Id = $"tm-{Guid.NewGuid():N}"[..11],
                RoundNumber = nextRoundNumber,
                Slot = (index / 2) + 1,
                PlayerOneWalletAddress = playerOne,
                PlayerOneName = ResolveDisplayName(playerOne),
                PlayerTwoWalletAddress = playerTwo,
                PlayerTwoName = ResolveDisplayName(playerTwo)
            });
        }

        tournament.Rounds.Add(nextRound);
    }

    private OnlinePlayerAlias UpsertPlayerInternal(string walletAddress, string displayName)
    {
        var normalizedWallet = NormalizeWallet(walletAddress);
        var normalizedName = NormalizeName(displayName, normalizedWallet);

        if (!_players.TryGetValue(normalizedWallet, out var alias))
        {
            alias = new OnlinePlayerAlias
            {
                WalletAddress = normalizedWallet,
                DisplayName = normalizedName,
                LastSeenAt = DateTime.UtcNow
            };
            _players[normalizedWallet] = alias;
        }
        else
        {
            alias.DisplayName = normalizedName;
            alias.LastSeenAt = DateTime.UtcNow;
        }

        return alias;
    }

    private string ResolveDisplayName(string walletAddress)
    {
        var normalizedWallet = NormalizeWallet(walletAddress);
        return _players.TryGetValue(normalizedWallet, out var alias)
            ? alias.DisplayName
            : ShortWallet(normalizedWallet);
    }

    private static string NormalizeWallet(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new InvalidOperationException("A connected wallet is required for online play.");
        }

        return walletAddress.Trim();
    }

    private static string NormalizeName(string displayName, string walletAddress)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var trimmed = displayName.Trim();
            return trimmed.Length <= 24 ? trimmed : trimmed[..24];
        }

        return ShortWallet(walletAddress);
    }

    private static string ShortWallet(string walletAddress)
    {
        return walletAddress.Length < 12 ? walletAddress : $"{walletAddress[..6]}...{walletAddress[^4..]}";
    }

    private static OnlinePlayerAlias CloneAlias(OnlinePlayerAlias alias)
    {
        return new OnlinePlayerAlias
        {
            WalletAddress = alias.WalletAddress,
            DisplayName = alias.DisplayName,
            LastSeenAt = alias.LastSeenAt
        };
    }

    private static OnlineHeadsUpMatch CloneHeadsUpMatch(OnlineHeadsUpMatch match)
    {
        return new OnlineHeadsUpMatch
        {
            Id = match.Id,
            HostWalletAddress = match.HostWalletAddress,
            HostName = match.HostName,
            ChallengerWalletAddress = match.ChallengerWalletAddress,
            ChallengerName = match.ChallengerName,
            WinnerWalletAddress = match.WinnerWalletAddress,
            WinnerName = match.WinnerName,
            Status = match.Status,
            CreatedAt = match.CreatedAt,
            UpdatedAt = match.UpdatedAt
        };
    }

    private static OnlineTournament CloneTournament(OnlineTournament tournament)
    {
        return new OnlineTournament
        {
            Id = tournament.Id,
            Name = tournament.Name,
            HostWalletAddress = tournament.HostWalletAddress,
            SeatLimit = tournament.SeatLimit,
            Status = tournament.Status,
            ChampionWalletAddress = tournament.ChampionWalletAddress,
            ChampionName = tournament.ChampionName,
            CreatedAt = tournament.CreatedAt,
            UpdatedAt = tournament.UpdatedAt,
            Entrants = tournament.Entrants.Select(CloneAlias).ToList(),
            Rounds = tournament.Rounds
                .Select(round => round.Select(match => new OnlineTournamentMatch
                {
                    Id = match.Id,
                    RoundNumber = match.RoundNumber,
                    Slot = match.Slot,
                    PlayerOneWalletAddress = match.PlayerOneWalletAddress,
                    PlayerOneName = match.PlayerOneName,
                    PlayerTwoWalletAddress = match.PlayerTwoWalletAddress,
                    PlayerTwoName = match.PlayerTwoName,
                    WinnerWalletAddress = match.WinnerWalletAddress,
                    WinnerName = match.WinnerName,
                    IsComplete = match.IsComplete
                }).ToList())
                .ToList()
        };
    }
}
