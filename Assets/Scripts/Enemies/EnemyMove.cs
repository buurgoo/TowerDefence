using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class EnemyMove : NetworkBehaviour {
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private float heightAboveGround = 0.35f;
    private Unit unit;
    private Castle targetCastle;
    private int ownerPlayerId;
    private int targetPlayerId;

    public void Initialize(MapGenerator generator, int ownerId, int targetId, Castle enemyCastle)
    {
        mapGenerator = generator;
        ownerPlayerId = ownerId;
        targetPlayerId = targetId;
        targetCastle = enemyCastle;

        unit = GetComponent<Unit>();
        unit.Initialize(ownerPlayerId);

        if (IsServer) StartCoroutine(UnitLoop());
    }

    private IEnumerator UnitLoop()
    {
        while (!unit.IsDead && !targetCastle.IsDestroyed)
        {
            RespawnAtOwnerCastle();

            yield return StartCoroutine(MoveToEnemyCastle());

            if (unit.IsDead || targetCastle.IsDestroyed) yield break;
            targetCastle.TakeDamage(unit.AttackDamage); // enemy castle boom boom
            yield return null;
        }
    }

    // private IEnumerator Start() {
    //     yield return null;

    //     if (mapGenerator == null) yield break;

    //     Vector2Int startCell = mapGenerator.GetCastlePosition(0);
    //     Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

    //     if (startCell.x < 0 || targetCell.x < 0) yield break;
        
    //     List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell);
    //     Debug.Log("Enemy: path length = " + path.Count);
    //     if (path.Count == 0 && startCell != targetCell) yield break;

    //     transform.position = mapGenerator.GridToWorld(startCell, heightAboveGround); 

    //     foreach (Vector2Int cell in path)
    //     {
    //         Vector3 targetPosition = mapGenerator.GridToWorld(cell, heightAboveGround);

    //         while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
    //         {
    //                 transform.position = Vector3.MoveTowards(
    //                     transform.position, 
    //                     targetPosition, 
    //                     moveSpeed * Time.deltaTime
    //                 );
    //                 yield return null;
    //         }        
    //         transform.position = targetPosition;
    //     }
    // }

    private IEnumerator MoveToEnemyCastle()
    {
        Vector2Int startCell = mapGenerator.GetCastlePosition(ownerPlayerId);
        Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

        List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell, onlyRoad: true);

        foreach (Vector2Int cell in path)
        {
            Vector3 destination = mapGenerator.GridToWorld(cell, heightAboveGround);

            while (!unit.IsDead)
            {
                float distanceToCastle = Vector3.Distance(transform.position, targetCastle.transform.position);
                if (distanceToCastle <= unit.AttackRange) yield break;

                transform.position = Vector3.MoveTowards(transform.position, destination, unit.MoveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, destination) <= 0.01f) break;
                yield return null;
            }

            if (unit.IsDead) yield break;
            transform.position = destination;
        }
    }

    private void RespawnAtOwnerCastle()
    {
        if (unit.IsDead) return;
        Vector2Int ownerCastleCell = mapGenerator.GetCastlePosition(ownerPlayerId);
        transform.position = mapGenerator.GridToWorld(ownerCastleCell, heightAboveGround);
    }
}
