from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from threading import Lock

from fastapi import FastAPI
from pydantic import BaseModel, Field


DATA_FILE = Path("collected_game_states.jsonl")
write_lock = Lock()

app = FastAPI(
    title="Tower Defence AI API",
    version="0.1.0",
)


class GridPosition(BaseModel):
    x: int
    y: int


class EnemyUnitPosition(BaseModel):
    x: float
    y: float
    exists: bool


class GameState(BaseModel):
    map_id: str
    player_id: int = Field(default=1, ge=0, le=1)
    coins: int = Field(ge=0)
    available_tower_positions: list[GridPosition]
    enemy_units: list[EnemyUnitPosition]
    game_finished: bool
    winner_player_id: int = Field(default=-1, ge=-1, le=1)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/game-state")
def receive_game_state(state: GameState) -> dict[str, object]:
    record = {
        "received_at": datetime.now(timezone.utc).isoformat(),
        **state.model_dump(),
    }

    with write_lock:
        with DATA_FILE.open("a", encoding="utf-8") as file:
            file.write(json.dumps(record, ensure_ascii=False) + "\n")

    enemy_count = sum(1 for unit in state.enemy_units if unit.exists)
    print(
        f"Received state: map={state.map_id}, "
        f"coins={state.coins}, "
        f"enemies={enemy_count}, "
        f"finished={state.game_finished}"
    )

    return { "accepted": True, "action": "wait" } # temp 
