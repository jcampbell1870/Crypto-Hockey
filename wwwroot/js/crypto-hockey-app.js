(function () {
    const defaultWalletState = {
        isConnected: false,
        address: null,
        chainId: 0,
        chainName: "Unknown",
        balance: 0
    };

    function shortWallet(address) {
        if (!address || address.length < 10) {
            return address || "";
        }
        return `${address.slice(0, 6)}...${address.slice(-4)}`;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function showMessage(element, message, isError = false) {
        if (!element) return;
        if (!message) {
            element.textContent = "";
            element.classList.add("hidden");
            element.classList.remove("status-error");
            return;
        }

        element.textContent = message;
        element.classList.remove("hidden");
        element.classList.toggle("status-error", isError);
    }

    async function apiFetch(url, options = {}) {
        const response = await fetch(url, {
            headers: {
                "Content-Type": "application/json",
                ...(options.headers || {})
            },
            ...options
        });

        const contentType = response.headers.get("content-type") || "";
        const payload = contentType.includes("application/json")
            ? await response.json()
            : await response.text();

        if (!response.ok) {
            const detail = typeof payload === "string"
                ? payload
                : payload.detail || payload.error_message || "Request failed.";
            throw new Error(detail);
        }

        return payload;
    }

    function createWalletController(onStateChange) {
        const disconnected = document.getElementById("wallet-disconnected");
        const connected = document.getElementById("wallet-connected");
        const warning = document.getElementById("wallet-warning");
        const connectButton = document.getElementById("connect-wallet");
        const switchButton = document.getElementById("switch-network");
        const disconnectButton = document.getElementById("disconnect-wallet");
        const copyButton = document.getElementById("copy-address");
        const address = document.getElementById("wallet-address");
        const network = document.getElementById("wallet-network");
        const balance = document.getElementById("wallet-balance");
        let walletState = { ...defaultWalletState };

        async function render() {
            const hasMetaMask = Boolean(window.metamaskInterop?.isMetaMaskInstalled?.());
            warning?.classList.toggle("hidden", hasMetaMask);
            disconnected?.classList.toggle("hidden", walletState.isConnected);
            connected?.classList.toggle("hidden", !walletState.isConnected);

            if (walletState.isConnected) {
                address.textContent = shortWallet(walletState.address);
                network.textContent = walletState.chainName || "Unknown";
                balance.textContent = `${Number(walletState.balance || 0).toFixed(4)} ETH`;
            }

            await onStateChange?.(walletState);
        }

        async function syncWallet() {
            if (window.metamaskInterop?.getWalletState) {
                walletState = await window.metamaskInterop.getWalletState();
            }
            await render();
        }

        connectButton?.addEventListener("click", async () => {
            walletState = window.metamaskInterop?.connectWallet
                ? await window.metamaskInterop.connectWallet()
                : { ...defaultWalletState };
            await render();
        });

        switchButton?.addEventListener("click", async () => {
            const switched = await window.metamaskInterop?.switchNetwork?.(11155111);
            if (switched) {
                await syncWallet();
            }
        });

        disconnectButton?.addEventListener("click", async () => {
            window.metamaskInterop?.disconnectWallet?.();
            walletState = { ...defaultWalletState };
            await render();
        });

        copyButton?.addEventListener("click", async () => {
            if (walletState.address) {
                await navigator.clipboard.writeText(walletState.address);
            }
        });

        return {
            syncWallet,
            getWalletState() {
                return walletState;
            }
        };
    }

    class GameEngine {
        constructor() {
            this.canvasWidth = 800;
            this.canvasHeight = 400;
            this.puckRadius = 5;
            this.paddleWidth = 10;
            this.paddleHeight = 80;
            this.paddleSpeed = 300;
            this.initialPuckSpeed = 200;
            this.maxPuckSpeed = 500;
            this.aiReactionTime = 0;
            this.difficulty = "Medium";
            this.state = this.createState();
        }

        createState() {
            return {
                puckX: this.canvasWidth / 2,
                puckY: this.canvasHeight / 2,
                puckVelocityX: this.initialPuckSpeed,
                puckVelocityY: this.initialPuckSpeed * 0.5,
                playerPaddleY: this.canvasHeight / 2 - this.paddleHeight / 2,
                opponentPaddleY: this.canvasHeight / 2 - this.paddleHeight / 2,
                playerScore: 0,
                opponentScore: 0,
                gameOver: false,
                winner: null
            };
        }

        initialize(difficulty) {
            this.difficulty = difficulty;
            this.aiReactionTime = 0;
            this.state = this.createState();
        }

        updatePaddle(yPosition) {
            const constrained = Math.max(0, Math.min(yPosition, this.canvasHeight - this.paddleHeight));
            this.state.playerPaddleY = constrained;
        }

        update(deltaTime) {
            if (this.state.gameOver) return;

            this.state.puckX += this.state.puckVelocityX * deltaTime;
            this.state.puckY += this.state.puckVelocityY * deltaTime;

            if (this.state.puckY - this.puckRadius <= 0 || this.state.puckY + this.puckRadius >= this.canvasHeight) {
                this.state.puckVelocityY = -this.state.puckVelocityY;
                this.state.puckY = Math.max(this.puckRadius, Math.min(this.state.puckY, this.canvasHeight - this.puckRadius));
            }

            this.checkPaddleCollision();

            if (this.state.puckX - this.puckRadius <= 0) {
                this.state.opponentScore += 1;
                this.resetPuck();
            } else if (this.state.puckX + this.puckRadius >= this.canvasWidth) {
                this.state.playerScore += 1;
                this.resetPuck();
            }

            this.updateAI(deltaTime);

            if (this.state.playerScore >= 5) {
                this.state.gameOver = true;
                this.state.winner = "Player";
            } else if (this.state.opponentScore >= 5) {
                this.state.gameOver = true;
                this.state.winner = "Opponent";
            }
        }

        checkPaddleCollision() {
            const state = this.state;
            if (
                state.puckX - this.puckRadius <= this.paddleWidth + 10 &&
                state.puckY >= state.playerPaddleY &&
                state.puckY <= state.playerPaddleY + this.paddleHeight &&
                state.puckVelocityX < 0
            ) {
                state.puckVelocityX = -state.puckVelocityX;
                state.puckVelocityY += (state.puckY - (state.playerPaddleY + this.paddleHeight / 2)) * 0.1;
                state.puckX = this.paddleWidth + this.puckRadius + 10;
                this.increasePuckSpeed();
            }

            if (
                state.puckX + this.puckRadius >= this.canvasWidth - this.paddleWidth - 10 &&
                state.puckY >= state.opponentPaddleY &&
                state.puckY <= state.opponentPaddleY + this.paddleHeight &&
                state.puckVelocityX > 0
            ) {
                state.puckVelocityX = -state.puckVelocityX;
                state.puckVelocityY += (state.puckY - (state.opponentPaddleY + this.paddleHeight / 2)) * 0.1;
                state.puckX = this.canvasWidth - this.paddleWidth - this.puckRadius - 10;
                this.increasePuckSpeed();
            }
        }

        updateAI(deltaTime) {
            this.aiReactionTime += deltaTime;
            const reactionDelay = this.difficulty === "Easy" ? 0.5 : this.difficulty === "Hard" ? 0.05 : 0.2;
            if (this.aiReactionTime < reactionDelay) {
                return;
            }

            const targetY = this.state.puckY - this.paddleHeight / 2;
            const moveSpeed = this.difficulty === "Easy"
                ? this.paddleSpeed * 0.6
                : this.difficulty === "Hard"
                    ? this.paddleSpeed
                    : this.paddleSpeed * 0.85;

            if (this.state.opponentPaddleY < targetY) {
                this.state.opponentPaddleY += moveSpeed * reactionDelay;
            } else if (this.state.opponentPaddleY > targetY) {
                this.state.opponentPaddleY -= moveSpeed * reactionDelay;
            }

            this.state.opponentPaddleY = Math.max(0, Math.min(this.state.opponentPaddleY, this.canvasHeight - this.paddleHeight));
            this.aiReactionTime = 0;
        }

        increasePuckSpeed() {
            const speed = Math.sqrt(this.state.puckVelocityX ** 2 + this.state.puckVelocityY ** 2);
            if (speed >= this.maxPuckSpeed) {
                return;
            }

            const nextSpeed = Math.min(speed * 1.05, this.maxPuckSpeed);
            const angle = Math.atan2(this.state.puckVelocityY, this.state.puckVelocityX);
            this.state.puckVelocityX = Math.cos(angle) * nextSpeed;
            this.state.puckVelocityY = Math.sin(angle) * nextSpeed;
        }

        resetPuck() {
            this.state.puckX = this.canvasWidth / 2;
            this.state.puckY = this.canvasHeight / 2;
            this.state.puckVelocityX = (Math.random() < 0.5 ? 1 : -1) * this.initialPuckSpeed;
            this.state.puckVelocityY = (Math.floor(Math.random() * 3) - 1) * this.initialPuckSpeed * 0.25;
        }
    }

    function initializeLeaderboardPage() {
        const refreshButton = document.getElementById("refresh-leaderboard");
        const body = document.getElementById("leaderboard-body");
        if (!refreshButton || !body) return;

        function renderRows(players) {
            body.innerHTML = players.map((player, index) => {
                const rankBadge = index === 0
                    ? '<span class="badge badge-gold">🥇 1st</span>'
                    : index === 1
                        ? '<span class="badge badge-silver">🥈 2nd</span>'
                        : index === 2
                            ? '<span class="badge badge-bronze">🥉 3rd</span>'
                            : `<span class="badge bg-secondary">${index + 1}</span>`;
                return `
                    <tr class="${index < 3 ? `top-${index + 1}` : ""}">
                        <td class="rank-col">${rankBadge}</td>
                        <td class="address-col">${player.wallet_address}</td>
                        <td class="stats-col text-center"><span class="wins-badge">${player.total_wins}</span></td>
                        <td class="stats-col text-center">${player.total_games}</td>
                        <td class="stats-col text-center"><span class="winrate">${Number(player.win_rate).toFixed(1)}%</span></td>
                        <td class="rewards-col text-right"><span class="rewards">${Number(player.total_rewards_earned).toFixed(2)}</span><span class="token-symbol">A1870</span></td>
                    </tr>
                `;
            }).join("");
        }

        refreshButton.addEventListener("click", async () => {
            const players = await apiFetch("/api/leaderboard?limit=50");
            renderRows(players);
        });
    }

    function initializeGamePage() {
        const canvas = document.getElementById("game-canvas");
        if (!canvas) return;

        const message = document.getElementById("game-message");
        const stats = document.getElementById("player-stats");
        const difficulty = document.getElementById("difficulty");
        const startButton = document.getElementById("start-game");
        const pauseButton = document.getElementById("pause-game");
        const resumeButton = document.getElementById("resume-game");
        const resetButton = document.getElementById("reset-game");
        const claimButton = document.getElementById("claim-reward");
        const playerScore = document.getElementById("player-score");
        const opponentScore = document.getElementById("opponent-score");
        const gameOverModal = document.getElementById("game-over-modal");
        const gameOverTitle = document.getElementById("game-over-title");
        const gameOverScore = document.getElementById("game-over-score");
        const gameOverDifficulty = document.getElementById("game-over-difficulty");
        const rewardMessage = document.getElementById("reward-message");

        const engine = new GameEngine();
        let walletState = { ...defaultWalletState };
        let running = false;
        let lastFrame = 0;
        let activeSessionId = null;
        let finalStateSubmitted = false;

        const wallet = createWalletController(async (state) => {
            walletState = state;
            startButton.disabled = !walletState.isConnected;
            if (walletState.isConnected && walletState.address) {
                const profile = await apiFetch(`/api/players/${encodeURIComponent(walletState.address)}`);
                stats.innerHTML = `
                    <p>Player: ${shortWallet(profile.wallet_address)}</p>
                    <p>Total Wins: ${profile.total_wins}</p>
                    <p>Total Games: ${profile.total_games}</p>
                    <p>Win Rate: ${Number(profile.win_rate).toFixed(1)}%</p>
                    <p>Total Rewards: ${Number(profile.total_rewards_earned).toFixed(2)} A1870</p>
                `;
            } else {
                stats.innerHTML = '<p class="text-muted">Connect wallet to see your stats.</p>';
            }
        });

        function syncButtons(state) {
            pauseButton.classList.toggle("hidden", !running || state.gameOver);
            resumeButton.classList.toggle("hidden", running || state.gameOver || !activeSessionId);
            resetButton.classList.toggle("hidden", !state.gameOver);
            claimButton.classList.toggle("hidden", !(state.gameOver && state.winner === "Player"));
        }

        function render() {
            window.gameRenderer.render(canvas, engine.state);
            playerScore.textContent = engine.state.playerScore;
            opponentScore.textContent = engine.state.opponentScore;
            syncButtons(engine.state);
        }

        async function submitFinalState() {
            if (finalStateSubmitted || !activeSessionId) return;
            await apiFetch(`/api/game-sessions/${activeSessionId}/complete`, {
                method: "POST",
                body: JSON.stringify({
                    player_score: engine.state.playerScore,
                    opponent_score: engine.state.opponentScore
                })
            });
            finalStateSubmitted = true;
            if (walletState.address) {
                const profile = await apiFetch(`/api/players/${encodeURIComponent(walletState.address)}`);
                stats.innerHTML = `
                    <p>Player: ${shortWallet(profile.wallet_address)}</p>
                    <p>Total Wins: ${profile.total_wins}</p>
                    <p>Total Games: ${profile.total_games}</p>
                    <p>Win Rate: ${Number(profile.win_rate).toFixed(1)}%</p>
                    <p>Total Rewards: ${Number(profile.total_rewards_earned).toFixed(2)} A1870</p>
                `;
            }
        }

        async function endGame() {
            running = false;
            await submitFinalState();
            gameOverModal.classList.remove("hidden");
            gameOverTitle.textContent = engine.state.winner === "Player" ? "🎉 YOU WON! 🎉" : "Game Over";
            rewardMessage.classList.toggle("hidden", engine.state.winner !== "Player");
            gameOverScore.textContent = `Final Score: ${engine.state.playerScore} - ${engine.state.opponentScore}`;
            gameOverDifficulty.textContent = `Difficulty: ${difficulty.value}`;
            syncButtons(engine.state);
        }

        function loop(timestamp) {
            if (!running) return;
            const delta = Math.min((timestamp - lastFrame) / 1000, 0.05);
            lastFrame = timestamp;
            engine.update(delta);
            render();
            if (engine.state.gameOver) {
                endGame().catch((error) => showMessage(message, error.message, true));
                return;
            }
            requestAnimationFrame(loop);
        }

        function startLoop() {
            running = true;
            lastFrame = performance.now();
            gameOverModal.classList.add("hidden");
            showMessage(message, "");
            requestAnimationFrame(loop);
            syncButtons(engine.state);
        }

        async function startGame() {
            if (!walletState.isConnected || !walletState.address) {
                showMessage(message, "Connect your wallet before starting a game.", true);
                return;
            }
            const session = await apiFetch("/api/game-sessions", {
                method: "POST",
                body: JSON.stringify({
                    player_address: walletState.address,
                    difficulty_level: difficulty.value
                })
            });
            activeSessionId = session.id;
            finalStateSubmitted = false;
            engine.initialize(difficulty.value);
            render();
            startLoop();
        }

        canvas.addEventListener("mousemove", (event) => {
            if (!running) return;
            const rect = canvas.getBoundingClientRect();
            const y = ((event.clientY - rect.top) / rect.height) * canvas.height;
            engine.updatePaddle(y - engine.paddleHeight / 2);
        });

        canvas.addEventListener("touchmove", (event) => {
            if (!running || !event.touches[0]) return;
            event.preventDefault();
            const rect = canvas.getBoundingClientRect();
            const y = ((event.touches[0].clientY - rect.top) / rect.height) * canvas.height;
            engine.updatePaddle(y - engine.paddleHeight / 2);
        }, { passive: false });

        startButton.addEventListener("click", () => startGame().catch((error) => showMessage(message, error.message, true)));
        pauseButton.addEventListener("click", () => {
            running = false;
            syncButtons(engine.state);
        });
        resumeButton.addEventListener("click", () => startLoop());
        resetButton.addEventListener("click", () => {
            engine.initialize(difficulty.value);
            activeSessionId = null;
            finalStateSubmitted = false;
            gameOverModal.classList.add("hidden");
            claimButton.disabled = false;
            claimButton.textContent = "Claim Reward";
            showMessage(message, "");
            render();
            syncButtons(engine.state);
        });
        claimButton.addEventListener("click", async () => {
            if (!activeSessionId) return;
            const result = await apiFetch(`/api/game-sessions/${activeSessionId}/claim`, { method: "POST" });
            if (result.success) {
                claimButton.disabled = true;
                claimButton.textContent = "Reward Claimed";
                showMessage(message, "Reward claim package created.");
            } else {
                showMessage(message, result.error_message || "Reward claim failed.", true);
            }
        });

        wallet.syncWallet().catch(() => {});
        render();
    }

    function initializeOnlinePage() {
        const lobbyScript = document.getElementById("initial-lobby-data");
        const headsUpList = document.getElementById("heads-up-list");
        const tournamentList = document.getElementById("tournament-list");
        const message = document.getElementById("online-message");
        const screenName = document.getElementById("screen-name");
        const saveAlias = document.getElementById("save-alias");
        const refreshLobby = document.getElementById("refresh-lobby");
        const createHeadsUp = document.getElementById("create-heads-up");
        const createTournament = document.getElementById("create-tournament");
        if (!headsUpList || !tournamentList || !screenName) return;

        let walletState = { ...defaultWalletState };
        let selectedTournamentId = null;
        let lobby = lobbyScript ? JSON.parse(lobbyScript.textContent) : { heads_up_matches: [], tournaments: [] };

        function currentDisplayName() {
            return (screenName.value || "").trim();
        }

        function ensureWallet() {
            if (!walletState.isConnected || !walletState.address) {
                throw new Error("Connect wallet first.");
            }
        }

        function canJoinHeadsUp(match) {
            return walletState.isConnected &&
                match.status === "WaitingForOpponent" &&
                match.host_wallet_address.toLowerCase() !== (walletState.address || "").toLowerCase();
        }

        function canReportHeadsUp(match) {
            const current = (walletState.address || "").toLowerCase();
            return walletState.isConnected &&
                match.status === "InProgress" &&
                [match.host_wallet_address, match.challenger_wallet_address].filter(Boolean).some((wallet) => wallet.toLowerCase() === current);
        }

        function canJoinTournament(tournament) {
            if (!walletState.isConnected || tournament.status !== "Registration") {
                return false;
            }
            const current = (walletState.address || "").toLowerCase();
            const alreadyJoined = tournament.entrants.some((entrant) => entrant.wallet_address.toLowerCase() === current);
            return !alreadyJoined && tournament.entrants.length < tournament.seat_limit;
        }

        function canReportTournamentMatch(tournament, match) {
            const current = (walletState.address || "").toLowerCase();
            return walletState.isConnected &&
                tournament.status === "InProgress" &&
                !match.is_complete &&
                [match.player_one_wallet_address, match.player_two_wallet_address].filter(Boolean).some((wallet) => wallet.toLowerCase() === current);
        }

        function opponentWallet(match) {
            const current = (walletState.address || "").toLowerCase();
            return match.host_wallet_address.toLowerCase() === current
                ? match.challenger_wallet_address
                : match.host_wallet_address;
        }

        function tournamentOpponentWallet(match) {
            const current = (walletState.address || "").toLowerCase();
            return match.player_one_wallet_address?.toLowerCase() === current
                ? match.player_two_wallet_address
                : match.player_one_wallet_address;
        }

        function renderLobby() {
            headsUpList.innerHTML = lobby.heads_up_matches.length === 0
                ? '<p class="text-muted">No active tables yet.</p>'
                : lobby.heads_up_matches.map((match) => `
                    <div class="arena-card">
                        <div>
                            <strong>${escapeHtml(match.host_name)}</strong>
                            <span>vs ${escapeHtml(match.challenger_name || "Waiting...")}</span>
                        </div>
                        <div class="chip-row">
                            <span class="chip">${escapeHtml(match.status)}</span>
                            ${match.winner_name ? `<span class="chip">Winner: ${escapeHtml(match.winner_name)}</span>` : ""}
                        </div>
                        <div class="action-row">
                            ${canJoinHeadsUp(match) ? `<button class="btn btn-sm btn-primary" data-action="join-heads-up" data-match-id="${escapeHtml(match.id)}">Join</button>` : ""}
                            ${canReportHeadsUp(match) ? `
                                <button class="btn btn-sm btn-outline-success" data-action="report-heads-up" data-match-id="${escapeHtml(match.id)}" data-winner="${escapeHtml(walletState.address || "")}">Report Me Winner</button>
                                <button class="btn btn-sm btn-outline-warning" data-action="report-heads-up" data-match-id="${escapeHtml(match.id)}" data-winner="${escapeHtml(opponentWallet(match) || "")}">Report Opponent Winner</button>
                            ` : ""}
                        </div>
                    </div>
                `).join("");

            tournamentList.innerHTML = lobby.tournaments.length === 0
                ? '<p class="text-muted">No tournaments yet.</p>'
                : lobby.tournaments.map((tournament) => `
                    <div class="arena-card">
                        <div>
                            <strong>${escapeHtml(tournament.name)}</strong>
                            <span class="chip">${tournament.entrants.length}/${tournament.seat_limit} players</span>
                            <span class="chip">${escapeHtml(tournament.status)}</span>
                            ${tournament.champion_name ? `<span class="chip">Champion: ${escapeHtml(tournament.champion_name)}</span>` : ""}
                        </div>
                        <div class="action-row">
                            ${canJoinTournament(tournament) ? `<button class="btn btn-sm btn-primary" data-action="join-tournament" data-tournament-id="${escapeHtml(tournament.id)}">Join</button>` : ""}
                            <button class="btn btn-sm btn-outline-primary" data-action="toggle-tournament" data-tournament-id="${escapeHtml(tournament.id)}">
                                ${selectedTournamentId === String(tournament.id) ? "Hide Bracket" : "View Bracket"}
                            </button>
                        </div>
                        ${selectedTournamentId === String(tournament.id) ? `
                            <div class="bracket-view">
                                ${tournament.rounds.length === 0
                                    ? '<p class="text-muted">Bracket starts once all 8 seats are filled.</p>'
                                    : tournament.rounds.map((round) => `
                                        <div class="round-block">
                                            <h5>Round ${round[0].round_number}</h5>
                                            ${round.map((match) => `
                                                <div class="bracket-match">
                                                    <span>${escapeHtml(match.player_one_name || "TBD")} vs ${escapeHtml(match.player_two_name || "TBD")}</span>
                                                    ${match.is_complete
                                                        ? `<span class="chip">Winner: ${escapeHtml(match.winner_name)}</span>`
                                                        : canReportTournamentMatch(tournament, match)
                                                            ? `
                                                                <div class="action-row mt-2">
                                                                    <button class="btn btn-sm btn-outline-success" data-action="report-tournament" data-tournament-id="${escapeHtml(tournament.id)}" data-match-id="${escapeHtml(match.id)}" data-winner="${escapeHtml(walletState.address || "")}">Report Me Winner</button>
                                                                    <button class="btn btn-sm btn-outline-warning" data-action="report-tournament" data-tournament-id="${escapeHtml(tournament.id)}" data-match-id="${escapeHtml(match.id)}" data-winner="${escapeHtml(tournamentOpponentWallet(match) || "")}">Report Opponent Winner</button>
                                                                </div>
                                                            `
                                                            : ""
                                                    }
                                                </div>
                                            `).join("")}
                                        </div>
                                    `).join("")
                                }
                            </div>
                        ` : ""}
                    </div>
                `).join("");
        }

        async function refresh() {
            lobby = await apiFetch("/api/online/lobby");
            renderLobby();
        }

        const wallet = createWalletController(async (state) => {
            walletState = state;
            if (walletState.address && !screenName.value) {
                screenName.value = shortWallet(walletState.address);
            }
            renderLobby();
        });

        async function runOnlineAction(action) {
            try {
                ensureWallet();
                await action();
                await refresh();
            } catch (error) {
                showMessage(message, error.message, true);
            }
        }

        saveAlias.addEventListener("click", () => runOnlineAction(async () => {
            const alias = await apiFetch("/api/online/players", {
                method: "POST",
                body: JSON.stringify({
                    wallet_address: walletState.address,
                    display_name: currentDisplayName()
                })
            });
            screenName.value = alias.display_name;
            showMessage(message, `Saved as ${alias.display_name}.`);
        }));

        refreshLobby.addEventListener("click", () => refresh().catch((error) => showMessage(message, error.message, true)));
        createHeadsUp.addEventListener("click", () => runOnlineAction(async () => {
            const match = await apiFetch("/api/online/heads-up", {
                method: "POST",
                body: JSON.stringify({
                    wallet_address: walletState.address,
                    display_name: currentDisplayName()
                })
            });
            showMessage(message, `Heads-up table ${match.id} is open.`);
        }));

        createTournament.addEventListener("click", () => runOnlineAction(async () => {
            const tournament = await apiFetch("/api/online/tournaments", {
                method: "POST",
                body: JSON.stringify({
                    wallet_address: walletState.address,
                    display_name: currentDisplayName()
                })
            });
            selectedTournamentId = String(tournament.id);
            showMessage(message, `Tournament ${tournament.name} created.`);
        }));

        document.getElementById("online-grid").addEventListener("click", async (event) => {
            const target = event.target.closest("button[data-action]");
            if (!target) return;

            const action = target.dataset.action;
            try {
                ensureWallet();
                if (action === "join-heads-up") {
                    await apiFetch(`/api/online/heads-up/${target.dataset.matchId}/join`, {
                        method: "POST",
                        body: JSON.stringify({
                            wallet_address: walletState.address,
                            display_name: currentDisplayName()
                        })
                    });
                    showMessage(message, `Joined table ${target.dataset.matchId}.`);
                } else if (action === "report-heads-up") {
                    await apiFetch(`/api/online/heads-up/${target.dataset.matchId}/report`, {
                        method: "POST",
                        body: JSON.stringify({
                            winner_wallet_address: target.dataset.winner,
                            reporter_wallet_address: walletState.address
                        })
                    });
                    showMessage(message, "Heads-up result submitted.");
                } else if (action === "join-tournament") {
                    const tournament = await apiFetch(`/api/online/tournaments/${target.dataset.tournamentId}/join`, {
                        method: "POST",
                        body: JSON.stringify({
                            wallet_address: walletState.address,
                            display_name: currentDisplayName()
                        })
                    });
                    selectedTournamentId = String(tournament.id);
                    showMessage(message, tournament.status === "InProgress" ? "Tournament is full and the bracket is live." : "Tournament seat locked.");
                } else if (action === "toggle-tournament") {
                    selectedTournamentId = selectedTournamentId === String(target.dataset.tournamentId) ? null : String(target.dataset.tournamentId);
                } else if (action === "report-tournament") {
                    const tournament = await apiFetch(`/api/online/tournaments/${target.dataset.tournamentId}/matches/${target.dataset.matchId}/report`, {
                        method: "POST",
                        body: JSON.stringify({
                            winner_wallet_address: target.dataset.winner,
                            reporter_wallet_address: walletState.address
                        })
                    });
                    selectedTournamentId = String(tournament.id);
                    showMessage(message, tournament.status === "Completed" ? `Tournament complete. Champion: ${tournament.champion_name}.` : "Tournament match result submitted.");
                }
                await refresh();
            } catch (error) {
                showMessage(message, error.message, true);
            }
        });

        wallet.syncWallet().catch(() => {});
        renderLobby();
    }

    document.addEventListener("DOMContentLoaded", () => {
        const page = document.body.dataset.page;
        if (page === "leaderboard") {
            initializeLeaderboardPage();
        } else if (page === "game") {
            initializeGamePage();
        } else if (page === "online") {
            initializeOnlinePage();
        }
    });
})();
