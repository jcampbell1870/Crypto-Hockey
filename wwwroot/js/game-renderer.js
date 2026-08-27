// Game Renderer for Canvas
window.gameRenderer = {
    render: function (canvasElement, gameState) {
        if (!canvasElement || !gameState) {
            return;
        }

        const canvas = canvasElement;
        if (!canvas || !canvas.getContext) {
            return;
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        // Clear canvas
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Draw center line
        ctx.strokeStyle = '#0f3460';
        ctx.setLineDash([10, 10]);
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(canvas.width / 2, 0);
        ctx.lineTo(canvas.width / 2, canvas.height);
        ctx.stroke();
        ctx.setLineDash([]);

        // Draw player paddle (left)
        ctx.fillStyle = '#e94560';
        ctx.fillRect(10, gameState.playerPaddleY, 10, 80);
        ctx.strokeStyle = '#ff6b6b';
        ctx.lineWidth = 2;
        ctx.strokeRect(10, gameState.playerPaddleY, 10, 80);

        // Draw opponent paddle (right)
        ctx.fillStyle = '#4ecdc4';
        ctx.fillRect(canvas.width - 20, gameState.opponentPaddleY, 10, 80);
        ctx.strokeStyle = '#95e1d3';
        ctx.lineWidth = 2;
        ctx.strokeRect(canvas.width - 20, gameState.opponentPaddleY, 10, 80);

        // Draw puck (glowing effect)
        ctx.fillStyle = '#ffd700';
        ctx.shadowColor = '#ffd700';
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.arc(gameState.puckX, gameState.puckY, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.shadowBlur = 0;

        // Draw score text
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 24px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(gameState.playerScore, canvas.width / 4, 40);
        ctx.fillText(gameState.opponentScore, (canvas.width * 3) / 4, 40);

        // Draw game status
        if (gameState.gameOver) {
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.fillStyle = gameState.winner === 'Player' ? '#4ecdc4' : '#e94560';
            ctx.font = 'bold 48px Arial';
            ctx.textAlign = 'center';
            ctx.fillText(gameState.winner === 'Player' ? 'YOU WIN!' : 'GAME OVER', canvas.width / 2, canvas.height / 2 - 20);

            ctx.fillStyle = '#ffffff';
            ctx.font = 'bold 24px Arial';
            ctx.fillText(`Final Score: ${gameState.playerScore} - ${gameState.opponentScore}`, canvas.width / 2, canvas.height / 2 + 30);
        }
    }
};
