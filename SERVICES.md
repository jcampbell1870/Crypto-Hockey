# Crypto Hockey - Services & API Documentation

## Overview

This document describes all services, their methods, and how they interact with each other.

## Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│            Blazor Components (UI)               │
│  (Game.razor, WalletConnection.razor, etc.)    │
└────────┬────────────────────────────────────────┘
		 │
		 ├─────────────────┬──────────────────┐
		 │                 │                  │
		 ▼                 ▼                  ▼
	┌─────────┐       ┌──────────────┐  ┌──────────┐
	│WalletSvc│       │GameService   │  │GameEngine│
	└────┬────┘       └──────┬───────┘  └──────────┘
		 │                   │
		 ▼                   ▼
	┌─────────────┐    ┌──────────────┐
	│BlockchainSvc│    │GameDbContext │
	└─────────────┘    └──────┬───────┘
		 │                    │
		 ▼                    ▼
	┌──────────────┐    ┌────────────┐
	│MetaMask JS   │    │SQL Database│
	│  Interop     │    │  (LocalDB) │
	└──────────────┘    └────────────┘
```

## Services

### 1. IWalletService / WalletService

**Purpose**: Manages MetaMask wallet connection and blockchain interactions through JavaScript interop.

**Location**: `Services/WalletService.cs`

**Dependencies**: 
- `IJSRuntime` (Blazor JS Interop)
- `window.metamaskInterop` (JavaScript)

**Methods**

#### ConnectWalletAsync()
Initiates MetaMask connection request.

```csharp
public async Task<WalletConnectionState> ConnectWalletAsync()
```

**Returns**: `WalletConnectionState` with connection details
```csharp
{
	IsConnected: true,
	Address: "0x1234...5678",
	ChainId: 1,
	ChainName: "Ethereum Mainnet",
	Balance: 2.5
}
```

**Throws**: 
- User rejects connection
- MetaMask not installed

#### GetWalletStateAsync()
Gets current wallet connection state without prompting.

```csharp
public async Task<WalletConnectionState> GetWalletStateAsync()
```

**Returns**: Current `WalletConnectionState`

#### DisconnectWalletAsync()
Disconnects the application from the wallet.

```csharp
public async Task DisconnectWalletAsync()
```

**Note**: This is application-level disconnect. Wallet itself requires manual disconnect in MetaMask UI.

#### SwitchNetworkAsync(int chainId)
Requests MetaMask to switch to a different blockchain network.

```csharp
public async Task<bool> SwitchNetworkAsync(int chainId)
```

**Parameters**:
- `chainId`: Network chain ID (1, 11155111, 137, etc.)

**Returns**: `true` if successful, `false` otherwise

**Supported Networks**:
- 1: Ethereum Mainnet
- 11155111: Sepolia Testnet
- 137: Polygon Mainnet

---

### 2. IBlockchainService / BlockchainService

**Purpose**: Handles blockchain interactions including token transfers and balance queries.

**Location**: `Services/BlockchainService.cs`

**Dependencies**:
- `Nethereum.Web3`
- `BlockchainConfig` (configuration)
- `ILogger`

**Methods**

#### SendRewardAsync(string walletAddress, decimal amount, int chainId)
Sends ERC-20 token rewards to a player's wallet.

```csharp
public async Task<bool> SendRewardAsync(
	string walletAddress, 
	decimal amount, 
	int chainId)
```

**Parameters**:
- `walletAddress`: Recipient wallet address (0x...)
- `amount`: Number of tokens to send
- `chainId`: Target blockchain network

**Returns**: `true` if transaction successful

**Requirements**:
- Valid Ethereum address format
- Backend wallet funded with gas (for mainnet/testnet)
- RPC endpoint accessible

**Flow**:
1. Validate wallet address format
2. Get RPC URL for chain
3. Initialize Web3 instance
4. Build token transfer transaction
5. Sign and send transaction
6. Return success status

#### GetTokenBalanceAsync(string walletAddress, int chainId)
Queries Arcade1870 token balance for a wallet.

```csharp
public async Task<decimal> GetTokenBalanceAsync(
	string walletAddress, 
	int chainId)
```

**Parameters**:
- `walletAddress`: Wallet to query
- `chainId`: Network to query

**Returns**: Token balance as decimal

**Contract Details**:
- Contract: `0x8eddD4edea39c5B5f77662453600F53A202EE47C`
- Token Standard: ERC-20
- Decimals: 18

#### ValidateWalletAsync(string walletAddress)
Validates wallet address format.

```csharp
public async Task<bool> ValidateWalletAsync(string walletAddress)
```

**Returns**: `true` if valid Ethereum address

**Validation Rules**:
- Must start with "0x"
- Must be 42 characters total
- Alphanumeric format

---

### 3. IGameService / GameService

**Purpose**: Manages game sessions, player profiles, and game logic.

**Location**: `Services/GameService.cs`

**Dependencies**:
- `GameDbContext` (Entity Framework)
- `IBlockchainService`
- `ILogger`

**Methods**

#### CreateGameSessionAsync(string playerAddress, string difficultyLevel)
Creates a new game session record.

```csharp
public async Task<GameSession> CreateGameSessionAsync(
	string playerAddress, 
	string difficultyLevel)
```

**Parameters**:
- `playerAddress`: Player's wallet address
- `difficultyLevel`: "Easy", "Medium", or "Hard"

**Returns**: New `GameSession` entity

**Creates**:
```csharp
{
	Id: 1,
	PlayerAddress: "0x...",
	DifficultyLevel: "Medium",
	PlayerScore: 0,
	OpponentScore: 0,
	StartedAt: DateTime.UtcNow,
	RewardClaimed: false
}
```

#### EndGameSessionAsync(int sessionId, int playerScore, int opponentScore)
Completes a game session and updates player stats.

```csharp
public async Task<GameSession> EndGameSessionAsync(
	int sessionId, 
	int playerScore, 
	int opponentScore)
```

**Parameters**:
- `sessionId`: Game session ID to end
- `playerScore`: Final player score
- `opponentScore`: Final AI opponent score

**Returns**: Updated `GameSession`

**Side Effects**:
- Updates session end time and scores
- Sets `PlayerWon` flag
- Sets reward amount if player won (10 A1870)
- Creates/updates `PlayerProfile`
- Updates player stats (wins, losses, total games)

#### GetOrCreatePlayerAsync(string walletAddress)
Gets existing player or creates new profile.

```csharp
public async Task<PlayerProfile> GetOrCreatePlayerAsync(
	string walletAddress)
```

**Parameters**:
- `walletAddress`: Player's wallet address

**Returns**: `PlayerProfile` entity

**Player Profile Structure**:
```csharp
{
	Id: 1,
	WalletAddress: "0x...",
	TotalGames: 10,
	TotalWins: 7,
	TotalLosses: 3,
	TotalRewardsEarned: 70.0m,
	CreatedAt: DateTime.UtcNow,
	LastPlayedAt: DateTime.UtcNow,
	WinRate: 70.0 // calculated property
}
```

#### GetPlayerGameHistoryAsync(string walletAddress, int limit = 10)
Retrieves player's recent game sessions.

```csharp
public async Task<List<GameSession>> GetPlayerGameHistoryAsync(
	string walletAddress, 
	int limit = 10)
```

**Parameters**:
- `walletAddress`: Player's wallet
- `limit`: Maximum number of records to return (default: 10)

**Returns**: List of `GameSession` objects

**Ordering**: Most recent games first

#### GetLeaderboardAsync(int limit = 10)
Gets top players by wins and rewards.

```csharp
public async Task<List<PlayerProfile>> GetLeaderboardAsync(
	int limit = 10)
```

**Parameters**:
- `limit`: Number of top players to return (default: 10)

**Returns**: List of top `PlayerProfile` objects

**Sorting**:
1. Primary: TotalWins (descending)
2. Secondary: TotalRewardsEarned (descending)

#### ClaimRewardAsync(int sessionId)
Claims ERC-20 token reward for a won game.

```csharp
public async Task<bool> ClaimRewardAsync(int sessionId)
```

**Parameters**:
- `sessionId`: Completed game session ID

**Returns**: `true` if reward claimed successfully

**Requirements**:
- Game session exists
- Player won the game
- Reward not already claimed
- BlockchainService.SendRewardAsync succeeds

**Side Effects**:
- Sets `RewardClaimed = true` on session
- Records transaction hash (if available)
- Logs reward claim event

---

### 4. IGameEngine / GameEngine

**Purpose**: Implements game physics, AI logic, and game state management.

**Location**: `Services/GameEngine.cs`

**Constants**:
```csharp
const float CanvasWidth = 800f;
const float CanvasHeight = 400f;
const float PuckRadius = 5f;
const float PaddleWidth = 10f;
const float PaddleHeight = 80f;
const float PaddleSpeed = 300f;
const float InitialPuckSpeed = 200f;
const float MaxPuckSpeed = 500f;
```

**Methods**

#### InitializeGame(string difficultyLevel)
Initializes a new game with specified difficulty.

```csharp
public void InitializeGame(string difficultyLevel)
```

**Parameters**:
- `difficultyLevel`: "Easy", "Medium", or "Hard"

**Initializes**:
- Puck at center (400, 200)
- Both paddles at center
- Scores reset to 0
- Game state set to running

#### UpdatePaddlePosition(bool isPlayer, float yPosition)
Updates player or AI paddle position.

```csharp
public void UpdatePaddlePosition(bool isPlayer, float yPosition)
```

**Parameters**:
- `isPlayer`: true for player paddle, false for AI
- `yPosition`: Y coordinate (constrained 0-320)

**Constraints**:
- Y must be between 0 and (CanvasHeight - PaddleHeight)
- Automatically clamped if out of range

#### UpdateGame(float deltaTime)
Main game update loop - called every frame.

```csharp
public void UpdateGame(float deltaTime)
```

**Parameters**:
- `deltaTime`: Time since last update in seconds

**Updates**:
1. Puck position based on velocity
2. Boundary collisions (top/bottom)
3. Paddle collisions
4. Scoring (left/right boundaries)
5. AI paddle movement
6. Win condition (first to 5)

**Physics Calculations**:
```csharp
// New position = old position + velocity * time
puckX += puckVelocityX * deltaTime;
puckY += puckVelocityY * deltaTime;

// Paddle collision reverses X velocity, adds spin from Y
puckVelocityX = -puckVelocityX;
puckVelocityY += (hitPoint - paddleCenter) * spinFactor;
```

#### GetGameState()
Returns current game state.

```csharp
public GameState GetGameState()
```

**Returns**: `GameState` object with all current values

**GameState Structure**:
```csharp
{
	PuckX: 400,
	PuckY: 200,
	PuckVelocityX: 200,
	PuckVelocityY: 50,
	PlayerPaddleY: 160,
	OpponentPaddleY: 150,
	PlayerScore: 2,
	OpponentScore: 1,
	GameOver: false,
	Winner: null
}
```

#### Reset()
Resets game to initial state.

```csharp
public void Reset()
```

---

## Data Models

### GameState
```csharp
public class GameState
{
	public float PuckX { get; set; }              // 0-800
	public float PuckY { get; set; }              // 0-400
	public float PuckVelocityX { get; set; }      // units/sec
	public float PuckVelocityY { get; set; }      // units/sec
	public float PlayerPaddleY { get; set; }      // 0-320
	public float OpponentPaddleY { get; set; }    // 0-320
	public int PlayerScore { get; set; }          // 0-5
	public int OpponentScore { get; set; }        // 0-5
	public bool GameOver { get; set; }
	public string? Winner { get; set; }           // "Player" or "Opponent"
}
```

### WalletConnectionState
```csharp
public class WalletConnectionState
{
	public bool IsConnected { get; set; }
	public string? Address { get; set; }          // "0x..."
	public int ChainId { get; set; }              // 1, 11155111, 137
	public string ChainName { get; set; }         // "Ethereum Mainnet"
	public decimal Balance { get; set; }          // ETH or MATIC balance
}
```

### GameSession
```csharp
public class GameSession
{
	public int Id { get; set; }
	public string PlayerAddress { get; set; }
	public int PlayerScore { get; set; }
	public int OpponentScore { get; set; }
	public string DifficultyLevel { get; set; }   // Easy, Medium, Hard
	public bool PlayerWon { get; set; }
	public DateTime StartedAt { get; set; }
	public DateTime EndedAt { get; set; }
	public decimal RewardAmount { get; set; }     // 10 or 0
	public string? TransactionHash { get; set; }  // Blockchain tx hash
	public bool RewardClaimed { get; set; }
}
```

### PlayerProfile
```csharp
public class PlayerProfile
{
	public int Id { get; set; }
	public string WalletAddress { get; set; }     // Unique
	public int TotalGames { get; set; }
	public int TotalWins { get; set; }
	public int TotalLosses { get; set; }
	public decimal TotalRewardsEarned { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastPlayedAt { get; set; }
	public List<GameSession> GameSessions { get; set; }

	// Calculated property
	public double WinRate => 
		TotalGames == 0 ? 0 : (double)TotalWins / TotalGames * 100;
}
```

---

## AI Difficulty Levels

### Easy
```csharp
ReactionDelay: 0.5 seconds
PaddleSpeed: 60% = 180 units/sec
Accuracy: Aims at puck with delay
```

### Medium (Default)
```csharp
ReactionDelay: 0.2 seconds
PaddleSpeed: 85% = 255 units/sec
Accuracy: Good tracking with slight delay
```

### Hard
```csharp
ReactionDelay: 0.05 seconds
PaddleSpeed: 100% = 300 units/sec
Accuracy: Excellent puck tracking
```

---

## Database Context

### GameDbContext
Located in `Data/GameDbContext.cs`

**DbSets**:
- `DbSet<PlayerProfile> PlayerProfiles`
- `DbSet<GameSession> GameSessions`

**Relationships**:
```
PlayerProfile (1) ---> (*) GameSession
```

**Indexes**:
- `PlayerProfiles.WalletAddress` (Unique)
- `GameSessions.PlayerAddress` (Non-unique)

---

## Dependency Injection Setup

In `Program.cs`:

```csharp
builder.Services.Configure<BlockchainConfig>(
	builder.Configuration.GetSection("BlockchainConfig"));

builder.Services.AddDbContext<GameDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameEngine, GameEngine>();
```

---

## Error Handling

### Common Exceptions

**MetaMask Connection**
- `JSException`: MetaMask not installed or connection rejected
- Handled in `WalletService.ConnectWalletAsync()`

**Blockchain Operations**
- `HttpRequestException`: RPC endpoint unreachable
- `InvalidOperationException`: Invalid contract address
- Handled in `BlockchainService` methods

**Database Operations**
- `DbUpdateException`: Database write failure
- `DbUpdateConcurrencyException`: Concurrency conflict
- Handled in `GameService` methods

---

## Testing

### Unit Test Examples

```csharp
[TestClass]
public class GameEngineTests
{
	[TestMethod]
	public void InitializeGame_SetsCorrectInitialState()
	{
		var engine = new GameEngine();
		engine.InitializeGame("Medium");
		var state = engine.GetGameState();

		Assert.AreEqual(400, state.PuckX);
		Assert.AreEqual(400, state.PuckY);
		Assert.AreEqual(0, state.PlayerScore);
	}
}
```

---

## Performance Considerations

1. **Game Loop**: Runs at 60 FPS (16ms per frame)
2. **Database Queries**: Leaderboard caches top 50 players
3. **AI Calculations**: Simplified pathfinding (direct targeting)
4. **Network Calls**: Async/await throughout to prevent blocking

---

For implementation details, refer to individual service files.
