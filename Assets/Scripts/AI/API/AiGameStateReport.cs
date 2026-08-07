using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class AiGameStateReport : NetworkBehaviour
{
    private const int HumanPlayerId = 0;
    private const int AiPlayerId = 1;
    private const int MaxEnemyUnits = 20;

    [Header("API")]
    [SerializeField]
    private string gameStateUrl = "http://127.0.0.1:8000/game-state";

    [Header("Sending")]
    [SerializeField]
    private float sendingIntervalSeconds = 1f;

    [Header("Game References")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [SerializeField]
    private Inventory inventory;

    private Coroutine reportingCoroutine;
    private string mapId;
    private bool isReporting;

    public void StartReporting()
    {
        if (isReporting)
        {
            Debug.LogWarning("AI reporting!");
            return;
        }

        ResolveReferences();
        mapId = Guid.NewGuid().ToString();
        isReporting = true;
        reportingCoroutine = StartCoroutine(ReportingLoop());
        Debug.Log($"AI reporting started. Map ID: {mapId}");
    }

    public void StopReporting()
    {
        isReporting = false;
        if (reportingCoroutine != null)
        {
            StopCoroutine(reportingCoroutine);
            reportingCoroutine = null;
        }

        Debug.Log("Stop AI reporting!");
    }

    private IEnumerator ReportingLoop()
    {
        while (isReporting)
        {
            ResolveReferences();
            AiGameState state = BuildCurrentGameState();
            yield return SendGameState(state);
            if (state.game_finished)
            {
                isReporting = false;
                reportingCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(sendingIntervalSeconds);
        }
    }

    private AiGameState BuildCurrentGameState()
    {
        bool gameFinished = TryGetGameResult(out int winnerPlayerId);
        return new AiGameState
        {
            map_id = mapId,
            player_id = AiPlayerId,
            coins = inventory != null ? inventory.Gold : 0,
            available_tower_positions = GetAvailableTowerPositions(),
            enemy_units = GetEnemyUnitPositions(),
            game_finished = gameFinished,
            winner_player_id = winnerPlayerId

            /*
                map_id: str
                player_id: int = Field(default=1, ge=0, le=1)
                coins: int = Field(ge=0)
                available_tower_positions: list[GridPosition]
                enemy_units: list[EnemyUnitPosition]
                game_finished: bool
                winner_player_id: int = Field(default=-1, ge=-1, le=1)
            */
        };
    }

    private List<GridPosition> GetAvailableTowerPositions()
    {
        var result = new List<GridPosition>();
        if (mapGenerator == null) return result;
        List<Vector2Int> positions = mapGenerator.GetTowerPosBtwPlayers(AiPlayerId, HumanPlayerId);
        foreach (Vector2Int position in positions)
            result.Add(new GridPosition( position.x, position.y));
        return result;
    }

    private List<EnemyUnitPosition> GetEnemyUnitPositions()
    {
        var result = new List<EnemyUnitPosition>(MaxEnemyUnits);

        Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            if (result.Count >= MaxEnemyUnits) break;
            if (unit == null || unit.IsDead || unit.OwnerPlayerId != HumanPlayerId) continue;

            Vector3 worldPosition = unit.transform.position;
            result.Add(new EnemyUnitPosition(worldPosition.x, worldPosition.z, true));
        }

        while (result.Count < MaxEnemyUnits)
            result.Add(new EnemyUnitPosition(0f, 0f, false));
        return result;
    }

    private bool TryGetGameResult(out int winnerPlayerId)
    {
        winnerPlayerId = -1;

        Castle[] castles = FindObjectsByType<Castle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Castle castle in castles)
        {
            if (castle == null) continue;
            if (castle.getCurrentHP() > 0) continue;

            int losingPlayerId = castle.getPlayerId();
            winnerPlayerId = losingPlayerId == HumanPlayerId ? AiPlayerId : HumanPlayerId;

            return true;
        }

        return false;
    }

    private IEnumerator SendGameState(AiGameState state)
    {
        string json = JsonUtility.ToJson(state);
        byte[] body = Encoding.UTF8.GetBytes(json);
        using UnityWebRequest request = new UnityWebRequest(gameStateUrl, UnityWebRequest.kHttpVerbPOST);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"AI API request failed: {request.error}");
            yield break;
        }

        Debug.Log($"Game state sent. Coins: {state.coins}, finished: {state.game_finished}");
    }

    private void ResolveReferences()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
    }

    private void OnDisable()
    {
        if (isReporting) StopReporting(); 
    }
}
