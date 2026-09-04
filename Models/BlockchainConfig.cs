namespace Crypto_Hockey.Models;

public class BlockchainConfig
{
    public string TokenContractAddress { get; set; } = string.Empty;
    public string TokenSymbol { get; set; } = "1870Coin";
    public string RewardVaultAddress { get; set; } = string.Empty;
    public string RewardIssuerUrl { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; } = 10m;
    public string EthereumRpcUrl { get; set; } = string.Empty;
    public string SepoliaRpcUrl { get; set; } = string.Empty;
    public string PolygonRpcUrl { get; set; } = string.Empty;
    public int DefaultNetworkChainId { get; set; } = 1;
    public int[] SupportedChainIds { get; set; } = [];

    public string Arcade1870ContractAddress
    {
        get => TokenContractAddress;
        set => TokenContractAddress = value;
    }
}
