using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class EndGameUI : NetworkBehaviour
{
    [Header("UI Panels & Text")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private Button returnToMenuButton;

    [Header("UI References To Reset")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject lobbyPanel;

    private void Awake()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);
    }

    private void Start()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void ShowWinScreenRpc(int winningPlayerId)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);

        if (winText != null)
        {
            winText.text = $"Player {winningPlayerId} Won!";
        }
    }

    private void OnReturnToMenuClicked()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);

        MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
        if (mapGen != null)
        {
            foreach (Transform child in mapGen.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}