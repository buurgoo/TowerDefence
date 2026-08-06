using System;
using System.Collections.Generic;

[Serializable]
public class GridPosition
{
    public int x;
    public int y;

    public GridPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

[Serializable]
public class AiGameState
{
    public string map_id;
    public int player_id;
    public int coins;
    public List<GridPosition> available_tower_positions;
    public bool enemy_units;
    public int enemy_unit_count;
    public bool game_finished;
    public int winner_player_id;
}

[Serializable]
public class AiApiResponse
{
    public bool accepted;
    public string action;
}

/*
class GridPosition(BaseModel):
    x: int
    y: int


class GameState(BaseModel):
    map_id: str
    player_id: int = Field(default=1, ge=0, le=1)
    coins: int = Field(ge=0)
    available_tower_positions: list[GridPosition]
    enemy_units: list[list[float] | None] # [(2.4456, 4, 1.222), (1.444, 1.23, 0), (3.3443, 1, 2), None, None, ..., None]
    game_finished: bool
    winner_player_id: int = Field(default=-1, ge=-1, le=1)
*/