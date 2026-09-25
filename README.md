# 🏒 Crypto Hockey

Crypto Hockey is a Python 3 / FastAPI web game that keeps the original air-hockey gameplay, MetaMask wallet flow, reward-claim integration, leaderboard tracking, and online arena experience.

## Current Stack

- Python 3.11
- FastAPI
- SQLAlchemy
- Jinja templates
- Browser-side JavaScript canvas game loop
- MetaMask browser integration

## Features

- Single-player air hockey vs AI with Easy / Medium / Hard difficulty
- MetaMask wallet connect, network switch, and copy-address support
- Persistent player profiles and leaderboard stats
- Reward-claim requests against the configured issuer URL
- Online heads-up tables and 8-player tournament lobby
- Render deployment via Python web service

## Local Development

### Prerequisites

- Python 3.11+
- pip
- MetaMask browser extension

### Install

```bash
cd /home/runner/work/Crypto-Hockey/Crypto-Hockey
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

### Run

```bash
uvicorn app.main:app --reload
```

Open:

- http://127.0.0.1:8000/
- http://127.0.0.1:8000/game
- http://127.0.0.1:8000/leaderboard
- http://127.0.0.1:8000/online

## Configuration

Environment variables:

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

If `REWARD_ISSUER_URL` is unset, reward claims fall back to offline success so gameplay is still testable.

## Render Deployment

This repository now deploys as a **Python** web service.

`render.yaml`:

- installs dependencies with `pip install -r requirements.txt`
- starts the app with `uvicorn app.main:app --host 0.0.0.0 --port $PORT`

If creating the service manually in Render:

- Runtime: **Python 3**
- Build Command: `pip install -r requirements.txt`
- Start Command: `uvicorn app.main:app --host 0.0.0.0 --port $PORT`

Health check:

- `/healthz`

## Notes

- Online arena state is in-memory and resets when the app restarts.
- Static assets remain under `/home/runner/work/Crypto-Hockey/Crypto-Hockey/wwwroot`.
- Legacy .NET files are still present in the repo history/worktree, but the active web app runtime is Python.
