using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem; 

public class DevCommandController : NetworkBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    
    [SerializeField] private string regenerateKeyName = "r"; 

    void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }
    }

    /*void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (IsHost || IsServer)
            {
                int randomSeed = Random.Range(1, 99999);
                mapGenerator.RegenerateMapRpc(randomSeed);
            }
            else
            {
                Debug.LogWarning("[Dev Command] Only the host player can trigger map regeneration!");
            }
        }
    }*/
}