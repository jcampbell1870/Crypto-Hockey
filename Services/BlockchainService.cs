using System.Net.Http.Json;
using System.Text.Json;
using Crypto_Hockey.Models;
using Microsoft.Extensions.Options;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Web3;

namespace Crypto_Hockey.Services;

public interface IBlockchainService
{
    Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId);
    Task<RewardClaimResult> RequestRewardClaimAsync(string walletAddress, RewardGameProof gameProof);
    Task<decimal> GetTokenBalanceAsync(string walletAddress, int chainId);
    Task<bool> ValidateWalletAsync(string walletAddress);
}

public class BlockchainService : IBlockchainService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BlockchainConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(
        IOptions<BlockchainConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<BlockchainService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId)
    {
        if (!IsValidAddress(walletAddress))
        {
            return false;
        }

        var claim = await RequestRewardClaimAsync(walletAddress, new RewardGameProof
        {
            GameId = $"legacy-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Mode = "legacy",
            CompletedAt = DateTime.UtcNow,
            PlayerWon = true
        });

        if (!claim.IsSuccessful)
        {
            return false;
        }

        _logger.LogInformation(
            "Reward claim package issued for {WalletAddress} amount {Amount} on chain {ChainId}",
            walletAddress,
            amount,
            chainId);

        return true;
    }

    public async Task<RewardClaimResult> RequestRewardClaimAsync(string walletAddress, RewardGameProof gameProof)
    {
        try
        {
            if (!IsValidAddress(walletAddress))
            {
                return new RewardClaimResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Invalid wallet address."
                };
            }

            if (string.IsNullOrWhiteSpace(_config.RewardIssuerUrl))
            {
                _logger.LogWarning("Reward issuer URL is not configured; using offline success fallback.");
                return new RewardClaimResult { IsSuccessful = true };
            }

            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(_config.RewardIssuerUrl, new
            {
                recipient = walletAddress,
                game = new
                {
                    gameId = gameProof.GameId,
                    mode = gameProof.Mode,
                    playerScore = gameProof.PlayerScore,
                    opponentScore = gameProof.OpponentScore,
                    difficultyLevel = gameProof.DifficultyLevel,
                    completedAt = gameProof.CompletedAt,
                    playerWon = gameProof.PlayerWon
                }
            });

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Reward issuer rejected claim for {WalletAddress}. Status {StatusCode}: {Error}",
                    walletAddress,
                    (int)response.StatusCode,
                    rawError);

                return new RewardClaimResult
                {
                    IsSuccessful = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(rawError)
                        ? $"Issuer error {(int)response.StatusCode}."
                        : rawError
                };
            }

            var payload = await response.Content.ReadFromJsonAsync<RewardClaimPayload>(JsonOptions);
            if (payload == null)
            {
                return new RewardClaimResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Reward issuer response was empty."
                };
            }

            return new RewardClaimResult
            {
                IsSuccessful = true,
                Payload = payload
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reward issuer call failed for wallet {WalletAddress}", walletAddress);
            return new RewardClaimResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<decimal> GetTokenBalanceAsync(string walletAddress, int chainId)
    {
        try
        {
            if (!IsValidAddress(walletAddress))
                return 0;

            var rpcUrl = GetRpcUrlForChain(chainId);
            if (string.IsNullOrEmpty(rpcUrl))
                return 0;

            var web3 = new Web3(rpcUrl);

            var balanceOfFunctionMessage = new BalanceOfFunction { Owner = walletAddress };
            var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

            var balance = await handler.QueryAsync<decimal>(
                _config.Arcade1870ContractAddress,
                balanceOfFunctionMessage);

            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting token balance: {Message}", ex.Message);
            return 0;
        }
    }

    public async Task<bool> ValidateWalletAsync(string walletAddress)
    {
        return await Task.FromResult(IsValidAddress(walletAddress));
    }

    private static bool IsValidAddress(string address)
    {
        return !string.IsNullOrEmpty(address) && address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && address.Length == 42;
    }

    private string GetRpcUrlForChain(int chainId)
    {
        return chainId switch
        {
            1 => _config.EthereumRpcUrl,
            11155111 => _config.SepoliaRpcUrl,
            137 => _config.PolygonRpcUrl,
            _ => string.Empty
        };
    }
}
