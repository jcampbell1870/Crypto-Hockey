# 🏒 CRYPTO HOCKEY - GETTING STARTED CHECKLIST

## ✅ PRE-FLIGHT CHECKLIST (5 minutes)

Before you start, make sure you have:

```
SYSTEM REQUIREMENTS
[ ] Windows 10/11 with PowerShell
[ ] .NET 10 SDK installed
	└─ Verify: dotnet --version
[ ] SQL Server LocalDB installed
	└─ Verify: SqlLocalDB list
[ ] Visual Studio 2026 or VS Code
[ ] 500MB free disk space
[ ] Internet connection (for Alchemy API)

INSTALLED SOFTWARE
[ ] Git (for version control)
[ ] MetaMask browser extension
	└─ Download: https://metamask.io/
[ ] Web browser (Chrome, Edge, Firefox, Safari)

API KEYS READY
[ ] Alchemy account created
	└─ Sign up: https://www.alchemy.com/
[ ] API key copied (for Ethereum Mainnet)
[ ] API key copied (for Sepolia Testnet)
```

---

## 🚀 LAUNCH SEQUENCE (10 minutes)

### Step 1: Navigate to Project (1 min)
```powershell
# Open PowerShell and run:
cd "C:\Users\thund\source\repos\Crypto Hockey\"

# Verify you're in the right place:
dir | grep -i "crypto hockey"  # Should find .csproj file
```

### Step 2: Restore Packages (2 min)
```powershell
dotnet restore

# Should show:
# "Restore completed in X.XXs"
```

### Step 3: Setup Database (2 min)
```powershell
dotnet ef database update

# Should show:
# "Build started..."
# "Build succeeded"
# Database created in LocalDB
```

### Step 4: Run Application (3 min)
```powershell
dotnet run

# Should show:
# "Now listening on: https://localhost:5001"
# Wait for "Application started"
```

### Step 5: Open in Browser (2 min)
```
1. Open your web browser
2. Navigate to: https://localhost:5001
3. You should see the Crypto Hockey home page
```

---

## 🎮 FIRST GAME SETUP (5 minutes)

### Before Playing, You Need:

```
[ ] MetaMask Installed
	└─ Download from: https://metamask.io/
	└─ Create or import wallet
	└─ Have some network selected (Ethereum, Sepolia, or Polygon)

[ ] API Key Configured
	└─ Edit: C:\Users\thund\source\repos\Crypto Hockey\appsettings.json
	└─ Find: "EthereumRpcUrl"
	└─ Replace YOUR_API_KEY with your Alchemy key
	└─ Save file
	└─ Restart dotnet run (Ctrl+C then dotnet run)
```

### Game Startup Steps:

1. **Open Browser**
   - Go to: https://localhost:5001
   - You should see home page with "🏒 Crypto Hockey"

2. **Connect Wallet**
   - Click "Start Playing Now"
   - Click "Connect MetaMask" button
   - MetaMask popup appears
   - Click "Connect" to approve
   - Your wallet address displays in the game

3. **Select Difficulty**
   - Choose: Easy, Medium, or Hard
   - Recommendation: Start with "Medium"

4. **Start Game**
   - Click "Start Game" button
   - Game canvas appears
   - Yellow puck appears in center

5. **Play!**
   - Move mouse up/down to control red paddle
   - Red paddle is on left side
   - Blue paddle is AI (right side)
   - Hit puck to opponent's goal (right side)
   - First to 5 points wins

6. **Win & Reward**
   - If you win (reach 5 points first)
   - "Claim Reward" button appears
   - Click to claim 10 A1870 tokens
   - Tokens transferred to your wallet

7. **Check Stats**
   - Click "Leaderboard" in navigation
   - See your ranking
   - View other players' stats

---

## ⚠️ TROUBLESHOOTING QUICK REFERENCE

### Issue: "Cannot connect to database"
```
Solution:
1. Check LocalDB is running:
   SqlLocalDB start mssqllocaldb

2. Run database setup:
   dotnet ef database update

3. Restart application:
   dotnet run
```

### Issue: "MetaMask not detected"
```
Solution:
1. Make sure MetaMask is installed
   → Download: https://metamask.io/

2. Enable MetaMask in browser
   → Click MetaMask icon in toolbar
   → Make sure it shows "Connected"

3. Reload page (F5)
   → Try connecting again
```

### Issue: "RPC error" or "Network error"
```
Solution:
1. Check API key in appsettings.json
   C:\Users\thund\source\repos\Crypto Hockey\appsettings.json

2. Make sure API key is correct
   → Get new one from: https://www.alchemy.com/

3. Restart application:
   → Stop: Ctrl+C
   → Start: dotnet run

4. Refresh browser: F5
```

### Issue: "HTTPS certificate warning"
```
Solution (Development Only):
1. This is normal for localhost
2. Click "Advanced" or "Continue anyway"
3. You're on HTTPS, this is secure
4. Not an issue
```

### Issue: "Game canvas not rendering"
```
Solution:
1. Check browser console (F12)
   → Look for red error messages
   → Copy error and search Google

2. Try different browser:
   → Chrome, Edge, Firefox

3. Clear browser cache:
   → Ctrl+Shift+Delete (most browsers)

4. Restart browser and try again
```

### Issue: "Cannot find '.NET SDK'"
```
Solution:
1. Make sure .NET 10 is installed:
   dotnet --version

2. If not installed:
   → Download from: https://dotnet.microsoft.com/
   → Install .NET 10 SDK

3. Restart PowerShell and try again
```

---

## 📁 FILES YOU MIGHT NEED TO EDIT

### Main Configuration File
```
File: appsettings.json
Location: C:\Users\thund\source\repos\Crypto Hockey\appsettings.json

Key settings to update:
{
  "BlockchainConfig": {
	"EthereumRpcUrl": "https://eth-mainnet.g.alchemy.com/v2/YOUR_API_KEY",
	"SepoliaRpcUrl": "https://eth-sepolia.g.alchemy.com/v2/YOUR_API_KEY",
	"PolygonRpcUrl": "https://polygon-rpc.com"
  }
}

Steps:
1. Open file with text editor
2. Find YOUR_API_KEY
3. Replace with your actual key from Alchemy
4. Save file
5. Restart application
```

### Other Important Files
```
Program.cs - Application startup
  └─ Usually no changes needed

Crypto Hockey.csproj - Project configuration
  └─ NuGet packages already configured

Components/Game.razor - Main game component
  └─ Game logic and UI

Services/GameEngine.cs - Game physics
  └─ Core game mechanics

Services/BlockchainService.cs - Blockchain integration
  └─ ERC-20 token interaction
```

---

## 🔑 GETTING YOUR API KEY (5 minutes)

### Step-by-Step: Alchemy API Key

1. **Go to Alchemy Website**
   - Visit: https://www.alchemy.com/

2. **Sign Up (if needed)**
   - Click "Get started"
   - Create account with email
   - Verify email address

3. **Create App**
   - Click "Create App" or "New App"
   - Enter app name: "Crypto Hockey"
   - Select chain: "Ethereum"
   - Select network: "Mainnet"
   - Click "Create App"

4. **Copy API Key**
   - Click "View Key"
   - Copy the key starting with "https://eth-mainnet..."
   - This is your API key

5. **Update Configuration**
   - Open: appsettings.json
   - Find: "EthereumRpcUrl"
   - Replace YOUR_API_KEY with your actual key
   - Save file

6. **Restart Application**
   - Stop: Ctrl+C in PowerShell
   - Start: dotnet run

### Alternative: Infura API Key

1. Visit: https://www.infura.io/
2. Sign up and create project
3. Get Ethereum Mainnet URL
4. Use same process as Alchemy above

---

## 🎯 COMMANDS CHEAT SHEET

```powershell
# Navigate to project
cd "C:\Users\thund\source\repos\Crypto Hockey\"

# Restore packages (first time)
dotnet restore

# Create/update database
dotnet ef database update

# Run application
dotnet run

# Build project
dotnet build

# Run tests
dotnet test

# Publish for production
dotnet publish -c Release

# Clean build artifacts
dotnet clean

# Check .NET version
dotnet --version

# Check CLI help
dotnet help
```

---

## 📚 DOCUMENTATION QUICK LINKS

After setup, read these in order:

1. **QUICK_REFERENCE.md** (5 min)
   - Essential commands and controls

2. **SETUP_GUIDE.md** (15 min)
   - Detailed setup and configuration

3. **README.md** (20 min)
   - Complete documentation

4. **SERVICES.md** (30 min)
   - API and architecture details

5. **DEPLOYMENT.md** (25 min)
   - Production deployment guide

6. **ARCHITECTURE.md** (15 min)
   - System design and diagrams

---

## ✅ SUCCESS CRITERIA

You'll know it's working when:

```
[ ] dotnet run succeeds (no errors)
[ ] No red text in console
[ ] Console shows "Now listening on: https://localhost:5001"
[ ] Browser opens page without errors
[ ] Home page loads with "🏒 Crypto Hockey" title
[ ] "Start Playing Now" button visible and clickable
[ ] Can click "Connect MetaMask"
[ ] MetaMask popup appears
[ ] After connecting, wallet address displays
[ ] Can select difficulty level
[ ] Can click "Start Game"
[ ] Game canvas renders with paddles and puck
[ ] Game is playable with mouse control
[ ] Game responds to paddle movement
[ ] Game tracks score correctly
```

When all of these are ✅, you're ready to play!

---

## 🎮 FIRST GAME TIPS

```
Gameplay Tips:
- Move paddle early to anticipate puck
- Hit puck from center of paddle for better control
- Easy mode is good for learning
- Medium mode is balanced and fun
- Hard mode is very challenging

MetaMask Tips:
- Make sure wallet is connected
- You don't need money to play (no gas fees for game)
- Claiming rewards requires MetaMask transaction
- Rewards are 10 A1870 tokens per win

Performance Tips:
- Keep browser window in focus
- Don't minimize mid-game (can affect rendering)
- Close unnecessary browser tabs
- Use a modern browser for best performance
```

---

## 🆘 GETTING HELP

If you get stuck:

1. **Check QUICK_REFERENCE.md**
   - Common issues and solutions

2. **Check SETUP_GUIDE.md**
   - Troubleshooting section

3. **Check browser console**
   - Press F12 to open Developer Tools
   - Look for red error messages
   - Copy error and search Google

4. **Check application logs**
   - Watch PowerShell output
   - Error messages usually appear here

5. **Restart everything**
   - Stop dotnet: Ctrl+C
   - Close browser completely
   - Restart dotnet: dotnet run
   - Open new browser window

---

## 📞 COMMON QUESTIONS

**Q: Do I need a real wallet to play?**
A: You need MetaMask, but you can use a testnet wallet (no real money needed)

**Q: Will claiming rewards cost me money?**
A: On mainnet, yes (gas fees). On testnet, no. Both give you test tokens.

**Q: Can I play offline?**
A: No, you need internet for MetaMask and blockchain connection

**Q: How long does a game take?**
A: Usually 2-5 minutes depending on skill level

**Q: Can I change difficulty mid-game?**
A: No, difficulty is set before game starts. Play again to change.

**Q: Where are my stats saved?**
A: In the database and blockchain (for rewards)

**Q: How do I check my earnings?**
A: Check Leaderboard or your wallet (MetaMask shows balance)

---

## 🎊 YOU'RE READY!

Everything is set up and ready to go.

### Next Step:
```powershell
cd "C:\Users\thund\source\repos\Crypto Hockey\"
dotnet run
```

Then open: **https://localhost:5001**

### Time until first play: ~2 minutes ⚡

---

## 📋 FINAL CHECKLIST BEFORE PLAYING

- [ ] PowerShell open
- [ ] In correct directory
- [ ] .NET 10 installed
- [ ] Database updated
- [ ] Application running
- [ ] Browser open to https://localhost:5001
- [ ] Home page loads
- [ ] MetaMask installed and ready
- [ ] API key configured (in appsettings.json)
- [ ] Ready to connect wallet and play

**All ✅?** Then you're ready! 🎉

---

**Enjoy Crypto Hockey!** 🏒⚡💰

For more info, see: INDEX.md
