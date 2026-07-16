using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyPool : NetworkBehaviour
{
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject swordsmanPrefab;

    [Header("Game objects")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private Castle player0Castle;
    [SerializeField] private Castle player1Castle;

    [Header("Pool settings")]
    [SerializeField] private int poolSize = 20;

    private readonly List<Unit> units = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        CreatePool(archerPrefab);
        CreatePool(swordsmanPrefab);
    }

    private void CreatePool(GameObject prefab)
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject unitObject = Instantiate(prefab);
            Unit unit = unitObject.GetComponent<Unit>();
            units.Add(unit);
            unitObject.SetActive(false);
        }
    }

    public Unit SpawnUnit(int ownerPlayerId, UnitType unitType)
    {
        if (!IsServer) return null;
        
        if (CountActiveUnits() >= poolSize)
        {
            Debug.LogWarning($"Maximum number of active units reached: {poolSize}");
            return null;
        }

        Unit unit = units.Find(candidate => !candidate.gameObject.activeSelf && MatchesType(candidate, unitType));

        if (unit == null)
        {
            Debug.LogWarning("There are no free units in the pool");
            return null;
        }

        int targetPlayerId = 1 - ownerPlayerId;
        Castle targetCastle = targetPlayerId == 0 ? player0Castle : player1Castle;
        unit.gameObject.SetActive(true);
        NetworkObject networkObject = unit.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned) networkObject.Spawn();
        unit.Initialize(ownerPlayerId, this);
        EnemyMove movement = unit.GetComponent<EnemyMove>();
        movement.Initialize(mapGenerator, ownerPlayerId, targetPlayerId, targetCastle);
        return unit;
    }

    private bool MatchesType(Unit unit, UnitType unitType)
    {
        return unitType switch
        {
            UnitType.Archer => unit is Archer,
            UnitType.Swordsman => unit is Swordsman,
            _ => false
        };
    }

    private int CountActiveUnits()
    {
        int count = 0;
        foreach (Unit unit in units)
            if (unit != null && unit.gameObject.activeSelf) count++;
        return count;
    }

    public void ReturnToPool(Unit unit)
    {
        if (!IsServer || unit == null) return;
        NetworkObject networkObject = unit.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn(false);
        unit.gameObject.SetActive(false);
    }
}