using UnityEngine;

public class Swordsman : Unit
{
    private void Reset()
    {
        maxHealth = 150;
        attackDamage = 25;
        attackRange = 1.5f;
        moveSpeed = 2f;
    }
}