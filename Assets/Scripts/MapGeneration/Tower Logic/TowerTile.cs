//using System.Collections;
//using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class TowerTile : TileData
{
    public int attackRange = 2;
    public int attackDamage = 20;
    public float attackCooldown = 1f;

    //public void Initialize(int ownerPlayerId)
    //{
    //    OwnerPlayerId = ownerPlayerId;
    //    //if (IsServer) StartCoroutine(AttackLoop());
    //}

    //private IEnumerator AttackLoop()
    //{
    //    Unit target = FindClosestEnemyUnit();
    //    if (target != null) target.TakeDamage(attackDamage);
    //    yield return new WaitForSeconds(attackCooldown);
    //}

    //private Unit FindClosestEnemyUnit()
    //{
    //    Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
    //    Unit closestTarget = null;
    //    float closestDistance = float.MaxValue;

    //    foreach (Unit unit in units)
    //    {
    //        if (unit.IsDead) continue;
    //        if (unit.OwnerPlayerId == OwnerPlayerId) continue;

    //        float distance = Vector3.Distance(transform.position, unit.transform.position);

    //        if (distance > attackRange) continue;
    //        if (distance < closestDistance)
    //        {
    //            closestDistance = distance;
    //            closestTarget = unit;
    //        }
    //    }

    //    return closestTarget;
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.DrawWireSphere(transform.position, attackRange);
    //}
}
