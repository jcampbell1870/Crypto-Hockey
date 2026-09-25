# Crypto Hockey Deployment Guide

## Render

Crypto Hockey now deploys as a Python 3 FastAPI service.

### Automatic Deployment

Use the root `/home/runner/work/Crypto-Hockey/Crypto-Hockey/render.yaml`.

It configures:

- runtime: Python
- build command: `pip install -r requirements.txt`
- start command: `uvicorn app.main:app --host 0.0.0.0 --port $PORT`
- health check: `/healthz`

### Manual Render Service Settings

- **Environment**: Python 3
- **Build Command**: `pip install -r requirements.txt`
- **Start Command**: `uvicorn app.main:app --host 0.0.0.0 --port $PORT`

### Required / Recommended Environment Variables

```bash
APP_ENV=production
DATABASE_URL=sqlite:///./crypto_hockey.db
REWARD_ISSUER_URL=https://crypto-chess.onrender.com/api/reward-claim
REWARD_AMOUNT=10
DEFAULT_NETWORK_CHAIN_ID=1
SUPPORTED_CHAIN_IDS=1,11155111,137
ARCADE1870_CONTRACT_ADDRESS=0x8eddD4edea39c5B5f77662453600F53A202EE47C
REWARD_VAULT_ADDRESS=0x1e4f6e4a382adbdb662733a19ae773d3ab8f497d
```

### Health Check

- `GET /healthz`

### Common Failure Modes

- If Render uses Node or Python build defaults unrelated to this repo, the service was created with the wrong runtime settings.
- If MetaMask is unavailable in the browser, wallet-dependent actions remain disabled.
- If `REWARD_ISSUER_URL` is invalid, reward claims fail but gameplay and leaderboard updates still work.
- If a SQLite database is used on ephemeral infrastructure, stats reset when the disk is replaced; use a persistent database URL for durable storage.

## Local Smoke Test

```bash
cd /home/runner/work/Crypto-Hockey/Crypto-Hockey
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload
```
