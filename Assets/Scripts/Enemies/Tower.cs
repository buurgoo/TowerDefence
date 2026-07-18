using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Tower : NetworkBehaviour
{
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 1f; 

    public int OwnerPlayerId { get; private set; }

    public void Initialize(int ownerPlayerId)
    {
        OwnerPlayerId = ownerPlayerId;
        if (IsServer) StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        while (IsServer)
        {
            Unit target = FindClosestEnemyUnit();
            if (target != null) target.TakeDamage(attackDamage);
            yield return new WaitForSeconds(attackCooldown);
        }
    }

    private Unit FindClosestEnemyUnit()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Unit unit in units)
        {
            if (unit.IsDead) continue;
            if (unit.OwnerPlayerId == OwnerPlayerId) continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);

            if (distance > attackRange) continue;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = unit;
            }
        }

        return closestTarget;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}