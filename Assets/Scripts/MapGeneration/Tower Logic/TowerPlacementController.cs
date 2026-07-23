using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class TowerPlacementController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GoldUI goldUI;

    [Header("Placement Settings")]
    [SerializeField] private int towerCost = 15;

    private int localPlayerId = -1;
    private List<GameObject> activeHighlights = new List<GameObject>();
    private GameObject hoverHighlightInstance;

    void Start()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (goldUI == null) goldUI = FindFirstObjectByType<GoldUI>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost || IsServer) localPlayerId = 0;
        else localPlayerId = 1;
    }

    void Update()
    {
        if (!IsClient || localPlayerId == -1) return;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            HandlePlacementInput();
        }
    }
    
    public void ClearHighlights()
    {
        foreach (GameObject highlight in activeHighlights)
        {
            if (highlight != null) Destroy(highlight);
        }
        activeHighlights.Clear();
    }


    private void HandlePlacementInput()
    {
        Debug.Log($"Tower place attempt by player {localPlayerId}");
        if (mapGenerator == null || Camera.main == null || Pointer.current == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridPos = WorldToGrid(hit.point);

            // Will safely evaluate false now if map isn't generated yet
            if (mapGenerator.IsValidTowerPlacement(gridPos, localPlayerId))
            {
                if (goldUI != null && goldUI.TrySpend(towerCost))
                {
                    mapGenerator.PlaceTowerServerRpc(gridPos, localPlayerId);
                }
            }
        }
    }

    private int mapHeightGrid() => mapGenerator.mapHeight;

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float offsetX = (mapGenerator.mapWidth * mapGenerator.tileSize) / 2f;
        float offsetZ = (mapGenerator.mapHeight * mapGenerator.tileSize) / 2f;

        int x = Mathf.FloorToInt((worldPos.x + offsetX) / mapGenerator.tileSize);
        int y = Mathf.FloorToInt((worldPos.z + offsetZ) / mapGenerator.tileSize);

        return new Vector2Int(x, y);
    }

    private void OnDestroy()
    {
        ClearHighlights();
        if (hoverHighlightInstance != null) Destroy(hoverHighlightInstance);
    }
}