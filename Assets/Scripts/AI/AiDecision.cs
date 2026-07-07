using System.Collections.Generic;
using UnityEngine;

public enum UnitType { None, Swordsman, Archer } // for future use, if we will empliment different types of units
public enum AiActionType { Wait, Attack, Defend }

public struct AiDecision
{
    public int TargetPlayerId;
    public UnitType UnitType;
    public AiActionType ActionType;
    public Vector2Int? TowerPosition;

    public bool CanAttack => ActionType == AiActionType.Attack && TargetPlayerId != -1 && UnitType != UnitType.None;
    public bool CanDefend => ActionType == AiActionType.Defend && TowerPosition.HasValue;
    
    
    public AiDecision(AiActionType actionType, int targetPlayerId, UnitType unitType, Vector2Int? towerPosition)
    {
        TargetPlayerId = targetPlayerId;
        UnitType = unitType;
        ActionType = actionType;
        TowerPosition = towerPosition;
    }

    public static AiDecision Wait()
    {
        return new AiDecision(AiActionType.Wait, -1, UnitType.None, null);
    }

    public static AiDecision Attack(int targetPlayerId, UnitType unitType)
    {
        return new AiDecision(AiActionType.Attack, targetPlayerId, unitType, null);
    }

    public static AiDecision Defend(Vector2Int towerPosition)
    {
        return new AiDecision(AiActionType.Defend, -1, UnitType.None, towerPosition);
    }
}
