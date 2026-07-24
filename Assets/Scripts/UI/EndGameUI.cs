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
    [SerializeField] private GameObject gameTitle;      
    [SerializeField] private GameObject menuBackground; 

    [Header("In-Game UIs To Clear")]
    [SerializeField] private GoldUI goldUI;
    [SerializeField] private SpawnUI spawnUI;

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
            winText.text = $"The end.";
        }
    }

    private void OnReturnToMenuClicked()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (goldUI == null) goldUI = FindFirstObjectByType<GoldUI>(FindObjectsInactive.Include);
        if (goldUI != null) goldUI.ResetAndHide();

        if (spawnUI == null) spawnUI = FindFirstObjectByType<SpawnUI>(FindObjectsInactive.Include);
        if (spawnUI != null) spawnUI.ResetAndHide();

        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameTitle != null) gameTitle.SetActive(true);
        if (menuBackground != null) menuBackground.SetActive(true);

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