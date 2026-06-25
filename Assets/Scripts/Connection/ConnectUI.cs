using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ConnectUI : NetworkBehaviour
{ 
    [SerializeField] 
    public Button hostButton;
    [SerializeField] 
    public Button clientButton;


    void Start()
    {
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
    }

    public void HostButtonOnClick()
    {
        NetworkManager.StartHost();
    }

    public void ClientButtonOnClick()
    {
        NetworkManager.StartClient();
    }

    public void DisconnectClient()
    {
        NetworkManager.Shutdown();
    }
}
