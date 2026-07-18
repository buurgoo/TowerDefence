using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class EnemyMove : NetworkBehaviour 
{
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

        if (IsHost) StartCoroutine(UnitLoop());
    }

    private IEnumerator UnitLoop()
    {
        //RespawnAtOwnerCastle();

        //while (!unit.IsDead && targetCastle.getCurrentHP() > 0)
        //{
            RespawnAtOwnerCastle();

            yield return StartCoroutine(MoveToEnemyCastle());

        //    if (unit.IsDead || targetCastle.getCurrentHP() <= 0) yield break;
        //    targetCastle.damage(unit.AttackDamage); // enemy castle boom boom
            unit.Die();
            yield return null;
        //}
    }

    private IEnumerator MoveToEnemyCastle()
    {
        Vector2Int startCell = mapGenerator.GetCastlePosition(ownerPlayerId);
        Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

        List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell, onlyRoad: true);

        foreach (Vector2Int cell in path)
        {
            if (mapGenerator.GetTileDataAt(cell).CurrentType == TileType.Tower)
            {
                TowerTile towerTile = mapGenerator.GetTileDataAt(cell) as TowerTile;
                Tower tower = towerTile.tower;

                if (Vector3.Distance(transform.position, 
                    mapGenerator.GridToWorld(cell, heightAboveGround)) <= tower.attackRange)
                {
                    tower.attack(unit);
                }
            }

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
