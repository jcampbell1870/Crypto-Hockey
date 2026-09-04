using Nethereum.Web3;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Crypto_Hockey.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Crypto_Hockey.Services;

public interface IBlockchainService
{
    Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId);
    Task<decimal> GetTokenBalanceAsync(string walletAddress, int chainId);
    Task<bool> ValidateWalletAsync(string walletAddress);
    decimal GetRewardAmount();
    int GetDefaultChainId();
}

public class BlockchainService : IBlockchainService
{
    private readonly BlockchainConfig _config;
    private readonly ILogger<BlockchainService> _logger;
    private readonly HttpClient _httpClient;

    public BlockchainService(
        IOptions<BlockchainConfig> config,
        ILogger<BlockchainService> logger,
        HttpClient httpClient)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId)
    {
        try
        {
            if (!IsValidAddress(walletAddress))
                return false;

            if (string.IsNullOrWhiteSpace(_config.RewardIssuerUrl) ||
                string.IsNullOrWhiteSpace(_config.RewardVaultAddress))
            {
                _logger.LogWarning("Reward issuer or treasury is not configured");
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync(
                _config.RewardIssuerUrl,
                new RewardRequest(
                    walletAddress,
                    amount,
                    _config.TokenContractAddress,
                    _config.TokenSymbol,
                    _config.RewardVaultAddress,
                    chainId));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reward issuer returned {StatusCode}", response.StatusCode);
                return false;
            }

            _logger.LogInformation("Reward issued for {WalletAddress}", walletAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending reward: {ex.Message}");
            return false;
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

            // Call contract to get balance
            var balanceOfFunctionMessage = new BalanceOfFunction { Owner = walletAddress };
            var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

            var balance = await handler.QueryAsync<decimal>(
                _config.TokenContractAddress,
                balanceOfFunctionMessage);

            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting token balance: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> ValidateWalletAsync(string walletAddress)
    {
        return await Task.FromResult(IsValidAddress(walletAddress));
    }

    public decimal GetRewardAmount() => _config.RewardAmount;

    public int GetDefaultChainId() => _config.DefaultNetworkChainId;

    private sealed record RewardRequest(
        string WalletAddress,
        decimal Amount,
        string TokenContractAddress,
        string TokenSymbol,
        string RewardVaultAddress,
        int ChainId);

    private bool IsValidAddress(string address)
    {
        return !string.IsNullOrEmpty(address) && address.StartsWith("0x") && address.Length == 42;
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
