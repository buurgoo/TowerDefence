using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory playerInventory; 
    [SerializeField] private TextMeshProUGUI goldText;

    void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.Gold.OnValueChanged += OnGoldChanged;
            
            UpdateGoldDisplay(playerInventory.Gold.Value);
        }
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

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.Gold.OnValueChanged -= OnGoldChanged;
        }
    }
}