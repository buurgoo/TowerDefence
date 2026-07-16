using Unity.Netcode;
using UnityEngine;

public abstract class Unit : NetworkBehaviour
{
    [SerializeField] protected int maxHealth;
    [SerializeField] protected int attackDamage;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float moveSpeed;

    private int currentHealth;

    public int OwnerPlayerId { get; private set; }

    public bool IsDead => currentHealth <= 0;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public int AttackDamage => attackDamage;

    public void Initialize(int ownerPlayerId)
    {
        OwnerPlayerId = ownerPlayerId;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || IsDead) return;
        currentHealth -= damage;
        Debug.Log($"{name} received {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die(); // oh no! You die(
    }

    private void Die()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();

        if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
        else Destroy(gameObject);
    }
}