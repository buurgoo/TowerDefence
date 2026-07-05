using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ConnectUI : NetworkBehaviour
{ 
    [SerializeField] 
    public Button hostButton;
    [SerializeField] 
    public Button clientButton;
    //[SerializeField]
    //public Button mapGeneratorButton;
    [SerializeField]
    public MapGenerator mapGenerator;

    private int _seed;


    void Start()
    {
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
        //mapGeneratorButton.onClick.AddListener(MapGeneratorButtonOnClick);
    }

    public override void OnNetworkSpawn()
    {
        RequestSeedRpc();
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
            mapGenerator.GenerateMap();
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
            mapGenerator.GenerateMap();
        }
        
    }

    //public void MapGeneratorButtonOnClick()
    //{
    //    mapGenerator.GenerateMap();
    //}

    public void DisconnectClient()
    {
        NetworkManager.Shutdown();
    }
}
