using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class AiDecisionLogic : NetworkBehaviour
{
    [SerializeField] public MapGenerator mapGenerator;
    [SerializeField] public EnemyPool enemyPool;
    [SerializeField] private float aiTickInterval = 1f;
    private int aiPlayerId = 1;

    private int Gold = 0;
    [SerializeField] public float goldGenerationInterval = 1f;
    [SerializeField] public int goldPerTick = 1;

    private Coroutine _tickCoroutine;

    public void InitializeAiAgent()
    {
        if (!IsServer) return;

        _tickCoroutine = StartCoroutine(GenerateGoldOverTime());
        Debug.Log("GoldUI initialized successfully for Ai.");

        StartCoroutine(RunAiLoop());
    }

    private IEnumerator RunAiLoop()
    {
        while (IsServer)
        {
            RunAiTick();
            yield return new WaitForSeconds(aiTickInterval);
        }
    }

    private void RunAiTick()
    {
        bool hasSwordsman = true; // Temporary
        bool hasArcher = true; // Temporary
        int targetPlayerId = 0;

        List<Vector2Int> availableTowerPositions = mapGenerator.GetTowerPosBtwPlayers(aiPlayerId, targetPlayerId);
        DecideAction(Gold, availableTowerPositions);
    }

    private void DecideAction(int gold, List<Vector2Int> towerPositions)
    {
        if (gold >= 5) 
        { 
            SpawnUnit(UnitType.Swordsman);
            Debug.Log("Ai spawning Unit");
        }
    }

    private void SpawnUnit(UnitType unitType)
    {
        Debug.Log($"Spawn inititated by Ai.");

        if (enemyPool == null) enemyPool = FindFirstObjectByType<EnemyPool>();

        if (enemyPool == null)
        {
            Debug.LogError("SpawnUI: EnemyPool is null!");
            return;
        }

        if (TrySpend(5))
        {
            enemyPool.SpawnUnit(aiPlayerId, unitType);
            Debug.Log($"Ai (player {aiPlayerId}) spawns unit.");
        }
        else
        {
            Debug.Log("Not enough gold to spawn unit!");
        }
    }

    public IEnumerator GenerateGoldOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(goldGenerationInterval);
            Gold += goldPerTick;
        }
    }

    private bool TrySpend(int amount)
    {
        bool result = TrySpendGold(amount);
        return result;
    }

    private bool TrySpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            return true;
        }
        return false;
    }
}
