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

   private AiAgent _aiAgent;

   public override void OnNetworkSpawn()
   {
       if (!IsServer) return;
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
               enemyPool.SpawnUnit(aiPlayerId, decision.UnitType);
               break;

           case AiActionType.Defend:
               Debug.Log($"AI agent Defend: move tower to " + $"{decision.TowerPosition.Value}");
               break;

           case AiActionType.Wait:
               Debug.Log("AI agent Wait");
               break;
       }
   }
}
