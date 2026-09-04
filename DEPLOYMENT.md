# Crypto Hockey - Deployment Checklist & Production Guide

## Pre-Deployment Verification

### Code Quality
- [ ] All compilation errors resolved (`dotnet build` succeeds)
- [ ] No warnings or deprecation notices
- [ ] Code follows C# naming conventions
- [ ] Comments added to complex logic
- [ ] No hardcoded secrets or API keys

### Testing
- [ ] Unit tests pass (`dotnet test`)
- [ ] MetaMask connection tested
- [ ] Game mechanics verified (all scores work correctly)
- [ ] AI difficulty levels tested (Easy, Medium, Hard)
- [ ] Leaderboard displays correctly
- [ ] Reward claiming tested (if backend wallet available)

### Security
- [ ] HTTPS enabled (production)
- [ ] Secrets moved to environment variables
- [ ] API keys rotated and secured
- [ ] Database connection encrypted
- [ ] CORS properly configured
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention verified (using EF Core)
- [ ] XSS prevention verified (Blazor's built-in protection)

### Configuration
- [ ] appsettings.json validated
- [ ] appsettings.Production.json created
- [ ] Database connection string verified
- [ ] RPC endpoints confirmed working
- [ ] Blockchain config matches actual contract addresses
- [ ] Supported networks properly configured

---

## Local Development Environment

### Prerequisites Installed
- [ ] .NET 10 SDK
- [ ] SQL Server or SQL Server Express
- [ ] Visual Studio 2026 or VS Code
- [ ] Git
- [ ] MetaMask browser extension
- [ ] Node.js (if running build scripts)

### Initial Setup
```powershell
# Navigate to project
cd "C:\Users\thund\source\repos\Crypto Hockey\"

# Install/restore packages
dotnet restore

# Create database
dotnet ef database update

# Run application
dotnet run
```

### Verification
- [ ] Application starts at https://localhost:5001
- [ ] Home page loads without errors
- [ ] MetaMask connection works
- [ ] Game page loads with canvas
- [ ] Leaderboard page loads
- [ ] Database queries complete successfully

---

## Staging Deployment

### Azure App Service Staging Slot

```powershell
# Create staging slot
az webapp deployment slot create \
  --resource-group MyResourceGroup \
  --name crypto-hockey-prod \
  --slot staging

# Deploy to staging
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath publish.zip
az webapp deployment source config-zip \
  --resource-group MyResourceGroup \
  --name crypto-hockey-prod \
  --slot staging \
  --src publish.zip
```

### Staging Verification Checklist

- [ ] Application loads at staging URL
- [ ] MetaMask connection functional
- [ ] Game mechanics work correctly
- [ ] Database operations successful
- [ ] Leaderboard loads within 2 seconds
- [ ] No console errors in browser
- [ ] Responsive design works on mobile
- [ ] Performance acceptable (game runs at 60 FPS)
- [ ] Error pages display correctly
- [ ] 404 handling works

### Load Testing (Optional)

```bash
# Using Apache Bench
ab -n 100 -c 10 https://crypto-hockey-staging.azurewebsites.net/

# Using JMeter for more complex scenarios
# See JMeter test plan: tests/load-test.jmx
```

---

## Production Deployment

### Pre-Production Checklist

**Security**
- [ ] SSL/TLS certificate installed
- [ ] HSTS headers enabled
- [ ] Security headers configured (CSP, X-Frame-Options, etc.)
- [ ] CORS whitelist configured
- [ ] Rate limiting enabled (if using API)
- [ ] DDoS protection enabled (if using Azure)

**Performance**
- [ ] Database indexes created
- [ ] Caching strategy implemented
- [ ] CDN configured for static assets
- [ ] Image optimization complete
- [ ] Minification enabled (CSS/JS)

**Monitoring**
- [ ] Application Insights configured
- [ ] Health check endpoint created
- [ ] Error logging configured
- [ ] Performance monitoring enabled
- [ ] Alerts configured for failures

**Backup & Recovery**
- [ ] Database backups configured (daily)
- [ ] Disaster recovery plan documented
- [ ] Backup testing scheduled
- [ ] Rollback procedure documented

### Azure Production Deployment

```powershell
# Create production resource group
az group create \
  --name CryptoHockeyProd \
  --location eastus

# Create App Service Plan
az appservice plan create \
  --name CryptoHockeyPlan \
  --resource-group CryptoHockeyProd \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group CryptoHockeyProd \
  --plan CryptoHockeyPlan \
  --name crypto-hockey-prod \
  --runtime "DOTNETCORE|10.0"

# Configure database (Azure SQL or managed)
az sql server create \
  --name crypto-hockey-sql \
  --resource-group CryptoHockeyProd \
  --admin-user sqladmin \
  --admin-password "StrongPassword123!"

# Deploy application
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath publish.zip
az webapp deployment source config-zip \
  --resource-group CryptoHockeyProd \
  --name crypto-hockey-prod \
  --src publish.zip
```

### Production Configuration

**appsettings.Production.json**
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Warning",
	  "Microsoft.AspNetCore": "Error"
	}
  },
  "AllowedHosts": "crypto-hockey.com,www.crypto-hockey.com",
  "BlockchainConfig": {
	"TokenContractAddress": "0xcF0A9F89ab34D39C11B5e08e1c6aC33A47e207c8",
	"TokenSymbol": "1870Coin",
	"RewardAmount": 10,
	"RewardVaultAddress": "<same Crypto Trivia treasury address>",
	"RewardIssuerUrl": "<Render reward issuer URL>",
	"DefaultNetworkChainId": 1,
	"SupportedChainIds": [1, 137]
  }
}
```

**Environment Variables**
```powershell
# Set in Azure App Service Configuration

ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = https://+:443;http://+:80

# Database
ConnectionStrings__DefaultConnection = Server=tcp:crypto-hockey-sql.database.windows.net,1433;Initial Catalog=CryptoHockeyDb;Persist Security Info=False;User ID=sqladmin;Password=[PASSWORD];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

# Blockchain
BlockchainConfig__TokenContractAddress = 0xcF0A9F89ab34D39C11B5e08e1c6aC33A47e207c8
BlockchainConfig__TokenSymbol = 1870Coin
BlockchainConfig__RewardVaultAddress = <same Crypto Trivia treasury address>
BlockchainConfig__RewardIssuerUrl = <Render reward issuer URL>
BlockchainConfig__EthereumRpcUrl = https://eth-mainnet.g.alchemy.com/v2/[YOUR_API_KEY]
BlockchainConfig__PolygonRpcUrl = https://polygon-rpc.com

# Analytics
APPINSIGHTS_INSTRUMENTATION_KEY = [YOUR_KEY]
```

---

## Docker Deployment

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Crypto_Hockey.dll"]
```

### Build and Push to Docker Hub
```bash
# Build image
docker build -t yourusername/crypto-hockey:1.0 .

# Login to Docker Hub
docker login

# Push image
docker push yourusername/crypto-hockey:1.0

# Run locally
docker run -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your_connection_string" \
  yourusername/crypto-hockey:1.0
```

### Kubernetes Deployment (Optional)
```yaml
apiVersion: v1
kind: Service
metadata:
  name: crypto-hockey-service
spec:
  selector:
	app: crypto-hockey
  ports:
	- protocol: TCP
	  port: 80
	  targetPort: 80
  type: LoadBalancer

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: crypto-hockey-deployment
spec:
  replicas: 3
  selector:
	matchLabels:
	  app: crypto-hockey
  template:
	metadata:
	  labels:
		app: crypto-hockey
	spec:
	  containers:
	  - name: crypto-hockey
		image: yourusername/crypto-hockey:1.0
		ports:
		- containerPort: 80
		env:
		- name: ASPNETCORE_ENVIRONMENT
		  value: "Production"
		- name: ConnectionStrings__DefaultConnection
		  valueFrom:
			secretKeyRef:
			  name: db-secret
			  key: connection-string
```

---

## Post-Deployment Verification

### Smoke Tests
```powershell
# Test home page
$response = Invoke-WebRequest -Uri "https://crypto-hockey.com" -UseBasicParsing
$response.StatusCode # Should be 200

# Test game page
$response = Invoke-WebRequest -Uri "https://crypto-hockey.com/game" -UseBasicParsing
$response.StatusCode # Should be 200

# Test API health
$response = Invoke-WebRequest -Uri "https://crypto-hockey.com/api/health" -UseBasicParsing
$response.StatusCode # Should be 200
```

### Functional Tests
- [ ] Navigate to home page
- [ ] Click "Play Game"
- [ ] Connect MetaMask wallet
- [ ] Start game
- [ ] Play and win a game
- [ ] Check leaderboard
- [ ] View player stats

### Performance Monitoring
- [ ] Page load time < 3 seconds
- [ ] Game runs at 60 FPS
- [ ] Leaderboard query < 500ms
- [ ] Database response time < 100ms

### Error Monitoring
- [ ] Check Application Insights for exceptions
- [ ] Review error logs for any issues
- [ ] Check Azure Monitor alerts
- [ ] Test 404 and error pages

---

## Maintenance & Monitoring

### Daily Checks
- [ ] Application is responding
- [ ] Database is accessible
- [ ] No spike in error rates
- [ ] Performance metrics normal

### Weekly Checks
- [ ] Review error logs for patterns
- [ ] Check database size and growth
- [ ] Verify backup completion
- [ ] Performance analysis

### Monthly Checks
- [ ] Update security patches
- [ ] Review cost optimization
- [ ] Test disaster recovery
- [ ] User feedback review

### Quarterly Checks
- [ ] Security audit
- [ ] Load testing
- [ ] Database optimization
- [ ] Dependency updates

---

## Rollback Procedure

### Quick Rollback (Azure Deployment Slots)
```powershell
# If using staging slot, swap back
az webapp deployment slot swap \
  --resource-group CryptoHockeyProd \
  --name crypto-hockey-prod \
  --slot staging
```

### Manual Rollback
```powershell
# Deploy previous version
$previousVersion = "publish-v1.0.0.zip"
az webapp deployment source config-zip \
  --resource-group CryptoHockeyProd \
  --name crypto-hockey-prod \
  --src $previousVersion
```

### Database Rollback
```sql
-- If migrations failed, roll back to previous
dotnet ef database update PreviousMigration

-- Or restore from backup
-- Use Azure SQL point-in-time restore feature
```

---

## Troubleshooting Production Issues

### Application Won't Start
```powershell
# Check logs
az webapp log tail --name crypto-hockey-prod --resource-group CryptoHockeyProd

# Restart app
az webapp restart --name crypto-hockey-prod --resource-group CryptoHockeyProd
```

### Database Connection Issues
```powershell
# Verify connection string
az webapp config appsettings list --name crypto-hockey-prod --resource-group CryptoHockeyProd

# Test connection
# Try connecting with SQL Server Management Studio using the connection string
```

### High Memory Usage
```powershell
# Check process info
az monitor metrics list-definitions --namespace "Microsoft.Web/sites" --resource crypto-hockey-prod

# Scale up if needed
az appservice plan update --name CryptoHockeyPlan --sku B3 --resource-group CryptoHockeyProd
```

### MetaMask Connection Issues
- Check browser console for JS errors
- Verify domain is whitelisted in MetaMask (if using dApp registry)
- Ensure HTTPS is enabled

---

## Security Hardening

### Secrets Management
Use Azure Key Vault:
```powershell
# Create key vault
az keyvault create --name CryptoHockeyVault --resource-group CryptoHockeyProd

# Add secrets
az keyvault secret set --vault-name CryptoHockeyVault --name db-connection --value "connection_string"
az keyvault secret set --vault-name CryptoHockeyVault --name blockchain-key --value "your_api_key"

# Link to App Service
az webapp identity assign --name crypto-hockey-prod --resource-group CryptoHockeyProd
```

### SSL/TLS Certificate
```powershell
# Import certificate
az webapp config ssl bind --name crypto-hockey-prod --certificate-thumbprint YOUR_THUMBPRINT --ssl-type SNI --resource-group CryptoHockeyProd
```

### WAF Rules (Web Application Firewall)
Configure Azure WAF for:
- SQL injection protection
- XSS protection
- Rate limiting
- Bot protection

---

## Performance Optimization

### Database Optimization
```sql
-- Create indexes
CREATE INDEX idx_playerscore ON GameSessions(PlayerAddress, PlayerWon);
CREATE INDEX idx_leaderboard ON PlayerProfiles(TotalWins DESC, TotalRewardsEarned DESC);

-- Statistics update
UPDATE STATISTICS PlayerProfiles;
UPDATE STATISTICS GameSessions;
```

### Caching Strategy
```csharp
// Add Redis caching (optional)
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

### Static Asset Optimization
- Minify CSS and JavaScript
- Compress images
- Enable CDN (Azure CDN)
- Set cache headers for static files

---

## Compliance & Legal

- [ ] Privacy Policy updated
- [ ] Terms of Service reviewed
- [ ] Cookie consent implemented
- [ ] GDPR compliance verified
- [ ] Age verification if applicable
- [ ] Disclaimers displayed

---

## Success Criteria

✅ **Deployment Successful When**:
1. Application is accessible and responsive
2. All critical features work correctly
3. No critical errors in logs
4. Performance meets targets (< 3s page load)
5. Security checks pass
6. Database is accessible and backed up
7. Monitoring and alerts are active

---

## Emergency Contacts

- **DevOps Lead**: [Contact Info]
- **Database Admin**: [Contact Info]
- **Security Team**: [Contact Info]
- **On-Call Support**: [Contact Info]

---

**For detailed troubleshooting, see [README.md](README.md) and [SETUP_GUIDE.md](SETUP_GUIDE.md)**
