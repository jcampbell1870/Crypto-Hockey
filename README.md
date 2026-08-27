# 🏒 Crypto Hockey - Web-Based Air Hockey Game with Blockchain Rewards

A modern, interactive web-based air hockey game built with Blazor, featuring MetaMask wallet integration and ERC-20 token rewards using the Arcade1870 token.

## 🎮 Features

- **Classic Air Hockey Gameplay**: Physics-based puck movement with paddle collision detection
- **AI Opponent**: Three difficulty levels (Easy, Medium, Hard) with adaptive AI
- **MetaMask Integration**: Connect your Web3 wallet securely
- **ERC-20 Token Rewards**: Earn Arcade1870 (A1870) tokens for winning games
- **Player Statistics**: Track your wins, losses, and earned rewards
- **Global Leaderboard**: Compete against other players worldwide
- **Responsive Design**: Play on desktop and mobile devices
- **Dark-Themed UI**: Modern, crypto-friendly interface

## 📋 Prerequisites

- .NET 10 SDK
- Visual Studio 2026 Community or later (or VS Code with C# extensions)
- MetaMask browser extension (for wallet connection)
- SQL Server LocalDB (for local development)

## 🚀 Getting Started

### 1. Clone and Setup

```bash
cd "C:\Users\thund\source\repos\Crypto Hockey\"
dotnet restore
```

### 2. Database Configuration

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoHockeyDb;Trusted_Connection=true;"
}
```

Create the database:

```bash
dotnet ef database update
```

### 3. Blockchain Configuration

Update `appsettings.json` with your RPC endpoints:

```json
"BlockchainConfig": {
	"Arcade1870ContractAddress": "0x8eddD4edea39c5B5f77662453600F53A202EE47C",
	"RewardAmount": "10",
	"EthereumRpcUrl": "https://eth-mainnet.g.alchemy.com/v2/YOUR_ALCHEMY_KEY",
	"SepoliaRpcUrl": "https://eth-sepolia.g.alchemy.com/v2/YOUR_ALCHEMY_KEY",
	"PolygonRpcUrl": "https://polygon-rpc.com",
	"DefaultNetworkChainId": 1,
	"SupportedChainIds": [1, 11155111, 137]
}
```

> **Note**: Get free RPC keys from [Alchemy](https://www.alchemy.com/) or [Infura](https://www.infura.io/)

### 4. Run the Application

```bash
dotnet run
```

Navigate to `https://localhost:5001` in your browser.

## 🎯 How to Play

1. **Connect Wallet**: Click "Connect MetaMask" to link your Web3 wallet
2. **Choose Difficulty**: Select Easy, Medium, or Hard AI opponent
3. **Play**: Use your mouse to control your paddle (left side)
4. **Win & Earn**: First to 5 points wins! Winners earn 10 A1870 tokens
5. **Claim Rewards**: Manually claim your earned tokens after winning

## 🏗️ Project Structure

```
Crypto Hockey/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor           # Landing page
│   │   ├── Game.razor           # Main game component
│   │   └── Leaderboard.razor    # Leaderboard page
│   ├── WalletConnection.razor   # Wallet UI component
│   ├── Layout/                  # Layout components
│   └── App.razor                # Root component
├── Services/
│   ├── WalletService.cs         # MetaMask integration
│   ├── BlockchainService.cs     # Smart contract interaction
│   ├── GameService.cs           # Game logic & database
│   └── GameEngine.cs            # Game physics & AI
├── Models/
│   ├── BlockchainConfig.cs
│   ├── GameSession.cs
│   ├── PlayerProfile.cs
│   └── WalletConnectionState.cs
├── Data/
│   └── GameDbContext.cs         # Entity Framework context
├── wwwroot/
│   ├── js/
│   │   ├── metamask-interop.js  # MetaMask JS interop
│   │   └── game-renderer.js     # Canvas rendering
│   ├── css/
│   │   └── game-styles.css      # Game styling
│   └── app.css                  # Global styles
├── Program.cs                   # Startup configuration
└── appsettings.json            # Configuration file
```

## 🔐 Smart Contract Details

**Arcade1870 Token**
- Contract Address: `0x8eddD4edea39c5B5f77662453600F53A202EE47C`
- Network: Ethereum Mainnet (configurable for testnet)
- Standard: ERC-20
- Reward Per Win: 10 A1870 tokens

[View on Etherscan](https://etherscan.io/token/0x8eddD4edea39c5B5f77662453600F53A202EE47C)

## 🎮 Game Mechanics

### Physics
- Realistic puck movement with velocity tracking
- Paddle-puck collision detection with angle reflection
- Progressive speed increase on paddle hits (max 500 units/sec)
- Boundary collision and bounce

### AI Difficulty Levels
- **Easy**: 60% paddle speed, 0.5s reaction delay
- **Medium**: 85% paddle speed, 0.2s reaction delay
- **Hard**: 100% paddle speed, 0.05s reaction delay

### Scoring
- First to 5 points wins
- Automatic game-over detection
- Session recorded in database
- Manual reward claiming available

## 📊 Database Schema

### PlayerProfile
- WalletAddress (unique)
- TotalGames, TotalWins, TotalLosses
- TotalRewardsEarned
- Win Rate (calculated)

### GameSession
- PlayerAddress
- PlayerScore, OpponentScore
- DifficultyLevel
- RewardAmount (10 tokens per win)
- TransactionHash, RewardClaimed status
- Timestamps

## 🌐 Supported Networks

1. **Ethereum Mainnet** (Chain ID: 1)
2. **Sepolia Testnet** (Chain ID: 11155111)
3. **Polygon Mainnet** (Chain ID: 137)

Switch networks in-game using the "Switch Network" button in wallet panel.

## 🚢 Deployment

### Azure Deployment

```bash
# Install Azure CLI
az webapp create --resource-group MyResourceGroup --plan MyAppServicePlan --name crypto-hockey-app

# Deploy
dotnet publish -c Release
az webapp deployment source config-zip --resource-group MyResourceGroup --name crypto-hockey-app --src bin/Release/net10.0/publish.zip
```

### Docker Deployment

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "Crypto_Hockey.dll"]
```

Build and run:

```bash
docker build -t crypto-hockey .
docker run -p 80:80 crypto-hockey
```

## 🔧 Configuration

### Environment Variables

For production, use environment variables instead of appsettings.json:

```bash
ASPNETCORE_ENVIRONMENT=Production
BlockchainConfig__Arcade1870ContractAddress=0x8eddD4edea39c5B5f77662453600F53A202EE47C
BlockchainConfig__EthereumRpcUrl=https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY
ConnectionStrings__DefaultConnection=your_connection_string
```

## 🧪 Testing

Run unit tests:

```bash
dotnet test
```

## 📱 Mobile Support

- Touch controls supported for mobile play
- Responsive canvas scaling
- Touch event handling for paddle movement

## 🔐 Security Considerations

- MetaMask ensures secure wallet connection (no private keys shared)
- Server-side reward validation recommended for production
- Environment variables for sensitive data
- HTTPS enforced in production
- ANTIFORGERY tokens enabled

## 🐛 Troubleshooting

### MetaMask Not Detected
- Ensure MetaMask extension is installed and enabled
- Check browser console for JS errors
- Clear browser cache and reload

### Database Errors
- Run `dotnet ef database update` to create schema
- Verify LocalDB is running: `SqlLocalDB.exe start mssqllocaldb`

### Connection Issues
- Check appsettings.json configuration
- Verify RPC URL endpoints are working
- Check firewall rules for HTTPS

## 📈 Future Enhancements

- [ ] Multiplayer support (peer-to-peer)
- [ ] Tournament modes
- [ ] NFT rewards for milestones
- [ ] Advanced statistics and analytics
- [ ] Seasonal rewards and events
- [ ] Mobile app (React Native)
- [ ] DAO governance for game parameters

## 📄 License

This project is provided as-is for educational and entertainment purposes.

## 🤝 Contributing

Contributions are welcome! Please follow standard Git practices:

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a pull request

## 💬 Support

For issues, questions, or suggestions, please open an issue on GitHub or contact the development team.

## 🙏 Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
- Blockchain integration via [Nethereum](https://nethereum.com/)
- Web3 wallet support powered by [MetaMask](https://metamask.io/)
- UI framework [Bootstrap 5](https://getbootstrap.com/)

---

**Happy Playing! 🏒⚽💰**

*Crypto Hockey - Where Classic Gaming Meets Web3*
