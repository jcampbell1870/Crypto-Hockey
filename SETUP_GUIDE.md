# Crypto Hockey Setup Guide

## Quick Start (5 minutes)

### 1. Install Dependencies

```powershell
# Open PowerShell and navigate to project directory
cd "C:\Users\thund\source\repos\Crypto Hockey\"

# Restore NuGet packages
dotnet restore
```

### 2. Setup Database

```powershell
# Create the initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to create database
dotnet ef database update

# Verify database was created:
# Check (localdb)\mssqllocaldb\CryptoHockeyDb
```

### 3. Configure Blockchain Settings

Edit `appsettings.json` and add your RPC URLs:

```json
{
  "BlockchainConfig": {
	"Arcade1870ContractAddress": "0x8eddD4edea39c5B5f77662453600F53A202EE47C",
	"RewardAmount": "10",
	"EthereumRpcUrl": "https://eth-mainnet.g.alchemy.com/v2/YOUR_ALCHEMY_KEY",
	"SepoliaRpcUrl": "https://eth-sepolia.g.alchemy.com/v2/YOUR_ALCHEMY_KEY",
	"PolygonRpcUrl": "https://polygon-rpc.com",
	"DefaultNetworkChainId": 1,
	"SupportedChainIds": [1, 11155111, 137]
  }
}
```

**Get Free RPC Keys:**
1. Visit [Alchemy](https://www.alchemy.com/) or [Infura](https://www.infura.io/)
2. Sign up and create a new app
3. Copy your API key and paste into appsettings.json

### 4. Install MetaMask (if not already installed)

1. Download [MetaMask for your browser](https://metamask.io/)
2. Create or import a wallet
3. Switch to Ethereum Mainnet or Sepolia Testnet

### 5. Run the Application

```powershell
dotnet run

# Application will start at https://localhost:5001
```

### 6. Test the Game

1. Open `https://localhost:5001` in your browser
2. Click "Start Playing Now"
3. Click "Connect MetaMask"
4. Approve the connection in MetaMask popup
5. Select a difficulty level
6. Click "Start Game"
7. Use mouse to control your paddle (left side)
8. Try to score more than the AI opponent (first to 5 wins)
9. If you win, click "Claim Reward"

## Configuration Details

### appsettings.json Structure

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "AllowedHosts": "*",

  // Blockchain Configuration
  "BlockchainConfig": {
	// Arcade1870 Token Details
	"Arcade1870ContractAddress": "0x8eddD4edea39c5B5f77662453600F53A202EE47C",
	"RewardAmount": "10",  // Tokens per win

	// RPC Endpoints (get from Alchemy, Infura, or public endpoints)
	"EthereumRpcUrl": "https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY",
	"SepoliaRpcUrl": "https://eth-sepolia.g.alchemy.com/v2/YOUR_KEY",
	"PolygonRpcUrl": "https://polygon-rpc.com",

	// Network Configuration
	"DefaultNetworkChainId": 1,  // 1=Mainnet, 11155111=Sepolia, 137=Polygon
	"SupportedChainIds": [1, 11155111, 137]
  },

  // Database Configuration
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoHockeyDb;Trusted_Connection=true;"
  }
}
```

### Supported Networks

| Network | Chain ID | RPC URL |
|---------|----------|---------|
| Ethereum Mainnet | 1 | https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY |
| Sepolia Testnet | 11155111 | https://eth-sepolia.g.alchemy.com/v2/YOUR_KEY |
| Polygon Mainnet | 137 | https://polygon-rpc.com |
| Polygon Mumbai | 80001 | https://polygon-mumbai.g.alchemy.com/v2/YOUR_KEY |

## Project Structure

```
Crypto Hockey/
│
├── Models/
│   ├── BlockchainConfig.cs         # Blockchain settings configuration
│   ├── GameSession.cs              # Game session data model
│   ├── PlayerProfile.cs            # Player statistics and history
│   └── WalletConnectionState.cs    # Wallet connection state
│
├── Services/
│   ├── WalletService.cs            # MetaMask integration service
│   ├── BlockchainService.cs        # Smart contract interaction
│   ├── GameService.cs              # Game business logic
│   └── GameEngine.cs               # Game physics and AI
│
├── Data/
│   └── GameDbContext.cs            # Entity Framework DbContext
│
├── Components/
│   ├── Pages/
│   │   ├── Home.razor              # Landing page
│   │   ├── Game.razor              # Main game component
│   │   └── Leaderboard.razor       # Leaderboard display
│   │
│   ├── WalletConnection.razor      # Wallet connection UI
│   ├── Layout/                     # Layout components
│   └── App.razor                   # Root component
│
├── wwwroot/
│   ├── js/
│   │   ├── metamask-interop.js     # MetaMask JavaScript bridge
│   │   └── game-renderer.js        # Canvas 2D rendering engine
│   │
│   ├── css/
│   │   └── game-styles.css         # Game styling
│   │
│   ├── lib/bootstrap/              # Bootstrap framework
│   └── app.css                     # Global styles
│
├── Properties/
│   └── launchSettings.json         # Debug configuration
│
├── Program.cs                      # Application startup
├── appsettings.json               # Configuration
└── Crypto Hockey.csproj           # Project file
```

## Using the Game

### Gameplay Instructions

1. **Connecting Wallet**
   - Click "Connect MetaMask" button
   - Approve the connection in MetaMask popup
   - Your wallet address will be displayed

2. **Selecting Difficulty**
   - Easy: AI moves slower, less accurate
   - Medium: Balanced difficulty (recommended)
   - Hard: AI moves at full speed, very fast

3. **Playing**
   - Move your mouse up/down to control your paddle (red, left side)
   - Click "Start Game" to begin
   - Try to hit the yellow puck toward the opponent's goal (right side)
   - First player to 5 points wins

4. **Claiming Rewards**
   - If you win, a "Claim Reward" button appears
   - Click to claim your 10 A1870 tokens
   - Tokens will be transferred to your connected wallet

### Game Rules

- **Scoring**: When the puck crosses the opponent's end line
- **Win Condition**: First to 5 points
- **Collision Physics**: Puck angle depends on paddle strike position
- **Speed**: Ball speeds up with each paddle hit (max speed limit)

## Database Schema

### PlayerProfile Table

```sql
CREATE TABLE PlayerProfiles (
	Id INT PRIMARY KEY IDENTITY(1,1),
	WalletAddress NVARCHAR(MAX) UNIQUE NOT NULL,
	TotalGames INT,
	TotalWins INT,
	TotalLosses INT,
	TotalRewardsEarned DECIMAL(18,8),
	CreatedAt DATETIME2,
	LastPlayedAt DATETIME2
);
```

### GameSession Table

```sql
CREATE TABLE GameSessions (
	Id INT PRIMARY KEY IDENTITY(1,1),
	PlayerAddress NVARCHAR(MAX),
	PlayerScore INT,
	OpponentScore INT,
	DifficultyLevel NVARCHAR(50),
	PlayerWon BIT,
	StartedAt DATETIME2,
	EndedAt DATETIME2,
	RewardAmount DECIMAL(18,8),
	TransactionHash NVARCHAR(MAX),
	RewardClaimed BIT
);
```

## Environment Variables (Production)

For production deployment, set these environment variables:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "https://+:443;http://+:80"
$env:BlockchainConfig__Arcade1870ContractAddress = "0x8eddD4edea39c5B5f77662453600F53A202EE47C"
$env:BlockchainConfig__EthereumRpcUrl = "https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY"
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SERVER;Database=CryptoHockeyDb;User Id=sa;Password=YOUR_PASSWORD;"
```

## Common Issues & Solutions

### Issue: "MetaMask is not installed"
**Solution**: 
1. Install MetaMask from https://metamask.io/
2. Create or import a wallet
3. Refresh the page

### Issue: "Database error" when starting game
**Solution**:
```powershell
# Stop the application
# Ensure LocalDB is running:
SqlLocalDB start mssqllocaldb

# Delete and recreate database:
dotnet ef database drop
dotnet ef database update
```

### Issue: "RPC error" or connection refused
**Solution**:
1. Verify your RPC URL in appsettings.json
2. Check that Alchemy/Infura account is active
3. Verify API key is correct and has balance (for testnet)
4. Test RPC endpoint directly in browser

### Issue: Transaction fails when claiming reward
**Solution**:
1. Ensure wallet has gas (ETH/MATIC)
2. Check token contract address is correct
3. Verify RPC endpoint has access to token contract
4. Increase gas limit in appsettings if needed

## Testing the Deployment

1. **Unit Tests**
   ```powershell
   dotnet test
   ```

2. **Manual Testing Checklist**
   - [ ] MetaMask connects successfully
   - [ ] Game starts on "Start Game" click
   - [ ] Puck moves and bounces correctly
   - [ ] AI paddle follows the puck
   - [ ] Score updates when puck crosses baseline
   - [ ] Game ends at 5 points
   - [ ] Reward claim button appears on win
   - [ ] Leaderboard displays top players
   - [ ] Player stats update after game
   - [ ] Network switch works in wallet panel

## Performance Optimization

### For Large-Scale Deployment

1. **Database Optimization**
   ```sql
   CREATE INDEX idx_player_address ON GameSessions(PlayerAddress);
   CREATE INDEX idx_wallet_address ON PlayerProfiles(WalletAddress);
   ```

2. **Caching** (Optional)
   - Implement Redis for leaderboard caching
   - Cache game session data

3. **Load Balancing**
   - Deploy multiple instances with sticky sessions
   - Use Azure Load Balancer or similar

## Next Steps

1. ✅ Run the application locally
2. ✅ Test game mechanics
3. ✅ Verify MetaMask integration
4. Deploy to staging environment
5. Deploy to production

---

For detailed documentation, see [README.md](README.md)
