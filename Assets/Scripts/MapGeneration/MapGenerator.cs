using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MapGenerator : NetworkBehaviour
{
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float tileSize = 1f;

    private TileData[,] mapGrid;
    
    public int playerCount = 2;
    public int neutralBasesCount = 3;
    public int minDistanceBetweenObjects = 5;

    private List<Vector2Int> criticalNodes = new List<Vector2Int>();
    private List<Vector2Int> spacedObjects = new List<Vector2Int>();

    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        mapSeed.OnValueChanged += OnSeedChanged;

        if (IsServer)
        {
            mapSeed.Value = Random.Range(1, 999999); 
        }
        else
        {
            if (mapSeed.Value != 0)
            {
                GenerateMap(mapSeed.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        mapSeed.OnValueChanged -= OnSeedChanged;
    }

    private void OnSeedChanged(int oldSeed, int newSeed)
    {
        GenerateMap(newSeed);
    }

    public void GenerateMap(int seed)
    {
        Random.InitState(seed);

        mapGrid = new TileData[mapWidth, mapHeight];
        criticalNodes.Clear();
        spacedObjects.Clear();

        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                mapGrid[x, y] = new TileData { GridPosition = new Vector2Int(x, y), CurrentType = TileType.Empty };
            }
        }

        for (int i = 0; i < playerCount; i++) {
            Vector2Int pos = FindValidPosition();
            
            if (pos == new Vector2Int(-1, -1))
            {
                Debug.LogError("Failed to place castle.");
                continue;
            }
            
            mapGrid[pos.x, pos.y].CurrentType = TileType.Castle;
            mapGrid[pos.x, pos.y].OwnerPlayerId = i;
            criticalNodes.Add(pos);
        }

        for (int i = 0; i < neutralBasesCount; i++)
        {
            Vector2Int pos = FindValidPosition();

            if (pos == new Vector2Int(-1, -1))
            {
                Debug.LogError("Failed to place outpost.");
                continue;
            }

            mapGrid[pos.x, pos.y].CurrentType = TileType.Outpost;
            criticalNodes.Add(pos);
        }

        GenerateRoadNetwork();

        ScatterResources();

        InstantiateDebugGrid();
    }

    private Vector2Int FindValidPosition()
    {
        int attempts = 0;
        while (attempts < 500)
        {
            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);
            Vector2Int pos = new Vector2Int(x, y);

            if (mapGrid[x, y].CurrentType == TileType.Empty && IsFarEnough(pos))
            {
                spacedObjects.Add(pos);
                return pos;
            }
            attempts++;
        }
        return new Vector2Int(-1, -1);
    }

    private void GenerateRoadNetwork()
    {
        for (int i = 0; i < criticalNodes.Count - 1; i++) {
            CreateLineRoad(criticalNodes[i], criticalNodes[i + 1]);
        }
    }

    private void CreateLineRoad(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        while (current.x != end.x) {
            current.x += (end.x > current.x) ? 1 : -1;
            if (mapGrid[current.x, current.y].CurrentType == TileType.Empty)
                mapGrid[current.x, current.y].CurrentType = TileType.Road;
        }
        while (current.y != end.y) {
            current.y += (end.y > current.y) ? 1 : -1;
            if (mapGrid[current.x, current.y].CurrentType == TileType.Empty)
                mapGrid[current.x, current.y].CurrentType = TileType.Road;
        }
    }

    [Header("Resource Settings")]
    [SerializeField] private int forestClusterCount = 5;
    [SerializeField] private int forestClusterSize = 10;
    [SerializeField, Range(0f, 1f)] private float mineSpawnChance = 0.005f;

    private void ScatterResources()
    {
        for (int i = 0; i < forestClusterCount; i++)
        {
            GenerateForestCluster();
        }

        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                if (mapGrid[x, y].CurrentType == TileType.Empty) {
                    if (Random.value < mineSpawnChance)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsFarEnough(pos))
                        {
                            mapGrid[x, y].CurrentType = TileType.Mine;
                            spacedObjects.Add(pos);
                        }
                    }
                }
            }
        }
    }

    private void GenerateForestCluster()
    {
        Vector2Int seed = GetRandomEmptyTile();
        if (seed == new Vector2Int(-1, -1)) return;

        List<Vector2Int> openList = new List<Vector2Int> { seed };
        int tilesSpawned = 0;

        while (openList.Count > 0 && tilesSpawned < forestClusterSize)
        {
            int randomIndex = Random.Range(0, openList.Count);
            Vector2Int currentTile = openList[randomIndex];
            openList.RemoveAt(randomIndex);

            if (mapGrid[currentTile.x, currentTile.y].CurrentType == TileType.Empty)
            {
                mapGrid[currentTile.x, currentTile.y].CurrentType = TileType.Forest;
                tilesSpawned++;

                foreach (var neighbor in GetNeighbors(currentTile))
                {
                    if (mapGrid[neighbor.x, neighbor.y].CurrentType == TileType.Empty && !openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }
    }

    private void InstantiateDebugGrid()
    {
        float offsetX = (mapWidth * tileSize) / 2f;
        float offsetZ = (mapHeight * tileSize) / 2f;
        
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float worldX = (x * tileSize) - offsetX + (tileSize / 2f);
                float worldZ = (y * tileSize) - offsetZ + (tileSize / 2f);
            
                cube.transform.position = new Vector3(worldX, 0, worldZ);
                cube.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);
                cube.transform.SetParent(this.transform);

                Renderer ren = cube.GetComponent<Renderer>();
                ren.material.color = GetColorForType(mapGrid[x, y].CurrentType, mapGrid[x, y].OwnerPlayerId);
            }
        }
    }

    private Color GetColorForType(TileType type, int owner)
    {
        switch (type) {
            case TileType.Castle: 
                if (owner == 0) return Color.red;
                if (owner == 1) return Color.blue;
                if (owner == 2) return Color.green;
                return Color.yellow;
            case TileType.Outpost: return Color.magenta;
            case TileType.Road: return new Color(0.54f, 0.27f, 0.07f);
            case TileType.Forest: return new Color(0f, 0.39f, 0f);
            case TileType.Mine: return Color.black;
            default: return new Color(0.49f, 0.99f, 0f);
        }
    }
    
    private Vector2Int GetRandomEmptyTile()
    {
        int attempts = 0;
        while (attempts < 300)
        {
            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);
            if (mapGrid[x, y].CurrentType == TileType.Empty) return new Vector2Int(x, y);
            attempts++;
        }
        return new Vector2Int(-1, -1);
    }
    
    private bool IsFarEnough(Vector2Int pos)
    {
        float minDistSq = minDistanceBetweenObjects * minDistanceBetweenObjects;
        foreach (var other in spacedObjects)
        {
            if ((pos - other).sqrMagnitude < minDistSq)
                return false;
        }
        return true;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (pos.x > 0) neighbors.Add(new Vector2Int(pos.x - 1, pos.y));
        if (pos.x < mapWidth - 1) neighbors.Add(new Vector2Int(pos.x + 1, pos.y));
        if (pos.y > 0) neighbors.Add(new Vector2Int(pos.x, pos.y - 1));
        if (pos.y < mapHeight - 1) neighbors.Add(new Vector2Int(pos.x, pos.y + 1));
        return neighbors;
    }

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos) { /* ... */ return new List<Vector2Int>(); }
    public Vector2Int GetCastlePosition(int playerId) { /* ... */ return Vector2Int.zero; }
    private bool IsWalkable(Vector2Int pos) { /* ... */ return true; }
    public Vector3 GridToWorld(Vector2Int cell, float height = 0.35f) { /* ... */ return Vector3.zero; }
}