from __future__ import annotations

from datetime import datetime
from datetime import timedelta
from decimal import Decimal
from pathlib import Path
from threading import RLock
from uuid import uuid4

import httpx
from eth_account import Account
from eth_account.messages import encode_defunct
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy import select
from sqlalchemy.orm import Session

from app.config import BASE_DIR, settings
from app.database import Base, engine, get_db
from app.models import GameSession, PlayerProfile
from app.online_arena import online_arena
from app.schemas import (
    ChallengeRequest,
    ClaimRewardRequest,
    ClaimRewardResponse,
    CompleteGameSessionRequest,
    CreateGameSessionRequest,
    CreateOnlineMatchRequest,
    JoinOnlineMatchRequest,
    ReportWinnerRequest,
    RewardGameProof,
    SyncPlayerRequest,
    UpsertPlayerRequest,
)


app = FastAPI(title=settings.app_name)
templates = Jinja2Templates(directory=str(BASE_DIR / "templates"))
app.mount("/static", StaticFiles(directory=str(BASE_DIR / "wwwroot")), name="static")
Base.metadata.create_all(bind=engine)
challenge_store: dict[str, dict] = {}
challenge_store_lock = RLock()
ALLOWED_CHALLENGE_ACTIONS = {"complete_session", "claim_reward"}


@app.on_event("startup")
def startup() -> None:
    Base.metadata.create_all(bind=engine)


def _normalize_wallet(wallet_address: str) -> str:
    wallet = wallet_address.strip()
    if not wallet:
        raise HTTPException(status_code=400, detail="Wallet address is required.")
    return wallet


def _player_to_dict(player: PlayerProfile) -> dict:
    return {
        "id": player.id,
        "wallet_address": player.wallet_address,
        "total_games": player.total_games,
        "total_wins": player.total_wins,
        "total_losses": player.total_losses,
        "total_rewards_earned": float(player.total_rewards_earned),
        "created_at": player.created_at.isoformat(),
        "last_played_at": player.last_played_at.isoformat(),
        "win_rate": player.win_rate,
    }


def _session_to_dict(session: GameSession) -> dict:
    return {
        "id": session.id,
        "player_address": session.player_address,
        "player_score": session.player_score,
        "opponent_score": session.opponent_score,
        "difficulty_level": session.difficulty_level,
        "player_won": session.player_won,
        "started_at": session.started_at.isoformat(),
        "ended_at": session.ended_at.isoformat() if session.ended_at else None,
        "reward_amount": float(session.reward_amount),
        "transaction_hash": session.transaction_hash,
        "reward_claimed": session.reward_claimed,
    }


def _get_or_create_player(db: Session, wallet_address: str) -> PlayerProfile:
    wallet = _normalize_wallet(wallet_address)
    player = db.scalar(select(PlayerProfile).where(PlayerProfile.wallet_address == wallet))
    if player is None:
        now = datetime.utcnow()
        player = PlayerProfile(
            wallet_address=wallet,
            total_games=0,
            total_wins=0,
            total_losses=0,
            total_rewards_earned=Decimal("0"),
            created_at=now,
            last_played_at=now,
        )
        db.add(player)
        db.commit()
        db.refresh(player)
    return player


async def _request_reward_claim(wallet_address: str, proof: RewardGameProof) -> tuple[bool, str | None, str | None]:
    if not settings.reward_issuer_url:
        if settings.environment.lower() != "production":
            return True, None, None
        return False, "Reward issuer URL is not configured.", None

    payload = {
        "recipient": wallet_address,
        "game": {
            "gameId": proof.game_id,
            "mode": proof.mode,
            "playerScore": proof.player_score,
            "opponentScore": proof.opponent_score,
            "difficultyLevel": proof.difficulty_level,
            "completedAt": proof.completed_at.isoformat() if proof.completed_at else None,
            "playerWon": proof.player_won,
        },
    }

    try:
        async with httpx.AsyncClient(timeout=20) as client:
            response = await client.post(settings.reward_issuer_url, json=payload)
    except httpx.HTTPError as exc:
        return False, str(exc), None

    if response.status_code >= 400:
        return False, response.text or f"Issuer error {response.status_code}.", None

    try:
        response_payload = response.json() if response.content else {}
    except ValueError:
        return False, "Reward issuer returned invalid JSON.", None
    nonce = response_payload.get("nonce")
    return True, None, nonce


def _cleanup_challenges() -> None:
    with challenge_store_lock:
        now = datetime.utcnow()
        expired = [challenge_id for challenge_id, challenge in challenge_store.items() if challenge["expires_at"] <= now]
        for challenge_id in expired:
            challenge_store.pop(challenge_id, None)


def _issue_challenge(
    wallet_address: str,
    action: str,
    session_id: int,
    player_score: int | None = None,
    opponent_score: int | None = None,
) -> dict:
    _cleanup_challenges()
    if action not in ALLOWED_CHALLENGE_ACTIONS:
        raise HTTPException(status_code=400, detail="Unsupported challenge action.")
    challenge_id = uuid4().hex
    nonce = uuid4().hex
    issued_at = datetime.utcnow()
    lines = [
        "Crypto Hockey authorization",
        f"Action: {action}",
        f"Wallet: {wallet_address}",
        f"Session: {session_id}",
    ]
    if player_score is not None and opponent_score is not None:
        lines.append(f"PlayerScore: {player_score}")
        lines.append(f"OpponentScore: {opponent_score}")
    lines.append(f"Nonce: {nonce}")
    lines.append(f"IssuedAt: {issued_at.isoformat()}Z")
    message = "\n".join(lines)
    with challenge_store_lock:
        challenge_store[challenge_id] = {
            "wallet_address": wallet_address,
            "action": action,
            "session_id": session_id,
            "player_score": player_score,
            "opponent_score": opponent_score,
            "message": message,
            "expires_at": issued_at + timedelta(minutes=5),
            "used": False,
        }
    return {
        "challenge_id": challenge_id,
        "message": message,
        "expires_at": (issued_at + timedelta(minutes=5)).isoformat() + "Z",
    }


def _verify_challenge(
    *,
    challenge_id: str,
    signature: str,
    wallet_address: str,
    action: str,
    session_id: int,
    player_score: int | None = None,
    opponent_score: int | None = None,
) -> None:
    _cleanup_challenges()
    with challenge_store_lock:
        challenge = challenge_store.get(challenge_id)
        if challenge is None or challenge["used"]:
            raise HTTPException(status_code=400, detail="Challenge is invalid or already used.")
        if challenge["wallet_address"].lower() != wallet_address.lower():
            raise HTTPException(status_code=403, detail="Challenge wallet does not match the session owner.")
        if challenge["action"] != action or challenge["session_id"] != session_id:
            raise HTTPException(status_code=400, detail="Challenge does not match this request.")
        if challenge["player_score"] != player_score or challenge["opponent_score"] != opponent_score:
            raise HTTPException(status_code=400, detail="Challenge does not match the submitted score.")

        recovered_wallet = Account.recover_message(
            encode_defunct(text=challenge["message"]),
            signature=signature,
        )
        if recovered_wallet.lower() != wallet_address.lower():
            raise HTTPException(status_code=403, detail="Signature does not match the session owner.")

        challenge["used"] = True


@app.get("/", response_class=HTMLResponse)
def home(request: Request) -> HTMLResponse:
    return templates.TemplateResponse("index.html", {"request": request, "page_name": "home"})


@app.get("/game", response_class=HTMLResponse)
def game(request: Request) -> HTMLResponse:
    return templates.TemplateResponse(
        "game.html",
        {
            "request": request,
            "page_name": "game",
            "reward_amount": settings.reward_amount,
        },
    )


@app.get("/leaderboard", response_class=HTMLResponse)
def leaderboard_page(request: Request, db: Session = Depends(get_db)) -> HTMLResponse:
    players = db.scalars(
        select(PlayerProfile)
        .order_by(PlayerProfile.total_wins.desc(), PlayerProfile.total_rewards_earned.desc())
        .limit(50)
    ).all()
    return templates.TemplateResponse(
        "leaderboard.html",
        {
            "request": request,
            "page_name": "leaderboard",
            "leaderboard": [_player_to_dict(player) for player in players],
        },
    )


@app.get("/online", response_class=HTMLResponse)
def online_page(request: Request) -> HTMLResponse:
    return templates.TemplateResponse(
        "online.html",
        {
            "request": request,
            "page_name": "online",
            "initial_lobby": online_arena.get_lobby_snapshot(),
        },
    )


@app.get("/healthz")
def healthz() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/auth/challenge")
def create_auth_challenge(payload: ChallengeRequest, db: Session = Depends(get_db)) -> dict:
    wallet = _normalize_wallet(payload.wallet_address)
    session = db.get(GameSession, payload.session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Game session not found.")
    if session.player_address.lower() != wallet.lower():
        raise HTTPException(status_code=403, detail="Wallet does not own this session.")
    return _issue_challenge(
        wallet_address=wallet,
        action=payload.action,
        session_id=payload.session_id,
        player_score=payload.player_score,
        opponent_score=payload.opponent_score,
    )


@app.get("/api/players/{wallet_address}")
def get_player(wallet_address: str, db: Session = Depends(get_db)) -> dict:
    wallet = _normalize_wallet(wallet_address)
    player = db.scalar(select(PlayerProfile).where(PlayerProfile.wallet_address == wallet))
    if player is None:
        raise HTTPException(status_code=404, detail="Player not found.")
    return _player_to_dict(player)


@app.post("/api/players")
def sync_player(payload: SyncPlayerRequest, db: Session = Depends(get_db)) -> dict:
    player = _get_or_create_player(db, payload.wallet_address)
    return _player_to_dict(player)


@app.get("/api/leaderboard")
def get_leaderboard(limit: int = 50, db: Session = Depends(get_db)) -> list[dict]:
    safe_limit = max(1, min(limit, 100))
    players = db.scalars(
        select(PlayerProfile)
        .order_by(PlayerProfile.total_wins.desc(), PlayerProfile.total_rewards_earned.desc())
        .limit(safe_limit)
    ).all()
    return [_player_to_dict(player) for player in players]


@app.post("/api/game-sessions")
def create_game_session(payload: CreateGameSessionRequest, db: Session = Depends(get_db)) -> dict:
    wallet = _normalize_wallet(payload.player_address)
    _get_or_create_player(db, wallet)
    session = GameSession(
        player_address=wallet,
        difficulty_level=payload.difficulty_level,
        started_at=datetime.utcnow(),
        player_score=0,
        opponent_score=0,
        reward_claimed=False,
    )
    db.add(session)
    db.commit()
    db.refresh(session)
    return _session_to_dict(session)


@app.post("/api/game-sessions/{session_id}/complete")
def complete_game_session(session_id: int, payload: CompleteGameSessionRequest, db: Session = Depends(get_db)) -> dict:
    session = db.get(GameSession, session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Game session not found.")
    if session.ended_at is not None:
        return _session_to_dict(session)
    _verify_challenge(
        challenge_id=payload.challenge_id,
        signature=payload.signature,
        wallet_address=session.player_address,
        action="complete_session",
        session_id=session.id,
        player_score=payload.player_score,
        opponent_score=payload.opponent_score,
    )

    session.ended_at = datetime.utcnow()
    session.player_score = payload.player_score
    session.opponent_score = payload.opponent_score
    session.player_won = payload.player_score > payload.opponent_score
    session.reward_amount = Decimal(str(settings.reward_amount if session.player_won else 0))

    player = _get_or_create_player(db, session.player_address)
    player.total_games += 1
    player.last_played_at = datetime.utcnow()
    if session.player_won:
        player.total_wins += 1
        player.total_rewards_earned += Decimal(str(settings.reward_amount))
    else:
        player.total_losses += 1

    db.add_all([session, player])
    db.commit()
    db.refresh(session)
    return _session_to_dict(session)


@app.post("/api/game-sessions/{session_id}/claim", response_model=ClaimRewardResponse)
async def claim_reward(session_id: int, payload: ClaimRewardRequest, db: Session = Depends(get_db)) -> ClaimRewardResponse:
    session = db.get(GameSession, session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Game session not found.")
    if session.ended_at is None:
        return ClaimRewardResponse(success=False, error_message="Reward cannot be claimed before the session is complete.")
    if session.reward_claimed or not session.player_won:
        return ClaimRewardResponse(success=False, error_message="Reward cannot be claimed for this session.")
    _verify_challenge(
        challenge_id=payload.challenge_id,
        signature=payload.signature,
        wallet_address=session.player_address,
        action="claim_reward",
        session_id=session.id,
    )

    proof = RewardGameProof(
        game_id=str(session.id),
        player_score=session.player_score,
        opponent_score=session.opponent_score,
        difficulty_level=session.difficulty_level,
        completed_at=session.ended_at,
        player_won=session.player_won,
    )
    success, error_message, nonce = await _request_reward_claim(session.player_address, proof)
    if success:
        session.reward_claimed = True
        session.transaction_hash = f"issuer-claim:{nonce}" if nonce else session.transaction_hash
        db.add(session)
        db.commit()
    return ClaimRewardResponse(success=success, error_message=error_message, nonce=nonce)


@app.post("/api/online/players")
def upsert_online_player(payload: UpsertPlayerRequest) -> dict:
    try:
        return online_arena.upsert_player(payload.wallet_address, payload.display_name)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.get("/api/online/lobby")
def get_online_lobby() -> dict:
    return online_arena.get_lobby_snapshot()


@app.post("/api/online/heads-up")
def create_heads_up(payload: CreateOnlineMatchRequest) -> dict:
    try:
        return online_arena.create_heads_up_match(payload.wallet_address, payload.display_name)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/api/online/heads-up/{match_id}/join")
def join_heads_up(match_id: str, payload: JoinOnlineMatchRequest) -> dict:
    try:
        return online_arena.join_heads_up_match(match_id, payload.wallet_address, payload.display_name)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/api/online/heads-up/{match_id}/report")
def report_heads_up_winner(match_id: str, payload: ReportWinnerRequest) -> dict:
    try:
        return online_arena.report_heads_up_winner(match_id, payload.winner_wallet_address, payload.reporter_wallet_address)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/api/online/tournaments")
def create_tournament(payload: CreateOnlineMatchRequest) -> dict:
    try:
        return online_arena.create_tournament(payload.wallet_address, payload.display_name)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/api/online/tournaments/{tournament_id}/join")
def join_tournament(tournament_id: str, payload: JoinOnlineMatchRequest) -> dict:
    try:
        return online_arena.join_tournament(tournament_id, payload.wallet_address, payload.display_name)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/api/online/tournaments/{tournament_id}/matches/{match_id}/report")
def report_tournament_winner(tournament_id: str, match_id: str, payload: ReportWinnerRequest) -> dict:
    try:
        return online_arena.report_tournament_winner(
            tournament_id,
            match_id,
            payload.winner_wallet_address,
            payload.reporter_wallet_address,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
