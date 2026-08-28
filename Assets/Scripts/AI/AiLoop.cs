using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class AiLoop : NetworkBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private float aiTickInterval = 1f;
    [SerializeField] private int aiPlayerId = 0;

    private int Gold = 0;
    [SerializeField] public float goldGenerationInterval = 1f;
    [SerializeField] public int goldPerTick = 1;

    private AiAgent _aiAgent;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _tickCoroutine = StartCoroutine(GenerateGoldOverTime());
        Debug.Log("GoldUI initialized successfully for Ai.");

        _aiAgent = new AiAgent(mapGenerator, aiPlayerId);
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
        int targetPlayerId = 1 - aiPlayerId;

        List<Vector2Int> availableTowerPositions = mapGenerator.GetTowerPosBtwPlayers(aiPlayerId, targetPlayerId);
        AiDecision decision = _aiAgent.ChooseAction(hasSwordsman, hasArcher, availableTowerPositions);

        switch (decision.ActionType)
        {
            case AiActionType.Attack:
                Debug.Log($"AI agent Attack: {decision.UnitType}, " + $"target player: {decision.TargetPlayerId}");
                SpawnUnit(decision.UnitType);
                break;

            case AiActionType.Defend:
                Debug.Log($"AI agent Defend: move tower to " + $"{decision.TowerPosition.Value}");
                break;

            case AiActionType.Wait:
                Debug.Log("AI agent Wait");
                break;
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
