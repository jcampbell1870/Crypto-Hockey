// Crypto Hockey - MetaMask / wallet connectivity
// Uses ethers.js (loaded via CDN in index.html) so the whole site remains
// static and can be published as-is through GitHub Pages.

const Wallet = (() => {
  let provider = null;
  let signer = null;
  let address = null;
  let chainId = null;

  function isMetaMaskAvailable() {
    return typeof window.ethereum !== "undefined";
  }

  async function connect() {
    if (!isMetaMaskAvailable()) {
      throw new Error(
        "MetaMask not detected. Please install the MetaMask browser extension."
      );
    }

    provider = new ethers.BrowserProvider(window.ethereum);

    const accounts = await provider.send("eth_requestAccounts", []);
    if (!accounts || accounts.length === 0) {
      throw new Error("No accounts returned by MetaMask.");
    }

    signer = await provider.getSigner();
    address = await signer.getAddress();

    const network = await provider.getNetwork();
    chainId = Number(network.chainId);

    window.ethereum.on?.("accountsChanged", (accts) => {
      if (!accts || accts.length === 0) {
        disconnect();
        window.dispatchEvent(new CustomEvent("wallet:disconnected"));
      } else {
        window.dispatchEvent(
          new CustomEvent("wallet:accountChanged", { detail: accts[0] })
        );
      }
    });

    window.ethereum.on?.("chainChanged", () => {
      window.location.reload();
    });

    return { address, chainId };
  }

  function disconnect() {
    provider = null;
    signer = null;
    address = null;
    chainId = null;
  }

  function getSigner() {
    return signer;
  }

  function getProvider() {
    return provider;
  }

  function getAddress() {
    return address;
  }

  function getChainId() {
    return chainId;
  }

  function isConnected() {
    return Boolean(address);
  }

  return {
    isMetaMaskAvailable,
    connect,
    disconnect,
    getSigner,
    getProvider,
    getAddress,
    getChainId,
    isConnected,
  };
})();
