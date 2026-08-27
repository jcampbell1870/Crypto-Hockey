# Crypto Hockey - Architecture & Data Flow

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Web Browser (Client)                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │           Blazor Interactive Components                   │  │
│  │  ┌─────────────┐ ┌──────────────┐ ┌──────────────────┐   │  │
│  │  │  Home.razor │ │ Game.razor   │ │ Leaderboard.razor│   │  │
│  │  └─────────────┘ └──────────────┘ └──────────────────┘   │  │
│  │       │                  │                     │          │  │
│  │       └──────────────────┴─────────────────────┘          │  │
│  │                          │                                │  │
│  │  ┌──────────────────────────────────────────────────┐    │  │
│  │  │   WalletConnection.razor (Wallet UI)            │    │  │
│  │  └──────────────────────────────────────────────────┘    │  │
│  │                          │                                │  │
│  │  ┌──────────────────────────────────────────────────┐    │  │
│  │  │  Canvas 2D (game-renderer.js)                    │    │  │
│  │  │  - Puck rendering                               │    │  │
│  │  │  - Paddle rendering                             │    │  │
│  │  │  - Score display                                │    │  │
│  │  └──────────────────────────────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                          │                                      │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │    JavaScript Interop Bridge                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  metamask-interop.js                            │   │  │
│  │  │  - Connect wallet                               │   │  │
│  │  │  - Switch networks                              │   │  │
│  │  │  - Get account & balance                        │   │  │
│  │  └──────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
							│
				   ╔════════╩════════╗
				   ║                 ║
				   ▼                 ▼
		 ┌──────────────────┐  ┌──────────────┐
		 │ MetaMask Wallet  │  │  HTTPS/WSS   │
		 │ (User's Browser) │  │  Connection  │
		 └──────────────────┘  └──────────────┘
				   │                 │
				   └────────┬────────┘
							▼
┌─────────────────────────────────────────────────────────────────┐
│                 ASP.NET Core Backend Server                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Blazor Server Components                     │  │
│  │  ┌──────────────────────────────────────────────────┐    │  │
│  │  │  Component State Management                     │    │  │
│  │  │  - Game state                                   │    │  │
│  │  │  - Wallet state                                 │    │  │
│  │  │  - Player stats                                 │    │  │
│  │  └──────────────────────────────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Services Layer                         │  │
│  │  ┌──────────────────┐  ┌──────────────┐                  │  │
│  │  │ IWalletService   │  │ IGameService │                  │  │
│  │  │ - Connect        │  │ - Game CRUD  │                  │  │
│  │  │ - Disconnect     │  │ - Stats      │                  │  │
│  │  │ - Get State      │  │ - Leaderboard│                  │  │
│  │  │ - Switch Network │  │ - Rewards    │                  │  │
│  │  └──────────────────┘  └──────────────┘                  │  │
│  │  ┌──────────────────────────────────────────────────┐    │  │
│  │  │ IBlockchainService        │ IGameEngine         │    │  │
│  │  │ - Send Reward              │ - Physics           │    │  │
│  │  │ - Get Token Balance        │ - AI Logic          │    │  │
│  │  │ - Validate Wallet          │ - Game State        │    │  │
│  │  └──────────────────────────────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Data Access Layer (EF Core)                 │  │
│  │  ┌──────────────────────────────────────────────────┐    │  │
│  │  │  GameDbContext                                  │    │  │
│  │  │  - PlayerProfile DbSet                          │    │  │
│  │  │  - GameSession DbSet                            │    │  │
│  │  └──────────────────────────────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            │                                    │
└─────────────────────────────────────────────────────────────────┘
							│
				   ╔════════╩════════╗
				   ║                 ║
				   ▼                 ▼
		 ┌──────────────────┐  ┌──────────────┐
		 │  SQL Server DB   │  │ RPC Provider │
		 │  (GameSessions,  │  │ (Alchemy,    │
		 │   PlayerProfile) │  │  Infura,     │
		 │                  │  │  Public RPC) │
		 └──────────────────┘  └──────────────┘
				   │                 │
				   └────────┬────────┘
							▼
		 ┌──────────────────────────────────┐
		 │  Blockchain (Ethereum, Polygon)  │
		 │  - Smart Contracts               │
		 │  - Arcade1870 Token (ERC-20)     │
		 └──────────────────────────────────┘
```

---

## 🔄 Data Flow Diagrams

### 1. Game Session Flow

```
User Opens Game
	 │
	 ▼
[WalletConnection Component]
	 │
  Connect? ──No─→ Show "Connect MetaMask" Button
	 │
	Yes
	 │
	 ▼
Call: IWalletService.ConnectWalletAsync()
	 │
	 ▼
JS Interop: window.metamaskInterop.connectWallet()
	 │
	 ▼
MetaMask Extension
	 │
  User Approves
	 │
	 ▼
Return: WalletConnectionState { Address, ChainId, Balance }
	 │
	 ▼
Call: IGameService.GetOrCreatePlayerAsync(address)
	 │
	 ▼
Database: Create/Read PlayerProfile
	 │
	 ▼
[Game.razor Component]
	 │
Select Difficulty → Start Game
	 │
	 ▼
Call: IGameService.CreateGameSessionAsync()
	 │
	 ▼
Initialize: IGameEngine.InitializeGame(difficulty)
	 │
	 ▼
Game Loop Starts (60 FPS)
	 │
	 ├─→ UpdatePaddlePosition(playerY)
	 ├─→ IGameEngine.UpdateGame(deltaTime)
	 ├─→ Render GameState to Canvas
	 └─→ Check Win Condition
	 │
Game Over (First to 5)
	 │
	 ▼
Call: IGameService.EndGameSessionAsync()
	 │
	 ▼
Update: PlayerProfile stats
Update: GameSession with results
	 │
Player Won?
	 │
  ├─No─→ Show "Play Again"
	 │
  ├─Yes─→ Show "Claim Reward" Button
		  │
		  ▼
	   User Clicks "Claim Reward"
		  │
		  ▼
	Call: IGameService.ClaimRewardAsync()
		  │
		  ▼
	Call: IBlockchainService.SendRewardAsync()
		  │
		  ▼
	RPC Call: eth_sendTransaction
		  │
		  ▼
	MetaMask Signs Transaction
		  │
		  ▼
	Smart Contract Receives Request
		  │
		  ▼
	Arcade1870 Token Transferred ✅
		  │
		  ▼
	Update: GameSession.RewardClaimed = true
```

### 2. Leaderboard Flow

```
User Navigates to Leaderboard
	 │
	 ▼
[Leaderboard.razor OnInitializedAsync]
	 │
	 ▼
Call: IGameService.GetLeaderboardAsync(limit: 50)
	 │
	 ▼
Database Query:
   SELECT TOP 50 FROM PlayerProfiles
   ORDER BY TotalWins DESC, TotalRewardsEarned DESC
	 │
	 ▼
Return: List<PlayerProfile>
	 │
	 ▼
Render Table:
  ├─ Rank
  ├─ Player Address
  ├─ Wins
  ├─ Win Rate
  └─ Total Rewards
```

### 3. AI Difficulty Flow

```
Game.razor selects Difficulty
	 │
	 ▼
IGameEngine.InitializeGame("Medium")
	 │
	 ▼
Set AI Parameters:
  ├─ Easy:   ReactionDelay=0.5s,  Speed=60%
  ├─ Medium: ReactionDelay=0.2s,  Speed=85%
  └─ Hard:   ReactionDelay=0.05s, Speed=100%
	 │
	 ▼
Each Frame:
  ├─ Calculate time since last AI update
  ├─ If >= ReactionDelay
  │   ├─ Calculate puck position
  │   ├─ Set target paddle Y
  │   ├─ Move paddle toward target at Speed
  │   └─ Reset reaction timer
  └─ Render new paddle position
```

---

## 🗄️ Database Schema

```
╔══════════════════════════════════╗
║      PlayerProfile               ║
╠══════════════════════════════════╣
║ Id (PK)                          ║ int
║ WalletAddress (UNIQUE)           ║ nvarchar(max)
║ TotalGames                       ║ int
║ TotalWins                        ║ int
║ TotalLosses                      ║ int
║ TotalRewardsEarned               ║ decimal(18,8)
║ CreatedAt                        ║ datetime2
║ LastPlayedAt                     ║ datetime2
║ GameSessions (FK)                ║ List<GameSession>
╚══════════════════════════════════╝
		   │
		   │ 1:N
		   │
╔══════════════════════════════════╗
║      GameSession                 ║
╠══════════════════════════════════╣
║ Id (PK)                          ║ int
║ PlayerAddress                    ║ nvarchar(max)
║ PlayerScore                      ║ int
║ OpponentScore                    ║ int
║ DifficultyLevel                  ║ nvarchar(50)
║ PlayerWon                        ║ bit
║ StartedAt                        ║ datetime2
║ EndedAt                          ║ datetime2
║ RewardAmount                     ║ decimal(18,8)
║ TransactionHash                  ║ nvarchar(max)
║ RewardClaimed                    ║ bit
╚══════════════════════════════════╝
```

---

## 🎮 Game Engine State Machine

```
┌─────────┐
│ INIT    │
└────┬────┘
	 │
	 ▼
┌──────────────────────┐
│ GAME_READY           │
│ - Paddles centered   │
│ - Puck at center     │
│ - Scores at 0        │
└────┬─────────────────┘
	 │ Player clicks "Start"
	 ▼
┌──────────────────────┐
│ GAME_RUNNING         │
│ - Physics updates    │
│ - AI moves           │
│ - Collisions check   │
│ - Score updates      │
└────┬─────────────────┘
	 │ Player score = 5 OR Opponent score = 5
	 ▼
┌──────────────────────┐
│ GAME_OVER            │
│ - Winner determined  │
│ - Stats recorded     │
│ - Rewards available  │
└────┬─────────────────┘
	 │ Player clicks "Play Again"
	 ▼
┌──────────────────────┐
│ GAME_RESET           │
└────┬─────────────────┘
	 │
	 └──→ Back to GAME_READY
```

---

## 🔐 Authentication & Authorization Flow

```
User Visits Game
	 │
	 ▼
MetaMask Installed?
	 │
  ├─No─→ Show "Install MetaMask"
	 │
  ├─Yes→ Show "Connect Wallet" Button
		│
		▼
   User Clicks Connect
		│
		▼
   window.ethereum.request({
	 method: 'eth_requestAccounts'
   })
		│
		▼
   MetaMask Popup Shown
		│
   User Approves/Rejects
		│
  ├─Reject→ Show Error
	 │
  ├─Approve→ Return Wallet Address
		   │
		   ▼
		Validate Address Format (0x... 42 chars)
		   │
	  ├─Invalid→ Show Error
		   │
	  ├─Valid→ Store in Component State
			 │
			 ▼
		  Get Current Chain ID
			 │
			 ▼
		  Is Chain Supported?
		  (1, 11155111, 137)
			 │
		  ├─No→ Show "Switch Network"
			 │
		  ├─Yes→ Wallet Connected ✅
				 Can Play & Claim Rewards
```

---

## ⚡ Performance Optimization Points

```
Game Loop (60 FPS Target)
	 │
  16ms per frame
	 │
  ├─ Input Processing:        1-2ms
  ├─ Game State Update:        2-3ms
  ├─ Physics Calculations:     2-3ms
  ├─ AI Logic:                 1-2ms
  ├─ Canvas Rendering:         5-7ms
  └─ Remaining Buffer:         2-3ms
	 │
  Total: ~14-20ms (safe)
```

---

## 🌐 Network Communication

```
Client ←→ Server (SignalR/HTTPS)
  │
  ├─ Game State Updates
  │   └─ 60 times/second (local calculation)
  │
  ├─ Game Session Start
  │   └─ 1 request
  │
  ├─ Game Session End
  │   └─ 1 request (async)
  │
  ├─ Wallet Connection
  │   └─ Direct to MetaMask (no server)
  │
  ├─ Reward Claim
  │   └─ 1 request to start, then
  │       Direct to Blockchain via RPC
  │
  └─ Leaderboard Fetch
	  └─ 1 request on page load


Blockchain (JSON-RPC)
  ├─ Get Balance (optional)
  ├─ Send Token Transfer (on reward claim)
  └─ Get Transaction Status (optional)
```

---

## 🚀 Deployment Architecture

```
┌──────────────────────────────────────────────┐
│         Azure App Service (Production)       │
├──────────────────────────────────────────────┤
│                                              │
│  ┌──────────────────────────────────────┐   │
│  │  Crypto Hockey Application           │   │
│  │  - Blazor Server                     │   │
│  │  - Services                          │   │
│  │  - Static Files (CSS, JS)            │   │
│  └──────────────────────────────────────┘   │
│                  │                          │
│  ┌──────────────────────────────────────┐   │
│  │  Auto-scaling (0-10 instances)       │   │
│  │  - Load based on traffic             │   │
│  │  - Geographic distribution (optional)│   │
│  └──────────────────────────────────────┘   │
└──────────────────────────────────────────────┘
			│                    │
	┌───────┴─────┐      ┌───────┴──────┐
	▼             ▼      ▼              ▼
┌────────────┐ ┌──────┐ ┌────────┐ ┌─────────┐
│ Azure SQL  │ │ CDN  │ │  Vault │ │ Monitor │
│  Database  │ │      │ │ (Keys) │ │ Insights│
└────────────┘ └──────┘ └────────┘ └─────────┘
```

---

## 📈 Scaling Considerations

```
Per Instance:
  - 2GB RAM
  - 2 vCPU cores
  - ~100 concurrent players per instance

Auto-scale triggers:
  - CPU > 70% → Scale up
  - CPU < 30% → Scale down
  - Response time > 5s → Scale up

Database:
  - Connection pooling (max 100)
  - Read replicas for leaderboard (optional)
  - Caching for top 50 players
```

---

**This architecture is designed for scalability, security, and optimal user experience.**

For implementation details, see [SERVICES.md](SERVICES.md)
