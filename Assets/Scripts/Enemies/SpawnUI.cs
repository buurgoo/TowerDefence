using UnityEngine;
using UnityEngine.UI;
//using Unity.Netcode;

public class SpawnUI : MonoBehaviour
{
    [SerializeField]
    public Button spawnButton;
    [SerializeField]
    public GameObject castle;

    private int player = 0;

    void Start()
    {
        spawnButton.onClick.AddListener(SpawnButtonOnClick);
    }

    public void SpawnButtonOnClick()
    {
        Debug.Log($"Spawn button clicked.");
        var instanceCastle = castle.GetComponent<Castle>();
        Debug.Log($"castle id = {instanceCastle.getPlayerId()}");
        if (instanceCastle.getPlayerId() == player)
        {
            instanceCastle.SpawnRpc("goblin");
        }
    }
}
