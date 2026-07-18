using UnityEngine;

public class Archer : Unit
{
    private void Reset()
    {
        maxHealth = 100;
        attackDamage = 15;
        attackRange = 7f;
        moveSpeed = 2f;
    }
}