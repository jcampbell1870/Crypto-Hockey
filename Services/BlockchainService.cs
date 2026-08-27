using Nethereum.Web3;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Crypto_Hockey.Models;
using Microsoft.Extensions.Options;

namespace Crypto_Hockey.Services;

public interface IBlockchainService
{
    Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId);
    Task<decimal> GetTokenBalanceAsync(string walletAddress, int chainId);
    Task<bool> ValidateWalletAsync(string walletAddress);
}

public class BlockchainService : IBlockchainService
{
    private readonly BlockchainConfig _config;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(IOptions<BlockchainConfig> config, ILogger<BlockchainService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<bool> SendRewardAsync(string walletAddress, decimal amount, int chainId)
    {
        try
        {
            if (!IsValidAddress(walletAddress))
                return false;

            var rpcUrl = GetRpcUrlForChain(chainId);
            if (string.IsNullOrEmpty(rpcUrl))
            {
                _logger.LogError($"No RPC URL configured for chain {chainId}");
                return false;
            }

            var web3 = new Web3(rpcUrl);

            // Note: In production, you would need a backend wallet to send tokens
            // This is a placeholder showing the structure
            // For now, rewards are recorded in the database
            _logger.LogInformation($"Reward of {amount} tokens prepared for {walletAddress} on chain {chainId}");

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
                _config.Arcade1870ContractAddress,
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
