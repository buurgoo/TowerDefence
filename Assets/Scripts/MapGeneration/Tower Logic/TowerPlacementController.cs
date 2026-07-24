using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TowerPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GoldUI goldUI;

    [Header("Placement Settings")]
    [SerializeField] private int towerCost = 15;

    private int localPlayerId = -1;
    private List<GameObject> activeHighlights = new List<GameObject>();
    private GameObject hoverHighlightInstance;

    private void Start()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (goldUI == null) goldUI = FindFirstObjectByType<GoldUI>();
    }

    private void Update()
    {
        if (localPlayerId == -1 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            localPlayerId = NetworkManager.Singleton.IsHost ? 0 : 1;
        }

        if (localPlayerId == -1) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            HandlePlacementInput(screenPos);
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

    private void HandlePlacementInput(Vector2 screenPos)
    {
        Debug.Log($"Tower place attempt by player {localPlayerId}");
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (goldUI == null) goldUI = FindFirstObjectByType<GoldUI>();

        Camera mainCam = Camera.main;
        if (mainCam == null || mapGenerator == null)
        {
            Debug.LogWarning("TowerPlacementController: Camera or MapGenerator missing!");
            return;
        }

        Ray ray = mainCam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector2Int gridPos = WorldToGrid(hit.point);

            // Validate placement on map
            if (mapGenerator.IsValidTowerPlacement(gridPos, localPlayerId))
            {
                if (goldUI != null && goldUI.TrySpend(towerCost))
                {
                    mapGenerator.PlaceTowerServerRpc(gridPos, localPlayerId);
                }
                else
                {
                    Debug.Log("TowerPlacementController: Not enough gold to place tower!");
                }
            }
            else
            {
                Debug.LogWarning($"TowerPlacementController: Invalid placement at {gridPos} for Player {localPlayerId}");
            }
        }
        else
        {
            Debug.LogWarning("TowerPlacementController: Raycast missed tile colliders!");
        }
    }

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