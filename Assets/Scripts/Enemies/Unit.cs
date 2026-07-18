using Unity.Netcode;
using UnityEngine;

public abstract class Unit : NetworkBehaviour
{
    [SerializeField] protected int maxHealth;
    [SerializeField] protected int attackDamage;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float moveSpeed;

    private int currentHealth;

    public int OwnerPlayerId = -1;

    public bool IsDead => currentHealth <= 0;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public int AttackDamage => attackDamage;

    private EnemyPool enemyPool;


    public void Initialize(int ownerPlayerId, EnemyPool pool)
    {
        OwnerPlayerId = ownerPlayerId;
        enemyPool = pool;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!IsHost || IsDead) return;

        currentHealth -= damage;
        Debug.Log($"{name} received {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die(); // oh no! You die(
    }

    public void Die()
    {
        // NetworkObject networkObject = GetComponent<NetworkObject>();

        // if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
        // else Destroy(gameObject);
        enemyPool.ReturnToPool(this); 
    }
}