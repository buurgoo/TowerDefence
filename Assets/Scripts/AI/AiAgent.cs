//using System.Collections.Generic;
//using UnityEngine;

//public class AiAgent
//{
//    private readonly MapGenerator _mapGenerator;
//    private readonly int _aiPlayerId;

//    public AiAgent(MapGenerator mapGenerator, int aiPlayerId)
//    {
//        _mapGenerator = mapGenerator;
//        _aiPlayerId = aiPlayerId;
//    }

//    public AiDecision ChooseAction(bool hasSwordsman, bool hasArcher, IReadOnlyList<Vector2Int> availableTowerPositions) // IReadOnlyList<Vector2Int> availableTowerPositions for tower for defence
//    {
//        // TODO: Implement AI logic to choose between attacking or defending based on the game state 
//        AiActionType modelAction = ChooseActionStub();

//        switch (modelAction)
//        {
//            case AiActionType.Attack: return ChooseAttack(hasSwordsman, hasArcher);
//            case AiActionType.Defend: return ChooseDefense(availableTowerPositions);
//            default: return AiDecision.Wait();
//        }
//    }

//    private AIDecision ChooseAttack(bool hasSwordsman, bool hasArcher)
//    {
//        int targetPlayerId = 1 - _aiPlayerId; 

//        Vector2Int aiCastlePosition = _mapGenerator.GetCastlePosition(_aiPlayerId);
//        Vector2Int targetCastlePosition = _mapGenerator.GetCastlePosition(targetPlayerId);

//        List<Vector2Int> path = _mapGenerator.FindPath(aiCastlePosition, targetCastlePosition);
//        if (path.Count == 0) return AiDecision.Wait();

//        UnitType unitType = ChooseUnitToAttack(hasSwordsman, hasArcher);
//        if (unitType == UnitType.None) return AiDecision.Wait();
        
//        return new AiDecision.Attack(targetPlayerId, unitType);
//    }

//    private AiDecision ChooseDefense(IReadOnlyList<Vector2Int> availableTowerPositions)
//    {
//        Vector2Int? towerPosition = ChooseTowerPositionStub(availableTowerPositions);

//        if (!towerPosition.HasValue) return AiDecision.Wait();
//        return AiDecision.Defend(towerPosition.Value);
//    }

//    private AiActionType ChooseActionStub()
//    {
//        // TODO: we choose action based on the game state 
//        // for now we say that AI always choose to attack
//        return AiActionType.Attack;
//    }

//    private UnitType ChooseUnitToAttack(bool hasSwordsman, bool hasArcher)
//    {
//        // TODO: Imlement and call AI agent to choose swordsman archer or none
//        UnitType modelChoice = UnitType.Swordsman; // temperary
  
//        if (modelChoice == UnitType.Swordsman && hasSwordsman) return UnitType.Swordsman;
//        if (modelChoice == UnitType.Archer && hasArcher) return UnitType.Archer;
        
//        return UnitType.None;
//    }

//    private Vector2Int? ChooseTowerPositionStub(IReadOnlyList<Vector2Int> availableTowerPositions)
//    {
//        // TODO: Implement and call AI agent to choose tower position for defence

//        if (availableTowerPositions == null || availableTowerPositions.Count == 0) return null;
//        return availableTowerPositions[0];
//    }
//}
