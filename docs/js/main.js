// Crypto Hockey - UI wiring: wallet connection, game lifecycle, reward claim.

document.addEventListener("DOMContentLoaded", () => {
  const connectBtn = document.getElementById("connect-wallet-btn");
  const walletStatus = document.getElementById("wallet-status");
  const walletAddressEl = document.getElementById("wallet-address");
  const startBtn = document.getElementById("start-game-btn");
  const difficultySelect = document.getElementById("difficulty-select");
  const canvas = document.getElementById("rink");
  const scoreEl = document.getElementById("score");
  const gameOverPanel = document.getElementById("game-over-panel");
  const gameOverMessage = document.getElementById("game-over-message");
  const claimBtn = document.getElementById("claim-reward-btn");
  const claimStatus = document.getElementById("claim-status");
  const playAgainBtn = document.getElementById("play-again-btn");
  const balanceEl = document.getElementById("token-balance");

  let scoreInterval = null;

  async function refreshBalance() {
    if (!Wallet.isConnected()) return;
    try {
      const balance = await Rewards.getBalance();
      if (balance) {
        balanceEl.textContent = `${Number(balance.formatted).toLocaleString()} ${balance.symbol}`;
      }
    } catch (err) {
      console.warn("Unable to read token balance:", err);
      balanceEl.textContent = "—";
    }
  }

  connectBtn.addEventListener("click", async () => {
    connectBtn.disabled = true;
    connectBtn.textContent = "Connecting…";
    try {
      const { address } = await Wallet.connect();
      walletStatus.classList.add("connected");
      walletAddressEl.textContent = `${address.slice(0, 6)}…${address.slice(-4)}`;
      connectBtn.textContent = "Connected";
      startBtn.disabled = false;
      await refreshBalance();
    } catch (err) {
      alert(err.message || "Failed to connect wallet.");
      connectBtn.disabled = false;
      connectBtn.textContent = "Connect MetaMask";
    }
  });

  startBtn.addEventListener("click", () => {
    if (!Wallet.isConnected()) {
      alert("Please connect your MetaMask wallet first.");
      return;
    }
    beginGame();
  });

  playAgainBtn.addEventListener("click", () => {
    gameOverPanel.classList.add("hidden");
    claimStatus.textContent = "";
    claimBtn.disabled = false;
    beginGame();
  });

  function beginGame() {
    gameOverPanel.classList.add("hidden");
    startBtn.disabled = true;
    Game.init(canvas, {
      difficulty: difficultySelect.value,
      onGameOverCallback: handleGameOver,
    });
    Game.start();

    if (scoreInterval) clearInterval(scoreInterval);
    scoreInterval = setInterval(() => {
      const s = Game.getScore();
      scoreEl.textContent = `You ${s.player} — ${s.ai} AI`;
    }, 200);
  }

  function handleGameOver({ winner, score }) {
    clearInterval(scoreInterval);
    startBtn.disabled = false;
    gameOverPanel.classList.remove("hidden");
    gameOverMessage.textContent =
      winner === "player"
        ? `🏆 You won ${score.player} - ${score.ai}!`
        : `😔 AI won ${score.ai} - ${score.player}. Play again to earn tokens!`;

    // Reward is granted just for playing a full game, win or lose.
    claimBtn.disabled = false;
  }

  claimBtn.addEventListener("click", async () => {
    claimBtn.disabled = true;
    claimStatus.textContent = "Confirm the transaction in MetaMask…";
    try {
      await Rewards.claimPlayReward();
      claimStatus.textContent = `✅ Reward claimed! You should receive ~${window.CRYPTO_HOCKEY_CONFIG.rewardAmountLabel} A1870 tokens.`;
      await refreshBalance();
    } catch (err) {
      console.error(err);
      claimStatus.textContent = `⚠️ Could not claim reward: ${err.shortMessage || err.message || "unknown error"}`;
      claimBtn.disabled = false;
    }
  });

  if (!Wallet.isMetaMaskAvailable()) {
    connectBtn.textContent = "MetaMask not found";
    connectBtn.disabled = true;
    walletStatus.textContent = "Install MetaMask to play and earn A1870 tokens.";
  }
});
