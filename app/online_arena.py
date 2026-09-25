from __future__ import annotations

from copy import deepcopy
from datetime import datetime
from threading import RLock
from uuid import uuid4


def _short_wallet(wallet_address: str) -> str:
    wallet_address = wallet_address.strip()
    return wallet_address if len(wallet_address) < 12 else f"{wallet_address[:6]}...{wallet_address[-4:]}"


def _normalize_wallet(wallet_address: str) -> str:
    wallet = wallet_address.strip()
    if not wallet:
        raise ValueError("A connected wallet is required for online play.")
    return wallet


def _normalize_name(display_name: str, wallet_address: str) -> str:
    trimmed = display_name.strip()
    if trimmed:
        return trimmed[:24]
    return _short_wallet(wallet_address)


class OnlineArenaService:
    def __init__(self) -> None:
        self._sync = RLock()
        self._players: dict[str, dict] = {}
        self._heads_up_matches: dict[str, dict] = {}
        self._tournaments: dict[str, dict] = {}

    def upsert_player(self, wallet_address: str, display_name: str) -> dict:
        with self._sync:
            wallet = _normalize_wallet(wallet_address)
            alias = self._players.get(wallet) or {"wallet_address": wallet}
            alias["display_name"] = _normalize_name(display_name, wallet)
            alias["last_seen_at"] = datetime.utcnow().isoformat()
            self._players[wallet] = alias
            return deepcopy(alias)

    def get_lobby_snapshot(self) -> dict:
        with self._sync:
            heads_up_matches = sorted(
                self._heads_up_matches.values(),
                key=lambda item: item["updated_at"],
                reverse=True,
            )[:20]
            tournaments = sorted(
                self._tournaments.values(),
                key=lambda item: item["updated_at"],
                reverse=True,
            )[:10]
            return {
                "heads_up_matches": deepcopy(heads_up_matches),
                "tournaments": deepcopy(tournaments),
            }

    def create_heads_up_match(self, wallet_address: str, display_name: str) -> dict:
        with self._sync:
            alias = self.upsert_player(wallet_address, display_name)
            now = datetime.utcnow().isoformat()
            match = {
                "id": f"hu-{uuid4().hex[:8]}",
                "host_wallet_address": alias["wallet_address"],
                "host_name": alias["display_name"],
                "challenger_wallet_address": None,
                "challenger_name": None,
                "winner_wallet_address": None,
                "winner_name": None,
                "status": "WaitingForOpponent",
                "created_at": now,
                "updated_at": now,
            }
            self._heads_up_matches[match["id"]] = match
            return deepcopy(match)

    def join_heads_up_match(self, match_id: str, wallet_address: str, display_name: str) -> dict:
        with self._sync:
            match = self._heads_up_matches.get(match_id)
            if match is None:
                raise ValueError("Heads-up table not found.")
            if match["status"] != "WaitingForOpponent":
                raise ValueError("This heads-up table is no longer open.")

            alias = self.upsert_player(wallet_address, display_name)
            if alias["wallet_address"].lower() == match["host_wallet_address"].lower():
                raise ValueError("You cannot join your own heads-up table.")

            match["challenger_wallet_address"] = alias["wallet_address"]
            match["challenger_name"] = alias["display_name"]
            match["status"] = "InProgress"
            match["updated_at"] = datetime.utcnow().isoformat()
            return deepcopy(match)

    def report_heads_up_winner(self, match_id: str, winner_wallet_address: str, reporter_wallet_address: str) -> dict:
        with self._sync:
            match = self._heads_up_matches.get(match_id)
            if match is None:
                raise ValueError("Heads-up table not found.")
            if match["status"] != "InProgress":
                raise ValueError("Winner can only be reported for active heads-up tables.")

            reporter = _normalize_wallet(reporter_wallet_address).lower()
            participants = {
                match["host_wallet_address"].lower(),
                (match["challenger_wallet_address"] or "").lower(),
            }
            if reporter not in participants:
                raise ValueError("Only seated players can report a heads-up result.")

            winner = _normalize_wallet(winner_wallet_address)
            if winner.lower() not in participants:
                raise ValueError("Winner must be one of the seated players.")

            match["winner_wallet_address"] = winner
            match["winner_name"] = self._players.get(winner, {}).get("display_name", _short_wallet(winner))
            match["status"] = "Completed"
            match["updated_at"] = datetime.utcnow().isoformat()
            return deepcopy(match)

    def create_tournament(self, wallet_address: str, display_name: str) -> dict:
        with self._sync:
            alias = self.upsert_player(wallet_address, display_name)
            now = datetime.utcnow().isoformat()
            tournament = {
                "id": f"trn-{uuid4().hex[:8]}",
                "name": f"{alias['display_name']}'s 8-Max",
                "host_wallet_address": alias["wallet_address"],
                "entrants": [deepcopy(alias)],
                "seat_limit": 8,
                "status": "Registration",
                "champion_wallet_address": None,
                "champion_name": None,
                "rounds": [],
                "created_at": now,
                "updated_at": now,
            }
            self._tournaments[tournament["id"]] = tournament
            return deepcopy(tournament)

    def join_tournament(self, tournament_id: str, wallet_address: str, display_name: str) -> dict:
        with self._sync:
            tournament = self._tournaments.get(tournament_id)
            if tournament is None:
                raise ValueError("Tournament not found.")
            if tournament["status"] != "Registration":
                raise ValueError("This tournament has already started.")

            alias = self.upsert_player(wallet_address, display_name)
            already_joined = any(
                entrant["wallet_address"].lower() == alias["wallet_address"].lower()
                for entrant in tournament["entrants"]
            )
            if already_joined:
                return deepcopy(tournament)
            if len(tournament["entrants"]) >= tournament["seat_limit"]:
                raise ValueError("Tournament is full.")

            tournament["entrants"].append(deepcopy(alias))
            tournament["updated_at"] = datetime.utcnow().isoformat()
            if len(tournament["entrants"]) == tournament["seat_limit"]:
                self._start_tournament(tournament)
            return deepcopy(tournament)

    def report_tournament_winner(
        self,
        tournament_id: str,
        match_id: str,
        winner_wallet_address: str,
        reporter_wallet_address: str,
    ) -> dict:
        with self._sync:
            tournament = self._tournaments.get(tournament_id)
            if tournament is None:
                raise ValueError("Tournament not found.")
            if tournament["status"] != "InProgress":
                raise ValueError("Tournament is not currently in play.")

            match = next(
                (
                    item
                    for round_matches in tournament["rounds"]
                    for item in round_matches
                    if item["id"] == match_id
                ),
                None,
            )
            if match is None:
                raise ValueError("Tournament match not found.")
            if match["is_complete"]:
                raise ValueError("This match is already complete.")

            reporter = _normalize_wallet(reporter_wallet_address).lower()
            participants = {
                (match["player_one_wallet_address"] or "").lower(),
                (match["player_two_wallet_address"] or "").lower(),
            }
            if reporter not in participants:
                raise ValueError("Only seated tournament players can report match results.")

            winner = _normalize_wallet(winner_wallet_address)
            if winner.lower() not in participants:
                raise ValueError("Winner must be seated in the selected tournament match.")

            match["winner_wallet_address"] = winner
            match["winner_name"] = self._players.get(winner, {}).get("display_name", _short_wallet(winner))
            match["is_complete"] = True
            tournament["updated_at"] = datetime.utcnow().isoformat()
            self._advance_tournament(tournament, match["round_number"])
            return deepcopy(tournament)

    def _start_tournament(self, tournament: dict) -> None:
        shuffled = sorted(tournament["entrants"], key=lambda _: uuid4().hex)
        first_round: list[dict] = []
        for index in range(0, len(shuffled), 2):
            first_round.append(
                {
                    "id": f"tm-{uuid4().hex[:8]}",
                    "round_number": 1,
                    "slot": (index // 2) + 1,
                    "player_one_wallet_address": shuffled[index]["wallet_address"],
                    "player_one_name": shuffled[index]["display_name"],
                    "player_two_wallet_address": shuffled[index + 1]["wallet_address"],
                    "player_two_name": shuffled[index + 1]["display_name"],
                    "winner_wallet_address": None,
                    "winner_name": None,
                    "is_complete": False,
                }
            )
        tournament["rounds"] = [first_round]
        tournament["status"] = "InProgress"
        tournament["updated_at"] = datetime.utcnow().isoformat()

    def _advance_tournament(self, tournament: dict, completed_round_number: int) -> None:
        current_round = next(
            (
                round_matches
                for round_matches in tournament["rounds"]
                if round_matches and round_matches[0]["round_number"] == completed_round_number
            ),
            None,
        )
        if current_round is None or any(not match["is_complete"] for match in current_round):
            return

        winners = [match["winner_wallet_address"] for match in current_round if match["winner_wallet_address"]]
        if len(winners) == 1:
            tournament["champion_wallet_address"] = winners[0]
            tournament["champion_name"] = self._players.get(winners[0], {}).get("display_name", _short_wallet(winners[0]))
            tournament["status"] = "Completed"
            return

        next_round_number = completed_round_number + 1
        if any(round_matches and round_matches[0]["round_number"] == next_round_number for round_matches in tournament["rounds"]):
            return

        next_round: list[dict] = []
        for index in range(0, len(winners), 2):
            player_one = winners[index]
            player_two = winners[index + 1]
            next_round.append(
                {
                    "id": f"tm-{uuid4().hex[:8]}",
                    "round_number": next_round_number,
                    "slot": (index // 2) + 1,
                    "player_one_wallet_address": player_one,
                    "player_one_name": self._players[player_one]["display_name"],
                    "player_two_wallet_address": player_two,
                    "player_two_name": self._players[player_two]["display_name"],
                    "winner_wallet_address": None,
                    "winner_name": None,
                    "is_complete": False,
                }
            )
        tournament["rounds"].append(next_round)


online_arena = OnlineArenaService()
