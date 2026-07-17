using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    public NetworkVariable<int> Gold = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float goldGenerationInterval = 1f;
    [SerializeField] private int goldPerTick = 1;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(GenerateGoldOverTime());
        }
    }

    private IEnumerator GenerateGoldOverTime()
    {
        while (IsServer)
        {
            yield return new WaitForSeconds(goldGenerationInterval);
            Gold.Value += goldPerTick;
        }
    }

    public bool TrySpendGold(int amount)
    {
        if (!IsServer) return false;

        if (Gold.Value >= amount)
        {
            Gold.Value -= amount;
            return true;
        }
        return false;
    }
}