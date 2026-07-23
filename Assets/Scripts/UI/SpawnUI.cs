using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SpawnUI : NetworkBehaviour
{
    [SerializeField] private Button spawnButton;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private int ownerPlayerId = -1;
    [SerializeField] private UnitType unitType = UnitType.Swordsman;
    [SerializeField] private GoldUI gold;
    [SerializeField] private GameObject spawnUIPanel;
    
    private void Awake()
    {
        SetUIVisibility(false);
    }

    public override void OnNetworkSpawn()
    {
        SetPlayerIdRpc();
    }

    void Start()
    {
        if (enemyPool == null) enemyPool = FindFirstObjectByType<EnemyPool>();

        if (spawnButton == null)
        {
            spawnButton = GetComponentInChildren<Button>();
        }

        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveListener(SpawnButtonOnClick);
            spawnButton.onClick.AddListener(SpawnButtonOnClick);
        }
    }
    
    public void InitializeAndStart()
    {
        if (enemyPool == null) enemyPool = FindFirstObjectByType<EnemyPool>();
        if (gold == null) gold = FindFirstObjectByType<GoldUI>();

        SetUIVisibility(true);
        Debug.Log($"SpawnUI initialized for Player {ownerPlayerId}.");
    }

    private void SetUIVisibility(bool visible)
    {
        if (spawnUIPanel != null)
        {
            spawnUIPanel.SetActive(visible);
        }
        else if (spawnButton != null)
        {
            spawnButton.gameObject.SetActive(visible);
        }
    }

    [Rpc(SendTo.Me)]
    public void SetPlayerIdRpc()
    {
        ownerPlayerId = IsHost ? 0 : 1;
    }

    public void SpawnButtonOnClick()
    {
        Debug.Log($"Spawn button clicked by player {ownerPlayerId}.");

        if (enemyPool == null) enemyPool = FindFirstObjectByType<EnemyPool>();
        if (gold == null) gold = FindFirstObjectByType<GoldUI>();

        if (enemyPool == null)
        {
            Debug.LogError("SpawnUI: EnemyPool is null!");
            return;
        }

        if (gold == null)
        {
            Debug.LogError("SpawnUI: GoldUI is null!");
            return;
        }

        if (gold.TrySpend(5))
        {
            SpawnUnitRpc(ownerPlayerId, unitType);
        }
        else
        {
            Debug.Log("Not enough gold to spawn unit!");
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SpawnUnitRpc(int playerOwnerId, UnitType unit)
    {
        if (IsHost && enemyPool != null)
        { 
            enemyPool.SpawnUnit(playerOwnerId, unit); 
        }
    }
}