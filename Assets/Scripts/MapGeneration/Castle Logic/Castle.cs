using Unity.Netcode;
using UnityEngine;

public class Castle : NetworkBehaviour
{
    [Header("Owner")]
    private NetworkVariable<int> networkPlayerId = new NetworkVariable<int>(-1);

    [Header("Health")]
    [SerializeField] private int maxHp = 100;
    private int currentHp;

    [Header("Visuals")]
    [SerializeField] private Renderer[] castleRenderers;

    [SerializeField] private Color player0Color = Color.red;
    [SerializeField] private Color player1Color = Color.blue;

    private void Awake()
    {
        if (castleRenderers == null || castleRenderers.Length == 0)
        {
            castleRenderers = GetComponentsInChildren<Renderer>();
        }

        currentHp = maxHp;
    }

    public override void OnNetworkSpawn()
    {
        networkPlayerId.OnValueChanged += OnPlayerChanged;

        ApplyColor(networkPlayerId.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkPlayerId.OnValueChanged -= OnPlayerChanged;
    }

    private void OnPlayerChanged(int previous, int current)
    {
        ApplyColor(current);
    }

    private void ApplyColor(int playerId)
    {
        Color color = playerId == 0 ? player0Color : player1Color;

        foreach (Renderer renderer in castleRenderers)
        {
            if (renderer == null)
                continue;

            renderer.material.SetColor("_BaseColor", color);
        }
    }

    public void setPlayerId(int id)
    {
        if (!IsServer)
            return;

        networkPlayerId.Value = id;
    }

    public int getPlayerId()
    {
        return networkPlayerId.Value;
    }

    public void setMaxHP(int hp)
    {
        maxHp = hp;
        currentHp = hp;
    }

    public int getMaxHP()
    {
        return maxHp;
    }

    public int getCurrentHP()
    {
        return currentHp;
    }


    public void heal(int heal)
    {
        currentHp += heal;

        if (currentHp > maxHp)
            currentHp = maxHp;
    }

    public void fullHeal()
    {
        currentHp = maxHp;
    }

    public void damage(int damage)
    {
        if (!IsServer && !IsHost) return;
        
        Debug.Log($"Player {networkPlayerId.Value} castle damaged. Remaining: {currentHp}/{maxHp}");
        currentHp -= damage;
        
        if (currentHp <= 0)
        {
            currentHp = 0;
            Lose(networkPlayerId.Value);
        }
    }

    public void Lose(int losingPlayerId)
    {
        int winningPlayerId = (losingPlayerId == 0) ? 1 : 0;

        Debug.Log($"Player {losingPlayerId}'s castle destroyed! Player {winningPlayerId} Wins!");

        EndGameUI endGameUI = FindFirstObjectByType<EndGameUI>();
        if (endGameUI != null)
        {
            endGameUI.ShowWinScreenRpc(winningPlayerId);
        }
        else
        {
            Debug.LogError("Castle: EndGameUI not found in scene!");
        }
    }
}