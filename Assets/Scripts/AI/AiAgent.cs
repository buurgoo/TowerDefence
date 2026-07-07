using System.Collections.Generic;
using UnityEngine;

public class AiAgent
{
    private readonly MapGenerator _mapGenerator;
    private readonly int _aiPlayerId;

    public AiAgent(MapGenerator mapGenerator, int aiPlayerId)
    {
        _mapGenerator = mapGenerator;
        _aiPlayerId = aiPlayerId;
    }

    public AttackDecision ChooseAttack(bool hasSwordsman, bool hasArcher)
    {
        int targetPlayerId = 1 - _aiPlayerId; 

        Vector2Int aiCastlePosition = _mapGenerator.GetCastlePosition(_aiPlayerId);
        Vector2Int targetCastlePosition = _mapGenerator.GetCastlePosition(targetPlayerId);

        List<Vector2Int> path = _mapGenerator.FindPath(aiCastlePosition, targetCastlePosition);
        if (path.Count == 0) return AttackDecision.Wait();

        UnitType unitType = ChooseUnitToAttack(hasSwordsman, hasArcher);
        if (unitType == UnitType.None) return AttackDecision.Wait();
        
        return new AttackDecision(targetPlayerId, unitType);
    }

    private UnitType ChooseUnitToAttack(bool hasSwordsman, bool hasArcher)
    {
        // TODO: Imlement and call AI agent to choose swordsman archer or none
        UnitType modelChoice = UnitType.Swordsman; // temperary
  
        if (modelChoice == UnitType.Swordsman && hasSwordsman) return UnitType.Swordsman;
        if (modelChoice == UnitType.Archer && hasArcher) return UnitType.Archer;
        
        return UnitType.None;
    }
}