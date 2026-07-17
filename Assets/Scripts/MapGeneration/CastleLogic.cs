using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Castle : NetworkBehaviour
{
    [Header("Owner")]
    private int playerId = -1;


    [Header("Health")]
    private int maxHp = 0;
    private int currentHp = 0;

    [SerializeField]
    public GameObject goblin;
    private Dictionary<string, GameObject> spawnableEnemies = new Dictionary<string, GameObject>();

    public void setPlayerId(int id)
    {
        if (playerId == -1) { playerId = id; }
    }

    public int getPlayerId()
    {
        return playerId;
    }

    public void setMaxHP(int hp)
    {
        spawnableEnemies.Add("goblin", goblin);
        if (maxHp == 0) { maxHp = hp; currentHp = hp; }
    }

    public int getMaxHP()
    {
        return maxHp;
    }

    public int getCurrentHP()
    {
        return currentHp;
    }

    public void damage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
        }
    }

    public void heal(int heal)
    {
        currentHp += heal;
        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }
    }

    public void fullHeal()
    {
        currentHp = maxHp;
    }

    [Rpc(SendTo.Me)]
    public void SpawnRpc(string enemy)
    {
        Debug.Log($"Spawning unit for player {playerId}.");
        var instance = Instantiate(spawnableEnemies[enemy], this.transform);
        var instanceEnemy = instance.GetComponent<Enemy>();
        int enemyPlayer = (playerId == 1) ? 0 : 1;
        instanceEnemy.SetTarget(enemyPlayer);
        instance.GetComponent<NetworkObject>().Spawn();
    }
}
