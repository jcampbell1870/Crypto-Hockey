// Game Renderer + Client-Side Gameplay Engine for Canvas
(function () {
    const canvasStates = new WeakMap();
    const canvasListeners = new WeakMap();

    const CANVAS_WIDTH = 800;
    const CANVAS_HEIGHT = 400;
    const PUCK_RADIUS = 5;
    const PADDLE_WIDTH = 10;
    const PADDLE_HEIGHT = 80;
    const WIN_SCORE = 5;
    const PLAYER_PADDLE_X = 10;
    const OPPONENT_PADDLE_X = CANVAS_WIDTH - 20;

    function getDifficultyConfig(difficulty) {
        switch (difficulty) {
            case 'Easy':
                return {
                    initialPuckSpeed: 240,
                    maxPuckSpeed: 700,
                    speedGrowth: 1.04,
                    aiReactionDelay: 0.5,
                    aiMoveSpeed: 180
                };
            case 'Hard':
                return {
                    initialPuckSpeed: 322,
                    maxPuckSpeed: 862.5,
                    speedGrowth: 1.1,
                    aiReactionDelay: 0.05,
                    aiMoveSpeed: 300
                };
            case 'Medium':
            default:
                return {
                    initialPuckSpeed: 280,
                    maxPuckSpeed: 750,
                    speedGrowth: 1.07,
                    aiReactionDelay: 0.2,
                    aiMoveSpeed: 255
                };
        }
    }

    function createInitialState(config) {
        return {
            puckX: CANVAS_WIDTH / 2,
            puckY: CANVAS_HEIGHT / 2,
            puckVelocityX: config.initialPuckSpeed,
            puckVelocityY: config.initialPuckSpeed * 0.5,
            playerPaddleY: CANVAS_HEIGHT / 2 - PADDLE_HEIGHT / 2,
            opponentPaddleY: CANVAS_HEIGHT / 2 - PADDLE_HEIGHT / 2,
            playerScore: 0,
            opponentScore: 0
        };
    }

    function clampPaddleY(y) {
        return Math.max(0, Math.min(y, CANVAS_HEIGHT - PADDLE_HEIGHT));
    }

    function render(canvasElement, gameState) {
        if (!canvasElement || !gameState) {
            return;
        }

        const canvas = canvasElement;
        if (!canvas || !canvas.getContext) {
            return;
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            return;
        }

        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        ctx.strokeStyle = '#0f3460';
        ctx.setLineDash([10, 10]);
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(canvas.width / 2, 0);
        ctx.lineTo(canvas.width / 2, canvas.height);
        ctx.stroke();
        ctx.setLineDash([]);

        ctx.fillStyle = '#e94560';
        ctx.fillRect(PLAYER_PADDLE_X, gameState.playerPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT);
        ctx.strokeStyle = '#ff6b6b';
        ctx.lineWidth = 2;
        ctx.strokeRect(PLAYER_PADDLE_X, gameState.playerPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT);

        ctx.fillStyle = '#4ecdc4';
        ctx.fillRect(OPPONENT_PADDLE_X, gameState.opponentPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT);
        ctx.strokeStyle = '#95e1d3';
        ctx.lineWidth = 2;
        ctx.strokeRect(OPPONENT_PADDLE_X, gameState.opponentPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT);

        ctx.fillStyle = '#ffd700';
        ctx.shadowColor = '#ffd700';
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.arc(gameState.puckX, gameState.puckY, PUCK_RADIUS, 0, Math.PI * 2);
        ctx.fill();
        ctx.shadowBlur = 0;

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 24px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(gameState.playerScore, canvas.width / 4, 40);
        ctx.fillText(gameState.opponentScore, (canvas.width * 3) / 4, 40);
    }

    function resetPuck(state, config) {
        state.puckX = CANVAS_WIDTH / 2;
        state.puckY = CANVAS_HEIGHT / 2;
        state.puckVelocityX = (Math.random() < 0.5 ? 1 : -1) * config.initialPuckSpeed;
        state.puckVelocityY = (Math.floor(Math.random() * 3) - 1) * config.initialPuckSpeed * 0.25;
    }

    function increasePuckSpeed(state, config) {
        const speed = Math.sqrt(state.puckVelocityX * state.puckVelocityX + state.puckVelocityY * state.puckVelocityY);
        if (speed >= config.maxPuckSpeed) {
            return;
        }

        const nextSpeed = Math.min(speed * config.speedGrowth, config.maxPuckSpeed);
        const angle = Math.atan2(state.puckVelocityY, state.puckVelocityX);
        state.puckVelocityX = Math.cos(angle) * nextSpeed;
        state.puckVelocityY = Math.sin(angle) * nextSpeed;
    }

    function updateAI(clientState, deltaTime) {
        clientState.aiReactionTime += deltaTime;
        if (clientState.aiReactionTime < clientState.config.aiReactionDelay) {
            return;
        }

        const state = clientState.state;
        const targetY = state.puckY - PADDLE_HEIGHT / 2;
        const moveDistance = clientState.config.aiMoveSpeed * clientState.config.aiReactionDelay;

        if (state.opponentPaddleY < targetY) {
            state.opponentPaddleY = clampPaddleY(state.opponentPaddleY + moveDistance);
        } else if (state.opponentPaddleY > targetY) {
            state.opponentPaddleY = clampPaddleY(state.opponentPaddleY - moveDistance);
        }

        clientState.aiReactionTime = 0;
    }

    function checkPaddleCollision(state, config) {
        if (
            state.puckX - PUCK_RADIUS <= PLAYER_PADDLE_X + PADDLE_WIDTH &&
            state.puckY >= state.playerPaddleY &&
            state.puckY <= state.playerPaddleY + PADDLE_HEIGHT &&
            state.puckVelocityX < 0
        ) {
            state.puckVelocityX = -state.puckVelocityX;
            state.puckVelocityY += (state.puckY - (state.playerPaddleY + PADDLE_HEIGHT / 2)) * 0.1;
            state.puckX = PLAYER_PADDLE_X + PADDLE_WIDTH + PUCK_RADIUS;
            increasePuckSpeed(state, config);
        }

        if (
            state.puckX + PUCK_RADIUS >= OPPONENT_PADDLE_X &&
            state.puckY >= state.opponentPaddleY &&
            state.puckY <= state.opponentPaddleY + PADDLE_HEIGHT &&
            state.puckVelocityX > 0
        ) {
            state.puckVelocityX = -state.puckVelocityX;
            state.puckVelocityY += (state.puckY - (state.opponentPaddleY + PADDLE_HEIGHT / 2)) * 0.1;
            state.puckX = OPPONENT_PADDLE_X - PUCK_RADIUS;
            increasePuckSpeed(state, config);
        }
    }

    function updateGame(clientState, deltaTime) {
        const state = clientState.state;

        state.playerPaddleY = clampPaddleY(clientState.playerInputY);

        state.puckX += state.puckVelocityX * deltaTime;
        state.puckY += state.puckVelocityY * deltaTime;

        if (state.puckY - PUCK_RADIUS <= 0 || state.puckY + PUCK_RADIUS >= CANVAS_HEIGHT) {
            state.puckVelocityY = -state.puckVelocityY;
            state.puckY = Math.max(PUCK_RADIUS, Math.min(state.puckY, CANVAS_HEIGHT - PUCK_RADIUS));
        }

        checkPaddleCollision(state, clientState.config);

        if (state.puckX - PUCK_RADIUS <= 0) {
            state.opponentScore += 1;
            resetPuck(state, clientState.config);
        } else if (state.puckX + PUCK_RADIUS >= CANVAS_WIDTH) {
            state.playerScore += 1;
            resetPuck(state, clientState.config);
        }

        updateAI(clientState, deltaTime);

        if (state.playerScore >= WIN_SCORE || state.opponentScore >= WIN_SCORE) {
            clientState.running = false;
            return true;
        }

        return false;
    }

    function notifyScoreIfChanged(clientState) {
        const state = clientState.state;
        if (
            state.playerScore === clientState.lastReportedPlayerScore &&
            state.opponentScore === clientState.lastReportedOpponentScore
        ) {
            return;
        }

        clientState.lastReportedPlayerScore = state.playerScore;
        clientState.lastReportedOpponentScore = state.opponentScore;

        if (clientState.dotNetRef) {
            clientState.dotNetRef.invokeMethodAsync(
                'OnGameScoreChanged',
                state.playerScore,
                state.opponentScore,
                clientState.gameToken
            );
        }
    }

    function gameLoop(canvas, clientState, timestamp) {
        if (!clientState.running) {
            return;
        }

        if (!clientState.lastFrameTime) {
            clientState.lastFrameTime = timestamp;
        }

        const elapsedSeconds = Math.min((timestamp - clientState.lastFrameTime) / 1000, 0.05);
        clientState.lastFrameTime = timestamp;

        const gameOver = updateGame(clientState, elapsedSeconds);
        render(canvas, clientState.state);
        notifyScoreIfChanged(clientState);

        if (gameOver) {
            if (clientState.dotNetRef) {
                clientState.dotNetRef.invokeMethodAsync(
                    'OnGameCompleted',
                    clientState.state.playerScore,
                    clientState.state.opponentScore,
                    clientState.gameToken
                );
            }
            return;
        }

        clientState.animationFrameId = window.requestAnimationFrame((nextTimestamp) =>
            gameLoop(canvas, clientState, nextTimestamp)
        );
    }

    function bindInputHandlers(canvas, clientState) {
        const existing = canvasListeners.get(canvas);
        if (existing) {
            return existing;
        }

        const movePaddleFromPointer = (clientY) => {
            const rect = canvas.getBoundingClientRect();
            if (!rect.height) {
                return;
            }

            const normalizedY = ((clientY - rect.top) / rect.height) * canvas.height;
            clientState.playerInputY = normalizedY - PADDLE_HEIGHT / 2;
        };

        const mouseMoveHandler = (event) => movePaddleFromPointer(event.clientY);
        const touchMoveHandler = (event) => {
            if (!event.touches || event.touches.length === 0) {
                return;
            }

            movePaddleFromPointer(event.touches[0].clientY);
            event.preventDefault();
        };

        canvas.addEventListener('mousemove', mouseMoveHandler);
        canvas.addEventListener('touchmove', touchMoveHandler, { passive: false });

        const listeners = { mouseMoveHandler, touchMoveHandler };
        canvasListeners.set(canvas, listeners);
        return listeners;
    }

    function unbindInputHandlers(canvas) {
        const listeners = canvasListeners.get(canvas);
        if (!listeners) {
            return;
        }

        canvas.removeEventListener('mousemove', listeners.mouseMoveHandler);
        canvas.removeEventListener('touchmove', listeners.touchMoveHandler);
        canvasListeners.delete(canvas);
    }

    function stopLoop(clientState) {
        clientState.running = false;
        if (clientState.animationFrameId) {
            window.cancelAnimationFrame(clientState.animationFrameId);
            clientState.animationFrameId = 0;
        }
    }

    window.gameRenderer = {
        render,
        startGame: function (canvasElement, difficulty, dotNetRef, gameToken) {
            if (!canvasElement) {
                return;
            }

            this.pauseGame(canvasElement);
            unbindInputHandlers(canvasElement);

            const config = getDifficultyConfig(difficulty);
            const state = createInitialState(config);
            const clientState = {
                config,
                state,
                dotNetRef: dotNetRef || null,
                running: true,
                animationFrameId: 0,
                lastFrameTime: 0,
                playerInputY: state.playerPaddleY,
                aiReactionTime: 0,
                lastReportedPlayerScore: -1,
                lastReportedOpponentScore: -1,
                gameToken: Number.isFinite(gameToken) ? gameToken : 0
            };

            canvasStates.set(canvasElement, clientState);
            bindInputHandlers(canvasElement, clientState);

            render(canvasElement, state);
            notifyScoreIfChanged(clientState);
            clientState.animationFrameId = window.requestAnimationFrame((timestamp) =>
                gameLoop(canvasElement, clientState, timestamp)
            );
        },
        pauseGame: function (canvasElement) {
            if (!canvasElement) {
                return;
            }

            const clientState = canvasStates.get(canvasElement);
            if (!clientState) {
                return;
            }

            stopLoop(clientState);
        },
        resetGame: function (canvasElement) {
            if (!canvasElement) {
                return;
            }

            const clientState = canvasStates.get(canvasElement);
            if (clientState) {
                stopLoop(clientState);
                canvasStates.delete(canvasElement);
            }

            const config = getDifficultyConfig('Medium');
            render(canvasElement, createInitialState(config));
        },
        disposeGame: function (canvasElement) {
            if (!canvasElement) {
                return;
            }

            const clientState = canvasStates.get(canvasElement);
            if (clientState) {
                stopLoop(clientState);
                canvasStates.delete(canvasElement);
            }

            unbindInputHandlers(canvasElement);
        }
    };
})();
