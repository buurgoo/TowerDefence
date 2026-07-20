using TMPro;
using UnityEngine;
using System.Collections;

public class GoldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory playerInventory; 
    [SerializeField] private TextMeshProUGUI goldText;

    public void StartTicking()
    {
        Debug.Log($"Gold counter started.");
        StartCoroutine(GenerateGoldOverTime());
    }

    public IEnumerator GenerateGoldOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(playerInventory.goldGenerationInterval);
            playerInventory.Gold += playerInventory.goldPerTick;
            UpdateGoldDisplay(playerInventory.Gold);
        }
    }

    public bool TrySpend(int amount)
    {
        bool result = playerInventory.TrySpendGold(amount);
        UpdateGoldDisplay(playerInventory.Gold);
        return result;
    }

    private void OnGoldChanged(int previousValue, int newValue)
    {
        UpdateGoldDisplay(newValue);
    }

    private void UpdateGoldDisplay(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {currentGold}";
        }
    }

    //private void OnDestroy()
    //{
    //    if (playerInventory != null)
    //    {
    //        playerInventory.Gold.OnValueChanged -= OnGoldChanged;
    //    }
    //}
}