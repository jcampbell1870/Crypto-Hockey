namespace Crypto_Hockey.Services;

public interface IGameEngine
{
    void InitializeGame(string difficultyLevel);
    void UpdatePaddlePosition(bool isPlayer, float yPosition);
    void UpdateGame(float deltaTime);
    GameState GetGameState();
    void Reset();
}

public class GameState
{
    public float PuckX { get; set; }
    public float PuckY { get; set; }
    public float PuckVelocityX { get; set; }
    public float PuckVelocityY { get; set; }
    public float PlayerPaddleY { get; set; }
    public float OpponentPaddleY { get; set; }
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public bool GameOver { get; set; }
    public string? Winner { get; set; }
}

public class GameEngine : IGameEngine
{
    private const float CanvasWidth = 800f;
    private const float CanvasHeight = 400f;
    private const float PuckRadius = 5f;
    private const float PaddleWidth = 10f;
    private const float PaddleHeight = 80f;
    private const float PaddleSpeed = 300f;
    private const float InitialPuckSpeed = 200f;
    private const float MaxPuckSpeed = 500f;

    private GameState _state = new();
    private string _difficultyLevel = "Medium";
    private float _aiReactionTime = 0f;

    public void InitializeGame(string difficultyLevel)
    {
        _difficultyLevel = difficultyLevel;
        _state = new GameState
        {
            PuckX = CanvasWidth / 2,
            PuckY = CanvasHeight / 2,
            PuckVelocityX = InitialPuckSpeed,
            PuckVelocityY = InitialPuckSpeed * 0.5f,
            PlayerPaddleY = CanvasHeight / 2 - PaddleHeight / 2,
            OpponentPaddleY = CanvasHeight / 2 - PaddleHeight / 2,
            PlayerScore = 0,
            OpponentScore = 0,
            GameOver = false
        };
    }

    public void UpdatePaddlePosition(bool isPlayer, float yPosition)
    {
        float constrainedY = Math.Max(0, Math.Min(yPosition, CanvasHeight - PaddleHeight));

        if (isPlayer)
            _state.PlayerPaddleY = constrainedY;
        else
            _state.OpponentPaddleY = constrainedY;
    }

    public void UpdateGame(float deltaTime)
    {
        if (_state.GameOver)
            return;

        // Update puck position
        _state.PuckX += _state.PuckVelocityX * deltaTime;
        _state.PuckY += _state.PuckVelocityY * deltaTime;

        // Boundary collisions (top/bottom)
        if (_state.PuckY - PuckRadius <= 0 || _state.PuckY + PuckRadius >= CanvasHeight)
        {
            _state.PuckVelocityY = -_state.PuckVelocityY;
            _state.PuckY = Math.Max(PuckRadius, Math.Min(_state.PuckY, CanvasHeight - PuckRadius));
        }

        // Paddle collisions
        CheckPaddleCollision();

        // Scoring
        if (_state.PuckX - PuckRadius <= 0)
        {
            _state.OpponentScore++;
            ResetPuck();
        }
        else if (_state.PuckX + PuckRadius >= CanvasWidth)
        {
            _state.PlayerScore++;
            ResetPuck();
        }

        // AI movement
        UpdateAI(deltaTime);

        // Check win condition (first to 5)
        if (_state.PlayerScore >= 5)
        {
            _state.GameOver = true;
            _state.Winner = "Player";
        }
        else if (_state.OpponentScore >= 5)
        {
            _state.GameOver = true;
            _state.Winner = "Opponent";
        }
    }

    public GameState GetGameState()
    {
        return _state;
    }

    public void Reset()
    {
        InitializeGame(_difficultyLevel);
    }

    private void CheckPaddleCollision()
    {
        // Player paddle (left side)
        if (_state.PuckX - PuckRadius <= PaddleWidth &&
            _state.PuckY >= _state.PlayerPaddleY &&
            _state.PuckY <= _state.PlayerPaddleY + PaddleHeight &&
            _state.PuckVelocityX < 0)
        {
            _state.PuckVelocityX = -_state.PuckVelocityX;
            _state.PuckVelocityY += (_state.PuckY - (_state.PlayerPaddleY + PaddleHeight / 2)) * 0.1f;
            _state.PuckX = PaddleWidth + PuckRadius;
            IncreasePuckSpeed();
        }

        // Opponent paddle (right side)
        if (_state.PuckX + PuckRadius >= CanvasWidth - PaddleWidth &&
            _state.PuckY >= _state.OpponentPaddleY &&
            _state.PuckY <= _state.OpponentPaddleY + PaddleHeight &&
            _state.PuckVelocityX > 0)
        {
            _state.PuckVelocityX = -_state.PuckVelocityX;
            _state.PuckVelocityY += (_state.PuckY - (_state.OpponentPaddleY + PaddleHeight / 2)) * 0.1f;
            _state.PuckX = CanvasWidth - PaddleWidth - PuckRadius;
            IncreasePuckSpeed();
        }
    }

    private void UpdateAI(float deltaTime)
    {
        _aiReactionTime += deltaTime;

        float reactionDelay = _difficultyLevel switch
        {
            "Easy" => 0.5f,
            "Medium" => 0.2f,
            "Hard" => 0.05f,
            _ => 0.2f
        };

        if (_aiReactionTime >= reactionDelay)
        {
            float targetY = _state.PuckY - PaddleHeight / 2;
            float moveSpeed = _difficultyLevel switch
            {
                "Easy" => PaddleSpeed * 0.6f,
                "Medium" => PaddleSpeed * 0.85f,
                "Hard" => PaddleSpeed,
                _ => PaddleSpeed * 0.85f
            };

            if (_state.OpponentPaddleY < targetY)
            {
                _state.OpponentPaddleY += moveSpeed * reactionDelay;
            }
            else if (_state.OpponentPaddleY > targetY)
            {
                _state.OpponentPaddleY -= moveSpeed * reactionDelay;
            }

            _state.OpponentPaddleY = Math.Max(0, Math.Min(_state.OpponentPaddleY, CanvasHeight - PaddleHeight));
            _aiReactionTime = 0;
        }
    }

    private void IncreasePuckSpeed()
    {
        float speed = (float)Math.Sqrt(_state.PuckVelocityX * _state.PuckVelocityX + _state.PuckVelocityY * _state.PuckVelocityY);
        if (speed < MaxPuckSpeed)
        {
            speed = Math.Min(speed * 1.05f, MaxPuckSpeed);
            float angle = (float)Math.Atan2(_state.PuckVelocityY, _state.PuckVelocityX);
            _state.PuckVelocityX = (float)Math.Cos(angle) * speed;
            _state.PuckVelocityY = (float)Math.Sin(angle) * speed;
        }
    }

    private void ResetPuck()
    {
        _state.PuckX = CanvasWidth / 2;
        _state.PuckY = CanvasHeight / 2;
        _state.PuckVelocityX = (Random.Shared.Next(0, 2) == 0 ? 1 : -1) * InitialPuckSpeed;
        _state.PuckVelocityY = (Random.Shared.Next(-1, 2) * InitialPuckSpeed * 0.25f);
    }
}
