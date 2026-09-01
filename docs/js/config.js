// Crypto Hockey - configuration
// Update these values to match your deployed Arcade1870 token contract.
window.CRYPTO_HOCKEY_CONFIG = {
  // Arcade1870 (A1870) ERC-20 token contract address.
  tokenAddress: "0x8eddD4edea39c5B5f77662453600F53A202EE47C",

  // Chain IDs the game will accept a connection on (empty array = any chain).
  // 1 = Ethereum Mainnet, 11155111 = Sepolia, 137 = Polygon.
  supportedChainIds: [1, 11155111, 137],

  // Amount of A1870 tokens awarded per completed game (used only for display;
  // the actual amount minted/transferred is controlled by the token contract).
  rewardAmountLabel: "10",

  // Name of the public, self-service contract method players call with their own
  // wallet to collect a "play to earn" reward. Because GitHub Pages only serves
  // static files (no server-side signer), the reward MUST be claimable by the
  // token contract itself (e.g. a public faucet/claim/reward function) rather
  // than pushed by a backend. Update this if your contract uses a different name.
  rewardClaimMethod: "claimReward",
};
