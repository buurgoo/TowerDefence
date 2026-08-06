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
public class EnemyUnitPosition
{
    public float x;
    public float y;
    public bool exists;

    public EnemyUnitPosition(float x, float y, bool exists)
    {
        this.x = x;
        this.y = y;
        this.exists = exists;
    }
}

[Serializable]
public class AiGameState
{
   public string map_id;
    public int player_id;
    public int coins;
    public List<GridPosition> available_tower_positions;
    public List<EnemyUnitPosition> enemy_units;
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
*/