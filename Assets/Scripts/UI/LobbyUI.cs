using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject menuPanel;   
    [SerializeField] private GameObject lobbyPanel;  

    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button startGameButton;

    [Header("Text Displays")]
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text statusText;

    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;

    [Header("Lobby Settings")]
    [SerializeField] private int minPlayersToStart = 2;

    private void Start()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();

        menuPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        hostButton.onClick.AddListener(OnHostClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private void OnEnable()
    {
        // Safe callback subscription handling
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    #region Button Handlers

    private void OnHostClicked()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            // Subscribe here if OnEnable ran before NetworkManager.Singleton was ready
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            ShowLobbyView();
            UpdatePlayerList(); 
        }
        else
        {
            if (statusText != null) statusText.text = "Failed to start Host!";
        }
    }

    private void OnConnectClicked()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            ShowLobbyView();
            if (statusText != null) statusText.text = "Connecting to server...";
            UpdatePlayerList();
        }
        else
        {
            if (statusText != null) statusText.text = "Failed to start Client!";
        }
    }

    private void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;

        // Hide UI for all players via RPC or client notification
        HideLobbyClientRpc();

        // Regenerate map via Network RPC so host AND client generate identical seeds
        int randomSeed = Random.Range(1, 99999);
        mapGenerator.GenerateMap();
    }

    [Rpc(SendTo.Everyone)]
    private void HideLobbyClientRpc()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    #endregion

    #region Network Callbacks

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"LobbyUI: Client connected with ID {clientId}");
        UpdatePlayerList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"LobbyUI: Client disconnected with ID {clientId}");
        UpdatePlayerList();
    }

    private void ShowLobbyView()
    {
        menuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void UpdatePlayerList()
    {
        if (NetworkManager.Singleton == null) return;

        IReadOnlyList<NetworkClient> connectedClients = NetworkManager.Singleton.ConnectedClientsList;
        int currentCount = connectedClients.Count;

        string text = $"<b>Connected Players ({currentCount}/{minPlayersToStart}):</b>\n\n";
        for (int i = 0; i < currentCount; i++)
        {
            ulong id = connectedClients[i].ClientId;
            string role = (id == NetworkManager.ServerClientId) ? " (Host)" : " (Client)";
            text += $"• Player {i} [ID: {id}]{role}\n";
        }

        if (playerListText != null) playerListText.text = text;

        bool isHost = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        bool hasEnoughPlayers = currentCount >= minPlayersToStart;

        // Show Start Game button for Host when enough players join
        if (startGameButton != null)
        {
            bool showStartButton = isHost && hasEnoughPlayers;
            startGameButton.gameObject.SetActive(showStartButton);

            // Hide the player list text when the Start Game button appears
            if (playerListText != null)
            {
                playerListText.gameObject.SetActive(!showStartButton);
            }
        }

        if (statusText != null)
        {
            if (!hasEnoughPlayers)
            {
                statusText.text = "Waiting for another player to join...";
            }
            else if (isHost)
            {
                statusText.text = "All players ready! Click Start Game to begin.";
            }
            else
            {
                statusText.text = "Waiting for the Host to start the game...";
            }
        }
    }

    #endregion
}