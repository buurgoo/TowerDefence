using UnityEngine;
using UnityEngine.UI;

public class SpawnUI : MonoBehaviour
{
    [SerializeField] public Button spawnButton;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private int ownerPlayerId = 0;
    [SerializeField] private UnitType unitType = UnitType.Swordsman;

    // private int player = 0;

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

    public void SpawnButtonOnClick()
    {
        Debug.Log($"Spawn button clicked.");
        // var instanceCastle = castle.GetComponent<Castle>();
        // Debug.Log($"Castle id = {instanceCastle.getPlayerId()}");
        // if (instanceCastle.getPlayerId() == player)
        // {
        //     instanceCastle.SpawnRpc("goblin");
        // }
        if (enemyPool == null) enemyPool = GetComponent<EnemyPool>();

        if (enemyPool == null)
        {
            Debug.LogError("EnemyPool is null");
            return;
        }
        enemyPool.SpawnUnit(ownerPlayerId, unitType);
    }
}
