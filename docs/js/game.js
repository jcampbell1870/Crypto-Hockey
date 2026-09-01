// Crypto Hockey - simple canvas air hockey game engine with an AI opponent.

const Game = (() => {
  const WIN_SCORE = 5;

  let canvas, ctx;
  let width, height;
  let animationId = null;
  let running = false;

  const puck = { x: 0, y: 0, vx: 0, vy: 0, radius: 12 };
  const player = { x: 0, y: 0, radius: 22, targetX: 0, targetY: 0 };
  const ai = { x: 0, y: 0, radius: 22, difficulty: "medium" };
  const score = { player: 0, ai: 0 };

  const DIFFICULTY_SPEED = { easy: 3, medium: 5, hard: 7.5 };

  let onGameOver = null;

  function init(canvasEl, { difficulty = "medium", onGameOverCallback } = {}) {
    canvas = canvasEl;
    ctx = canvas.getContext("2d");
    width = canvas.width;
    height = canvas.height;
    ai.difficulty = difficulty;
    onGameOver = onGameOverCallback;

    resetPositions();
    score.player = 0;
    score.ai = 0;

    // Guard against duplicate listeners if init() is called again without stop().
    canvas.removeEventListener("mousemove", handlePointerMove);
    canvas.removeEventListener("touchmove", handleTouchMove);
    canvas.addEventListener("mousemove", handlePointerMove);
    canvas.addEventListener("touchmove", handleTouchMove, { passive: false });
  }

  function resetPositions(scoredBy) {
    puck.x = width / 2;
    puck.y = height / 2;
    const dir = scoredBy === "player" ? -1 : 1;
    puck.vx = 0;
    puck.vy = 4 * (Math.random() > 0.5 ? 1 : -1) * dir || 4;

    player.x = width / 2;
    player.y = height - 60;
    player.targetX = player.x;
    player.targetY = player.y;

    ai.x = width / 2;
    ai.y = 60;
  }

  function handlePointerMove(e) {
    const rect = canvas.getBoundingClientRect();
    setPlayerTarget(e.clientX - rect.left, e.clientY - rect.top);
  }

  function handleTouchMove(e) {
    e.preventDefault();
    const rect = canvas.getBoundingClientRect();
    const touch = e.touches[0];
    setPlayerTarget(touch.clientX - rect.left, touch.clientY - rect.top);
  }

  function setPlayerTarget(x, y) {
    player.targetX = Math.min(Math.max(x, player.radius), width - player.radius);
    player.targetY = Math.min(
      Math.max(y, height / 2 + player.radius),
      height - player.radius
    );
  }

  function start() {
    running = true;
    loop();
  }

  function stop() {
    running = false;
    if (animationId) cancelAnimationFrame(animationId);
    canvas.removeEventListener("mousemove", handlePointerMove);
    canvas.removeEventListener("touchmove", handleTouchMove);
  }

  function loop() {
    if (!running) return;
    update();
    draw();
    animationId = requestAnimationFrame(loop);
  }

  function update() {
    // Ease player paddle toward pointer target for smoother movement.
    player.x += (player.targetX - player.x) * 0.35;
    player.y += (player.targetY - player.y) * 0.35;

    updateAI();

    puck.x += puck.vx;
    puck.y += puck.vy;

    // Wall bounce (left/right)
    if (puck.x - puck.radius < 0 || puck.x + puck.radius > width) {
      puck.vx *= -1;
      puck.x = Math.min(Math.max(puck.x, puck.radius), width - puck.radius);
    }

    // Goals (top/bottom): only score if puck is within the goal mouth.
    const goalHalfWidth = 70;
    if (puck.y - puck.radius < 0) {
      if (Math.abs(puck.x - width / 2) < goalHalfWidth) {
        score.player++;
        checkGameOver();
        resetPositions("player");
      } else {
        puck.vy *= -1;
        puck.y = puck.radius;
      }
    } else if (puck.y + puck.radius > height) {
      if (Math.abs(puck.x - width / 2) < goalHalfWidth) {
        score.ai++;
        checkGameOver();
        resetPositions("ai");
      } else {
        puck.vy *= -1;
        puck.y = height - puck.radius;
      }
    }

    resolvePaddleCollision(player);
    resolvePaddleCollision(ai);

    // Friction / speed cap
    puck.vx *= 0.995;
    puck.vy *= 0.995;
    const speed = Math.hypot(puck.vx, puck.vy);
    const maxSpeed = 14;
    if (speed > maxSpeed) {
      puck.vx = (puck.vx / speed) * maxSpeed;
      puck.vy = (puck.vy / speed) * maxSpeed;
    }
  }

  function updateAI() {
    const speed = DIFFICULTY_SPEED[ai.difficulty] || DIFFICULTY_SPEED.medium;
    const targetX = puck.y < height / 2 ? puck.x : width / 2;
    const targetY = puck.y < height / 2 ? Math.min(puck.y, height / 2 - ai.radius) : height / 4;

    const dx = targetX - ai.x;
    const dy = targetY - ai.y;
    const dist = Math.hypot(dx, dy) || 1;
    ai.x += (dx / dist) * Math.min(speed, dist);
    ai.y += (dy / dist) * Math.min(speed, dist);

    ai.x = Math.min(Math.max(ai.x, ai.radius), width - ai.radius);
    ai.y = Math.min(Math.max(ai.y, ai.radius), height / 2 - ai.radius);
  }

  function resolvePaddleCollision(paddle) {
    const dx = puck.x - paddle.x;
    const dy = puck.y - paddle.y;
    const dist = Math.hypot(dx, dy);
    const minDist = puck.radius + paddle.radius;

    if (dist < minDist && dist > 0) {
      const nx = dx / dist;
      const ny = dy / dist;
      puck.x = paddle.x + nx * minDist;
      puck.y = paddle.y + ny * minDist;

      const speed = Math.hypot(puck.vx, puck.vy) || 6;
      const impact = Math.max(speed, 6);
      puck.vx = nx * impact;
      puck.vy = ny * impact;
    }
  }

  function checkGameOver() {
    if (score.player >= WIN_SCORE || score.ai >= WIN_SCORE) {
      const winner = score.player >= WIN_SCORE ? "player" : "ai";
      stop();
      if (typeof onGameOver === "function") {
        onGameOver({ winner, score: { ...score } });
      }
    }
  }

  function draw() {
    ctx.clearRect(0, 0, width, height);

    // Rink
    ctx.fillStyle = "#0b1e33";
    ctx.fillRect(0, 0, width, height);
    ctx.strokeStyle = "rgba(0, 229, 255, 0.5)";
    ctx.lineWidth = 3;
    ctx.strokeRect(4, 4, width - 8, height - 8);
    ctx.beginPath();
    ctx.moveTo(0, height / 2);
    ctx.lineTo(width, height / 2);
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(width / 2, height / 2, 50, 0, Math.PI * 2);
    ctx.stroke();

    // Goals
    ctx.strokeStyle = "#ff4d6d";
    ctx.beginPath();
    ctx.moveTo(width / 2 - 70, 2);
    ctx.lineTo(width / 2 + 70, 2);
    ctx.moveTo(width / 2 - 70, height - 2);
    ctx.lineTo(width / 2 + 70, height - 2);
    ctx.stroke();

    drawCircle(ai.x, ai.y, ai.radius, "#ff4d6d");
    drawCircle(player.x, player.y, player.radius, "#00e5ff");
    drawCircle(puck.x, puck.y, puck.radius, "#ffffff");
  }

  function drawCircle(x, y, radius, color) {
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.closePath();
  }

  function getScore() {
    return { ...score };
  }

  return { init, start, stop, getScore, WIN_SCORE };
})();
