// MetaMask Interoperability for Crypto Hockey
window.metamaskInterop = {
    // Check if MetaMask is installed
    isMetaMaskInstalled: function () {
        return typeof window.ethereum !== 'undefined' && window.ethereum.isMetaMask;
    },

    // Connect to MetaMask wallet
    connectWallet: async function () {
        try {
            if (!this.isMetaMaskInstalled()) {
                return {
                    isConnected: false,
                    address: null,
                    chainId: 0,
                    chainName: 'Unknown',
                    balance: 0
                };
            }

            // Request account access
            const accounts = await window.ethereum.request({
                method: 'eth_requestAccounts'
            });

            if (!accounts || accounts.length === 0) {
                return {
                    isConnected: false,
                    address: null,
                    chainId: 0,
                    chainName: 'Unknown',
                    balance: 0
                };
            }

            const address = accounts[0];
            const chainId = await window.ethereum.request({ method: 'eth_chainId' });
            const chainIdNumber = parseInt(chainId, 16);
            const chainName = this.getChainName(chainIdNumber);

            // Get balance
            const balance = await window.ethereum.request({
                method: 'eth_getBalance',
                params: [address, 'latest']
            });

            const balanceInEther = parseInt(balance, 16) / Math.pow(10, 18);

            return {
                isConnected: true,
                address: address,
                chainId: chainIdNumber,
                chainName: chainName,
                balance: balanceInEther
            };
        } catch (error) {
            console.error('Error connecting wallet:', error);
            return {
                isConnected: false,
                address: null,
                chainId: 0,
                chainName: 'Unknown',
                balance: 0
            };
        }
    },

    // Get current wallet state
    getWalletState: async function () {
        try {
            if (!this.isMetaMaskInstalled()) {
                return {
                    isConnected: false,
                    address: null,
                    chainId: 0,
                    chainName: 'Unknown',
                    balance: 0
                };
            }

            const accounts = await window.ethereum.request({
                method: 'eth_accounts'
            });

            if (!accounts || accounts.length === 0) {
                return {
                    isConnected: false,
                    address: null,
                    chainId: 0,
                    chainName: 'Unknown',
                    balance: 0
                };
            }

            const address = accounts[0];
            const chainId = await window.ethereum.request({ method: 'eth_chainId' });
            const chainIdNumber = parseInt(chainId, 16);
            const chainName = this.getChainName(chainIdNumber);

            const balance = await window.ethereum.request({
                method: 'eth_getBalance',
                params: [address, 'latest']
            });

            const balanceInEther = parseInt(balance, 16) / Math.pow(10, 18);

            return {
                isConnected: true,
                address: address,
                chainId: chainIdNumber,
                chainName: chainName,
                balance: balanceInEther
            };
        } catch (error) {
            console.error('Error getting wallet state:', error);
            return {
                isConnected: false,
                address: null,
                chainId: 0,
                chainName: 'Unknown',
                balance: 0
            };
        }
    },

    // Disconnect wallet
    disconnectWallet: function () {
        try {
            // Note: MetaMask doesn't have a built-in disconnect method
            // We just clear our local state; user must disconnect in MetaMask UI
            console.log('Wallet disconnected from application');
        } catch (error) {
            console.error('Error disconnecting wallet:', error);
        }
    },

    // Switch to a different network
    switchNetwork: async function (chainId) {
        try {
            if (!this.isMetaMaskInstalled()) {
                return false;
            }

            const hexChainId = '0x' + chainId.toString(16);

            try {
                await window.ethereum.request({
                    method: 'wallet_switchEthereumChain',
                    params: [{ chainId: hexChainId }],
                });
                return true;
            } catch (switchError) {
                // This error code indicates that the chain has not been added to MetaMask
                if (switchError.code === 4902) {
                    const chainData = this.getChainData(chainId);
                    if (chainData) {
                        await window.ethereum.request({
                            method: 'wallet_addEthereumChain',
                            params: [chainData],
                        });
                        return true;
                    }
                }
                throw switchError;
            }
        } catch (error) {
            console.error('Error switching network:', error);
            return false;
        }
    },

    // Helper: Get chain name
    getChainName: function (chainId) {
        const chains = {
            1: 'Ethereum Mainnet',
            11155111: 'Sepolia Testnet',
            137: 'Polygon Mainnet',
            80001: 'Polygon Mumbai'
        };
        return chains[chainId] || 'Unknown Network';
    },

    // Helper: Get chain configuration for adding to MetaMask
    getChainData: function (chainId) {
        const chainDataMap = {
            11155111: {
                chainId: '0xaa36a7',
                chainName: 'Sepolia',
                nativeCurrency: { name: 'ETH', symbol: 'ETH', decimals: 18 },
                rpcUrls: ['https://eth-sepolia.g.alchemy.com/v2/demo'],
                blockExplorerUrls: ['https://sepolia.etherscan.io']
            },
            137: {
                chainId: '0x89',
                chainName: 'Polygon',
                nativeCurrency: { name: 'MATIC', symbol: 'MATIC', decimals: 18 },
                rpcUrls: ['https://polygon-rpc.com'],
                blockExplorerUrls: ['https://polygonscan.com']
            }
        };
        return chainDataMap[chainId] || null;
    },

    // Send a transaction
    sendTransaction: async function (to, value, data) {
        try {
            if (!this.isMetaMaskInstalled()) {
                throw new Error('MetaMask is not installed');
            }

            const accounts = await window.ethereum.request({
                method: 'eth_accounts'
            });

            if (!accounts || accounts.length === 0) {
                throw new Error('No accounts found');
            }

            const txHash = await window.ethereum.request({
                method: 'eth_sendTransaction',
                params: [{
                    from: accounts[0],
                    to: to,
                    value: value,
                    data: data
                }],
            });

            return txHash;
        } catch (error) {
            console.error('Error sending transaction:', error);
            throw error;
        }
    }
};
