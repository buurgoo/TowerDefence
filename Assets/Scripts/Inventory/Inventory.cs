using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int Gold = 0;

    [SerializeField] public float goldGenerationInterval = 1f;
    [SerializeField] public int goldPerTick = 1;

    public bool TrySpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            return true;
        }
        return false;
    }
}