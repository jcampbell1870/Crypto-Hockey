// Crypto Hockey - ERC-20 (Arcade1870) reward handling.
// Runs entirely client-side against the connected player's own MetaMask
// signer, which is the only safe way to move tokens from a purely static
// GitHub Pages deployment (there is no backend/server to hold a private key).

const ERC20_ABI = [
  "function balanceOf(address owner) view returns (uint256)",
  "function decimals() view returns (uint8)",
  "function symbol() view returns (string)",
  "function transfer(address to, uint256 amount) returns (bool)",
  // Optional self-service "play to earn" claim function. Not part of the
  // standard ERC-20 interface, so calls to it are wrapped in try/catch and
  // fail gracefully if the deployed contract doesn't implement it.
  "function claimReward() returns (bool)",
];

const Rewards = (() => {
  function getContract() {
    const signer = Wallet.getSigner();
    if (!signer) throw new Error("Wallet is not connected.");
    const { tokenAddress } = window.CRYPTO_HOCKEY_CONFIG;
    return new ethers.Contract(tokenAddress, ERC20_ABI, signer);
  }

  async function getBalance() {
    const address = Wallet.getAddress();
    if (!address) return null;
    const contract = getContract();
    const [raw, decimals, symbol] = await Promise.all([
      contract.balanceOf(address),
      contract.decimals().catch(() => 18),
      contract.symbol().catch(() => "A1870"),
    ]);
    return { formatted: ethers.formatUnits(raw, decimals), symbol };
  }

  /**
   * Ask the connected player's wallet to call the token contract's
   * self-service reward/claim function. Returns the transaction receipt
   * on success, or throws if the contract doesn't support it / the user
   * rejects the transaction / the claim reverts (e.g. cooldown).
   */
  async function claimPlayReward() {
    const contract = getContract();
    const method = window.CRYPTO_HOCKEY_CONFIG.rewardClaimMethod;

    if (typeof contract[method] !== "function") {
      throw new Error(
        `Token contract does not expose a "${method}" function. ` +
          "Update js/config.js to match your deployed contract."
      );
    }

    const tx = await contract[method]();
    return await tx.wait();
  }

  return { getBalance, claimPlayReward };
})();
