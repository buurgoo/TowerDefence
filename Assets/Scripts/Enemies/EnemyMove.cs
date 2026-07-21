using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class EnemyMove : NetworkBehaviour 
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private float heightAboveGround = 0.35f;
    [SerializeField] private float towerCheckInterval = 0.2f; 
    [SerializeField] private float unitAttackInterval = 1.0f;

    private Unit unit;
    private Castle targetCastle;
    private int ownerPlayerId;
    private int targetPlayerId;
    private Coroutine attackCheckCoroutine;
    private Coroutine unitLoopCoroutine;

    public void Initialize(MapGenerator generator, int ownerId, int targetId, Castle enemyCastle)
    {
        mapGenerator = generator;
        ownerPlayerId = ownerId;
        targetPlayerId = targetId;
        targetCastle = enemyCastle;

        unit = GetComponent<Unit>();

        if (IsHost || IsServer)
        {
            StopAllMoveRoutines();

            RespawnAtOwnerCastle();
            unitLoopCoroutine = StartCoroutine(UnitLoop());
            attackCheckCoroutine = StartCoroutine(AttackCheckLoop());
        }
    }

    private IEnumerator UnitLoop()
    {
        yield return StartCoroutine(MoveToEnemyCastle());

        while (!unit.IsDead && targetCastle != null && targetCastle.getCurrentHP() > 0)
        {
            float distanceToCastle = Vector3.Distance(transform.position, targetCastle.transform.position);
            if (distanceToCastle <= unit.AttackRange)
            {
                targetCastle.damage(unit.AttackDamage);
            }

            yield return new WaitForSeconds(unitAttackInterval);
        }
    }

    private IEnumerator MoveToEnemyCastle()
    {
        Vector2Int startCell = mapGenerator.GetCastlePosition(ownerPlayerId);
        Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

        List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell, onlyRoad: true);

        if (path == null || path.Count == 0) yield break;

        foreach (Vector2Int cell in path)
        {
            Vector3 destination = mapGenerator.GridToWorld(cell, heightAboveGround);

            while (!unit.IsDead)
            {
                float distanceToCastle = Vector3.Distance(transform.position, targetCastle.transform.position);
                if (distanceToCastle <= unit.AttackRange)
                {
                    yield break;
                }

                transform.position = Vector3.MoveTowards(transform.position, destination, unit.MoveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, destination) <= 0.01f) break;
                                
                yield return null;
            }

            if (unit.IsDead) break;
            transform.position = destination;
        }
    }

    private IEnumerator AttackCheckLoop()
    {
        while (mapGenerator == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        while (!unit.IsDead)
        {
            yield return new WaitForSeconds(towerCheckInterval);

            if (mapGenerator.towers == null) continue;

            for (int i = 0; i < mapGenerator.towers.Count; i++)
            {
                TowerTile towerCell = mapGenerator.towers[i];
                if (towerCell == null || towerCell.tower == null) continue;
                if (towerCell.OwnerPlayerId == ownerPlayerId) continue;

                float distance = Vector3.Distance(transform.position, mapGenerator.GridToWorld(towerCell.GridPosition, heightAboveGround));
                if (distance <= towerCell.tower.attackRange)
                {
                    towerCell.tower.attack(unit);
                }
            }
        }
    }

    private void StopAllMoveRoutines()
    {
        if (attackCheckCoroutine != null)
        {
            StopCoroutine(attackCheckCoroutine);
            attackCheckCoroutine = null;
        }
        if (unitLoopCoroutine != null)
        {
            StopCoroutine(unitLoopCoroutine);
            unitLoopCoroutine = null;
        }
    }

    private void RespawnAtOwnerCastle() 
    {
        if (unit.IsDead) return;

        Vector2Int ownerCastleCell = mapGenerator.GetCastlePosition(ownerPlayerId);
        if (ownerCastleCell != new Vector2Int(-1, -1))
        {
            transform.position = mapGenerator.GridToWorld(ownerCastleCell, heightAboveGround);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        StopAllMoveRoutines();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopAllMoveRoutines();
    }
}