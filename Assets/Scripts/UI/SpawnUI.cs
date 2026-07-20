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

    public override void OnNetworkSpawn()
    {
        setPlayerIdRpc();
    }

    void Start()
    {
        if (enemyPool == null) enemyPool = GetComponent<EnemyPool>();

        if (spawnButton == null)
        {
            Debug.LogError("Spawn Button is not assigned");
            return;
        }

        if (enemyPool == null)
        {
            Debug.LogError("EnemyPool component was not found on GameManager");
            return;
        }
        spawnButton.onClick.AddListener(SpawnButtonOnClick);
    }

    [Rpc(SendTo.Me)]
    public void setPlayerIdRpc()
    {
        if (IsHost) { ownerPlayerId = 0; }
        else { ownerPlayerId = 1; }
    }

    public void SpawnButtonOnClick()
    {
        Debug.Log($"Spawn button clicked by player {ownerPlayerId}.");

        if (enemyPool == null) enemyPool = GetComponent<EnemyPool>();

        if (enemyPool == null)
        {
            Debug.LogError("EnemyPool is null");
            return;
        }
        if (gold == null) { Debug.LogError("Gold is null"); return; }

        if (gold.TrySpend(5))
        {
            spawnUnitRpc(ownerPlayerId, unitType);
        }
        else
        {
            Debug.Log("Not enough gold.");
        }
    }

    [Rpc(SendTo.Everyone)]
    public void spawnUnitRpc(int ownerPlayerId, UnitType unitType)
    {
        if (IsHost) { 
            enemyPool.SpawnUnit(ownerPlayerId, unitType); 
        }
    }
}
