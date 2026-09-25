from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, Field


class CreateGameSessionRequest(BaseModel):
    player_address: str = Field(min_length=1)
    difficulty_level: str = "Medium"


class CompleteGameSessionRequest(BaseModel):
    player_score: int = Field(ge=0)
    opponent_score: int = Field(ge=0)
    challenge_id: str = Field(min_length=1)
    signature: str = Field(min_length=1)


class ClaimRewardResponse(BaseModel):
    success: bool
    error_message: str | None = None
    nonce: str | None = None


class ClaimRewardRequest(BaseModel):
    challenge_id: str = Field(min_length=1)
    signature: str = Field(min_length=1)


class UpsertPlayerRequest(BaseModel):
    wallet_address: str = Field(min_length=1)
    display_name: str = ""


class SyncPlayerRequest(BaseModel):
    wallet_address: str = Field(min_length=1)


class ChallengeRequest(BaseModel):
    wallet_address: str = Field(min_length=1)
    action: str = Field(min_length=1)
    session_id: int = Field(gt=0)
    player_score: int | None = Field(default=None, ge=0)
    opponent_score: int | None = Field(default=None, ge=0)


class CreateOnlineMatchRequest(BaseModel):
    wallet_address: str = Field(min_length=1)
    display_name: str = ""


class JoinOnlineMatchRequest(BaseModel):
    wallet_address: str = Field(min_length=1)
    display_name: str = ""


class ReportWinnerRequest(BaseModel):
    winner_wallet_address: str = Field(min_length=1)
    reporter_wallet_address: str = Field(min_length=1)


class RewardGameProof(BaseModel):
    game_id: str
    mode: str = "hockey"
    player_score: int
    opponent_score: int
    difficulty_level: str
    completed_at: datetime | None
    player_won: bool
