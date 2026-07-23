using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Castle : NetworkBehaviour
{
    [Header("Owner")]
    private NetworkVariable<int> networkPlayerId = new NetworkVariable<int>(-1);

    [Header("Health")]
    private int maxHp = 0;
    private int currentHp = 0;

    public void setPlayerId(int id)
    {
        if (IsServer || IsHost)
        {
            networkPlayerId.Value = id;
        }
    }

    public int getPlayerId()
    {
        return networkPlayerId.Value;
    }

    public void setMaxHP(int hp)
    {
        if (maxHp == 0) { maxHp = hp; currentHp = hp; }
    }

    public int getMaxHP() => maxHp;
    public int getCurrentHP() => currentHp;

    public void damage(int damage)
    {
        if (!IsServer && !IsHost) return;
        
        Debug.Log($"Player {networkPlayerId.Value} castle damaged. Remaining: {currentHp}/{maxHp}");
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            Lose();
        }
    }

    public void Lose()
    {
        int losingPlayerId = networkPlayerId.Value;
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

    public void heal(int heal)
    {
        currentHp += heal;
        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }
    }

    public void fullHeal()
    {
        currentHp = maxHp;
    }
}