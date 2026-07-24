using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SpawnUI : MonoBehaviour
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

    private void Start()
    {
        if (spawnButton == null) spawnButton = GetComponentInChildren<Button>();

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

        if (NetworkManager.Singleton != null)
        {
            ownerPlayerId = NetworkManager.Singleton.IsHost ? 0 : 1;
        }

        SetUIVisibility(true);
        Debug.Log($"SpawnUI initialized for Player {ownerPlayerId}.");
    }

    private void SetUIVisibility(bool visible)
    {
        if (visible) gameObject.SetActive(true);

        if (spawnUIPanel != null)
        {
            spawnUIPanel.SetActive(visible);
        }
        else if (spawnButton != null)
        {
            spawnButton.gameObject.SetActive(visible);
        }
    }

    public void ResetAndHide()
    {
        SetUIVisibility(false);
    }

    public void SpawnButtonOnClick()
    {
        if (ownerPlayerId == -1 && NetworkManager.Singleton != null)
        {
            ownerPlayerId = NetworkManager.Singleton.IsHost ? 0 : 1;
        }

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
            enemyPool.RequestSpawnUnitServerRpc(ownerPlayerId, unitType);
        }
        else
        {
            Debug.Log("Not enough gold to spawn unit!");
        }
    }
}