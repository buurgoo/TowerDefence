using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{
    public TowerTile tower;

    private bool isOnCooldown = false;
    private float timeRemaining;

    public TowerBehaviour(TowerTile towerTile) { tower = towerTile; }

    private void cooldown()
    {
        if (!isOnCooldown)
        {
            isOnCooldown = true;
            timeRemaining = tower.attackCooldown;
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
            unit.TakeDamage(tower.attackDamage);
            cooldown();
        }
    }
}
