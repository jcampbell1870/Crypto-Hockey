from __future__ import annotations

from datetime import datetime
from decimal import Decimal

from sqlalchemy import Boolean, DateTime, Integer, Numeric, String
from sqlalchemy.orm import Mapped, mapped_column

from app.database import Base


class PlayerProfile(Base):
    __tablename__ = "player_profiles"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    wallet_address: Mapped[str] = mapped_column(String(64), unique=True, index=True)
    total_games: Mapped[int] = mapped_column(Integer, default=0)
    total_wins: Mapped[int] = mapped_column(Integer, default=0)
    total_losses: Mapped[int] = mapped_column(Integer, default=0)
    total_rewards_earned: Mapped[Decimal] = mapped_column(Numeric(18, 8), default=Decimal("0"))
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    last_played_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    @property
    def win_rate(self) -> float:
        if self.total_games == 0:
            return 0.0
        return round((self.total_wins / self.total_games) * 100, 1)


class GameSession(Base):
    __tablename__ = "game_sessions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    player_address: Mapped[str] = mapped_column(String(64), index=True)
    player_score: Mapped[int] = mapped_column(Integer, default=0)
    opponent_score: Mapped[int] = mapped_column(Integer, default=0)
    difficulty_level: Mapped[str] = mapped_column(String(32), default="Medium")
    player_won: Mapped[bool] = mapped_column(Boolean, default=False)
    started_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    ended_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)
    reward_amount: Mapped[Decimal] = mapped_column(Numeric(18, 8), default=Decimal("0"))
    transaction_hash: Mapped[str | None] = mapped_column(String(256), nullable=True)
    reward_claimed: Mapped[bool] = mapped_column(Boolean, default=False)
