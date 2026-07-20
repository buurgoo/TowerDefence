using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ConnectUI : NetworkBehaviour
{ 
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private MapGenerator mapGenerator;
    [SerializeField]
    private GoldUI gold;

    private int _seed;


    void Start()
    {
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
        startButton.onClick.AddListener(StartButtonOnClick);
    }

    public override void OnNetworkSpawn()
    {
        RequestSeedRpc();
    }

    public void StartButtonOnClick()
    {
        startGameRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void startGameRpc()
    {
        mapGenerator.GenerateMap();
        gold.StartTicking();
    }

    public void HostButtonOnClick()
    {
        _seed = Random.Range(0, 10000);
        NetworkManager.StartHost();
    }

    public void ClientButtonOnClick()
    {
        NetworkManager.StartClient();
    }

    [Rpc(SendTo.Everyone)]
    public void ReceiveSeedRpc(int seed)
    {
        if (IsClient && !IsHost)
        {
            Debug.Log($"Received seed {seed}");
            _seed = seed;
            mapGenerator.generationSeed = seed;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RespondWithSeedRpc()
    {
        if (IsHost)
        {
            Debug.Log($"Sent seed {_seed}");
            ReceiveSeedRpc(_seed);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RequestSeedRpc()
    {
        if (IsClient && !IsHost) { RespondWithSeedRpc(); }
        else 
        {
            mapGenerator.generationSeed = _seed;
        }
        
    }

    public void DisconnectClient()
    {
        NetworkManager.Shutdown();
    }
}
