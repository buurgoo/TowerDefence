using UnityEngine;

public class Tower : MonoBehaviour
{
    public int attackRange = 5;
    public int attackDamage = 75;
    public float attackCooldown = 1f;

    private bool isOnCooldown = false;
    private float timeRemaining = 0;

    public void createTower(int range, int damage, float cooldown) 
    { 
        attackRange = range;
        attackDamage = damage;
        attackCooldown = cooldown;
    }

    public void createTower(TowerTile tile)
    {
        attackRange = tile.attackRange;
        attackDamage = tile.attackDamage;
        attackCooldown = tile.attackCooldown;
    }

    private void cooldown()
    {
        if (!isOnCooldown)
        {
            isOnCooldown = true;
            timeRemaining = attackCooldown;
            Invoke("tick", 0.1f);
        }
    }

    private void tick()
    {
        Debug.Log($"Cooldown tick. Remaining: {timeRemaining}");
        timeRemaining -= 0.1f;
        if (timeRemaining >= 0)
        {
            Invoke("tick", 0.1f);
        }
        else
        {
            timeRemaining = 0;
            isOnCooldown = false;
        }
    }

    public void attack(Unit unit)
    {
        Debug.Log("Tower tries attacking.");
        if (!isOnCooldown)
        {
            Debug.Log("Tower attacks.");
            unit.TakeDamage(attackDamage);
            cooldown();
        }
    }
}
