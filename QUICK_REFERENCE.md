# 🏒 Crypto Hockey - Quick Reference Card

## 🚀 Get Started in 60 Seconds

```powershell
# 1. Navigate to project
cd "C:\Users\thund\source\repos\Crypto Hockey\"

# 2. Setup database
dotnet ef database update

# 3. Run application
dotnet run

# 4. Open browser
https://localhost:5001
```

## 🎮 Game Controls

| Action | Control |
|--------|---------|
| Move Paddle | Mouse Up/Down |
| Start Game | Click "Start Game" Button |
| Pause Game | Click "Pause Game" Button |
| Claim Reward | Click "Claim Reward" (after winning) |

## 🔗 Key URLs

| Page | URL |
|------|-----|
| Home | https://localhost:5001 |
| Game | https://localhost:5001/game |
| Leaderboard | https://localhost:5001/leaderboard |

## ⚙️ Configuration Quick Reference

**File**: `appsettings.json`

```json
// Get free RPC key from alchemy.com or infura.io
"EthereumRpcUrl": "https://eth-mainnet.g.alchemy.com/v2/YOUR_API_KEY"

// Database (LocalDB by default)
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoHockeyDb;..."

// Token Contract
"Arcade1870ContractAddress": "0x8eddD4edea39c5B5f77662453600F53A202EE47C"
```

## 🔧 Essential Commands

```powershell
# Restore packages
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test

# Create migration (after model changes)
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Run application
dotnet run

# Publish for production
dotnet publish -c Release
```

## 📁 Project Structure (At a Glance)

```
Components/        → UI Components (Razor)
  Pages/          → Game, Home, Leaderboard
  WalletConnection.razor → Wallet UI

Services/         → Business Logic
  WalletService.cs        → MetaMask
  GameService.cs          → Game Logic
  BlockchainService.cs    → Smart Contracts
  GameEngine.cs           → Physics & AI

Models/           → Data Models
Data/             → Database Context
wwwroot/          → Static Files
  js/             → JavaScript interop
  css/            → Game styling
```

## 🔐 Blockchain Networks Supported

| Network | Chain ID | Mainnet |
|---------|----------|---------|
| Ethereum | 1 | ✅ Yes |
| Sepolia | 11155111 | ❌ Testnet |
| Polygon | 137 | ✅ Yes |

## 💰 Token Information

- **Name**: Arcade1870
- **Symbol**: A1870
- **Address**: `0x8eddD4edea39c5B5f77662453600F53A202EE47C`
- **Standard**: ERC-20
- **Decimals**: 18
- **Reward per Win**: 10 tokens
- **Explorer**: https://etherscan.io/token/0x8eddD4edea39c5B5f77662453600F53A202EE47C

## 🎯 Game Rules

| Rule | Details |
|------|---------|
| Objective | Get more points than AI opponent |
| Win Condition | First to 5 points |
| Scoring | 1 point per goal (puck past opponent) |
| Paddle Control | Mouse moves your paddle (red, left) |
| AI Paddle | Automatically tracks puck (blue, right) |

## 🤖 AI Difficulty Levels

| Difficulty | Speed | Reaction | Best For |
|------------|-------|----------|----------|
| Easy | 60% | 0.5s | Beginners |
| Medium | 85% | 0.2s | Most players |
| Hard | 100% | 0.05s | Challenge |

## 📊 Player Statistics Tracked

- Total Games Played
- Total Wins
- Total Losses
- Win Rate (%)
- Total Rewards Earned (A1870)
- Last Played Date

## 🐛 Quick Troubleshooting

| Issue | Solution |
|-------|----------|
| MetaMask not found | Install from metamask.io |
| Database error | Run `dotnet ef database update` |
| RPC connection failed | Check Alchemy API key in appsettings.json |
| Game won't start | Check browser console for errors |
| Wallet won't connect | Allow pop-ups in browser settings |

## 📚 Documentation Map

- **START HERE**: SETUP_GUIDE.md
- **Understanding Services**: SERVICES.md
- **Deployment**: DEPLOYMENT.md
- **Full Details**: README.md
- **Project Summary**: IMPLEMENTATION_COMPLETE.md

## 🌐 Browser Requirements

- Modern browser with ES6+ support
- MetaMask extension installed
- JavaScript enabled
- Cookies enabled
- WebSocket support (for real-time updates)

## 🔐 Security Essentials

✅ Do:
- Use HTTPS in production
- Keep API keys in environment variables
- Validate user input on server-side
- Use MetaMask for wallet security
- Keep dependencies updated

❌ Don't:
- Expose API keys in code
- Store private keys in database
- Skip HTTPS in production
- Disable MetaMask security features
- Run unverified contracts

## 💾 Database Backup

```powershell
# Backup LocalDB
SqlLocalDB backup "mssqllocaldb" "C:\backups\CryptoHockey.bak"

# Restore LocalDB
SqlLocalDB restore "mssqllocaldb" "C:\backups\CryptoHockey.bak"
```

## 📈 Performance Tips

- Keep game at 60 FPS
- Optimize canvas rendering
- Use database indexes
- Implement caching for leaderboard
- Minimize network requests
- Compress static assets

## 🚀 Deployment Checklist

- [ ] Build succeeds
- [ ] Database configured
- [ ] RPC endpoints working
- [ ] Secrets in environment variables
- [ ] HTTPS enabled
- [ ] Monitoring configured
- [ ] Backups scheduled
- [ ] Tested on staging
- [ ] Performance acceptable
- [ ] Security audit passed

## 📞 Common Questions

**Q: How do I add more AI difficulties?**
A: Edit `GameEngine.cs` and add new difficulty levels in the `UpdateAI` method.

**Q: Can I change the reward amount?**
A: Yes, edit `appsettings.json` → `BlockchainConfig` → `RewardAmount`.

**Q: How do I add multiplayer?**
A: Would require SignalR for real-time communication. See GitHub for multiplayer branch.

**Q: Can I run this without MetaMask?**
A: No, MetaMask connection is required for rewards to work as designed.

**Q: How often should I backup?**
A: Daily recommended. See DEPLOYMENT.md for backup configuration.

---

## 🎉 You're All Set!

Everything is ready to run. Start with:

```powershell
dotnet run
```

Then visit: **https://localhost:5001**

Enjoy your Crypto Hockey game! 🏒⚡💰

---

**Need help?** Check out:
- SETUP_GUIDE.md for detailed setup
- README.md for complete documentation  
- SERVICES.md for API reference
- DEPLOYMENT.md for production deployment
