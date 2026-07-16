using Unity.Netcode;
using UnityEngine;

public class Castle : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 500;

    private int currentHealth;

    public int OwnerPlayerId { get; private set; }
    public bool IsDestroyed => currentHealth <= 0;

    public void Initialize(int ownerPlayerId)
    {
        OwnerPlayerId = ownerPlayerId;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || IsDestroyed) return;
        currentHealth -= damage;
        Debug.Log($"Castle {OwnerPlayerId} received {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"Castle {OwnerPlayerId} destroyed");
        }
    }
}