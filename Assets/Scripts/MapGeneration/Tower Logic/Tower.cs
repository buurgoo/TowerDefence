using UnityEngine;

public class Tower : MonoBehaviour
{
    public int attackRange = 2;
    public int attackDamage = 20;
    public float attackCooldown = 1f;

    private bool isOnCooldown = false;
    private float timeRemaining = 0;

    public Tower() {}

    public Tower(int range, int damage, float cooldown) 
    { 
        attackRange = range;
        attackDamage = damage;
        attackCooldown = cooldown;
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
        timeRemaining -= 0.1f;
        if (timeRemaining >= 0)
        {
            Invoke("tick", 0.1f);
        }
        else
        {
            isOnCooldown = false;
        }
    }

    public void attack(Unit unit)
    {
        if (!isOnCooldown)
        {
            unit.TakeDamage(attackDamage);
            cooldown();
        }
    }
}
