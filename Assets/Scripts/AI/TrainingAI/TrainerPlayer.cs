using System.Collections.Generic;
using UnityEngine;

public class TrainerPlayer
{
    public enum TrainingStrategyType{ Strategy1, Strategy2, Strategy3, Strategy4 }

    private readonly MapGenerator _mapGenerator;
    private readonly EnemyPool _enemyPool;
    private readonly Inventory _inventory;
    private readonly int _playerId;
    private readonly TrainingStrategyType _strategy;

    private int _towersBuilt = 0;

    private const int UnitCost = 5;
    private const int TowerCost = 15;

    public TrainerPlayer(MapGenerator mapGenerator, EnemyPool enemyPool, Inventory inventory, int playerId, TrainingStrategyType strategy)
    {
        _mapGenerator = mapGenerator;
        _enemyPool = enemyPool;
        _inventory = inventory;
        _playerId = playerId;
        _strategy = strategy;
    }

    public void Tick()
    {
        switch (_strategy)
        {
            case TrainingStrategyType.Strategy1:
                Strategy1(); // We spawn new unit ehen we can do it
                break;

            case TrainingStrategyType.Strategy2:
                Strategy2(); // We spawn 3 units at once 
                break;

            case TrainingStrategyType.Strategy3:
                Strategy3(1); // We build 1 tower and then spawn 3 units at once 
                break;

            case TrainingStrategyType.Strategy4:
                Strategy3(2); // We build 2 tower and then spawn 3 units at once
                break;
        }
    }

    private void Strategy1()
    {
        if (_inventory.TrySpendGold(UnitCost)) SpawnUnit();
    }

    private void Strategy2()
    {
        int waveCost = UnitCost * 3;
        if (_inventory.TrySpendGold(waveCost)) SpawnX3Units();
    }

    private void Strategy3(int requiredTowerCount)
    {
        if (_towersBuilt < requiredTowerCount)
        {
            if (_inventory.TrySpendGold(TowerCost))
            {
                if (TryBuildTowerNearCastle()) _towersBuilt++; 
                else _inventory.Gold += TowerCost; 
            }
            return;
        }

        Strategy2();
    }

    private void SpawnUnit()
    {
        _enemyPool.SpawnUnit(_playerId, UnitType.Swordsman);
    }

    private void SpawnX3Units()
    {
        for (int i = 0; i < 3; i++) SpawnUnit();
    }

    private bool TryBuildTowerNearCastle() 
    // Логіка з башнами досить складна, я пропоную не заморачуватись і просто ставити поруч з замком
    // бо ворожі юніти всеодно туди прийдуть, і це всеодно лише допоміжний елемент для тренування моделі
    {
        int enemyPlayerId = 1 - _playerId;
        List<Vector2Int> positions = _mapGenerator.GetTowerPosBtwPlayers(_playerId, enemyPlayerId);
        if (positions == null || positions.Count == 0) return false;
        Vector2Int castle = _mapGenerator.GetCastlePosition(_playerId);
        Vector2Int? bestPosition = null;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int position in positions)
        {
            if (!_mapGenerator.IsValidTowerPlacement(position, _playerId)) continue;

            int distance = ManhattanDistance(position, castle);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPosition = position;
            }
        }

        if (!bestPosition.HasValue) return false;
        BuildTower(bestPosition.Value);
        return true;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void BuildTower(Vector2Int position)
    {
        _mapGenerator.PlaceTowerServerRpc(position, _playerId);
        Debug.Log($"Training opponent {_playerId} builds tower at {position}");
    }
}
