from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path


BASE_DIR = Path(__file__).resolve().parent.parent


def _normalize_database_url(raw_url: str) -> str:
    if raw_url.startswith("postgres://"):
        return raw_url.replace("postgres://", "postgresql+psycopg://", 1)
    if raw_url.startswith("postgresql://"):
        return raw_url.replace("postgresql://", "postgresql+psycopg://", 1)
    return raw_url


def _split_chain_ids(value: str) -> list[int]:
    chain_ids: list[int] = []
    for part in value.split(","):
        candidate = part.strip()
        if candidate.isdigit():
            chain_ids.append(int(candidate))
    return chain_ids or [1, 11155111, 137]


@dataclass(slots=True)
class Settings:
    app_name: str = "Crypto Hockey"
    environment: str = os.getenv("APP_ENV", "development")
    database_url: str = field(
        default_factory=lambda: _normalize_database_url(
            os.getenv("DATABASE_URL", f"sqlite:///{BASE_DIR / 'crypto_hockey.db'}")
        )
    )
    arcade1870_contract_address: str = os.getenv(
        "ARCADE1870_CONTRACT_ADDRESS",
        "0x8eddD4edea39c5B5f77662453600F53A202EE47C",
    )
    reward_vault_address: str = os.getenv(
        "REWARD_VAULT_ADDRESS",
        "0x1e4f6e4a382adbdb662733a19ae773d3ab8f497d",
    )
    reward_issuer_url: str = os.getenv("REWARD_ISSUER_URL", "")
    reward_amount: float = float(os.getenv("REWARD_AMOUNT", "10"))
    default_network_chain_id: int = int(os.getenv("DEFAULT_NETWORK_CHAIN_ID", "1"))
    supported_chain_ids: list[int] = field(
        default_factory=lambda: _split_chain_ids(os.getenv("SUPPORTED_CHAIN_IDS", "1,11155111,137"))
    )


settings = Settings()
