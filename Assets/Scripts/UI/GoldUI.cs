using TMPro;
using UnityEngine;
using System.Collections;

public class GoldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory playerInventory; 
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameObject goldPanel;
    [SerializeField] private GameObject goldIcon;

    private Coroutine _tickCoroutine;

    private void Awake()
    {
        if (goldText == null) goldText = GetComponentInChildren<TextMeshProUGUI>();
        
        // Hide UI elements initially while in lobby
        SetUIVisibility(false);
    }

    public void InitializeAndStart()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<Inventory>();
        }

        SetUIVisibility(true);

        if (playerInventory != null)
        {
            UpdateGoldDisplay(playerInventory.Gold);
        }
        else
        {
            Debug.LogWarning("GoldUI: Player Inventory reference is missing on this client!");
        }

        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
        }

        _tickCoroutine = StartCoroutine(GenerateGoldOverTime());
        Debug.Log("GoldUI initialized successfully for client.");
    }

    private void SetUIVisibility(bool visible)
    {
        // Toggle parent panel if assigned
        if (goldPanel != null)
        {
            goldPanel.SetActive(visible);
        }

        // Toggle icon and text elements independently
        if (goldIcon != null)
        {
            goldIcon.SetActive(visible);
        }

        if (goldText != null)
        {
            goldText.gameObject.SetActive(visible);
        }
    }

    public IEnumerator GenerateGoldOverTime()
    {
        while (true)
        {
            if (playerInventory != null)
            {
                yield return new WaitForSeconds(playerInventory.goldGenerationInterval);
                playerInventory.Gold += playerInventory.goldPerTick;
                UpdateGoldDisplay(playerInventory.Gold);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    public bool TrySpend(int amount)
    {
        if (playerInventory == null) return false;

        bool result = playerInventory.TrySpendGold(amount);
        UpdateGoldDisplay(playerInventory.Gold);
        return result;
    }

    private void UpdateGoldDisplay(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = $"{currentGold}";
        }
    }
}