using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
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

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        mapGrid = new TileData[mapWidth, mapHeight];
        criticalNodes.Clear();
        spacedObjects.Clear();

        // 1. initialize clean grid
        for (int x = 0; x < mapWidth; x++) {
            
            for (int y = 0; y < mapHeight; y++) {
                mapGrid[x, y] = new TileData { GridPosition = new Vector2Int(x, y), CurrentType = TileType.Empty };
            }
            
        }

        // 2. scatter player castles FIRST
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

        // 3. scatter outposts (if turns yellow well... nothing we can do) 
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

        // 4. generate roads but SKIPP CASTLES
        GenerateRoadNetwork();

        // 5. scatter forests and mines ONLY on remaining empty tiles
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

            if (mapGrid[x, y].CurrentType == TileType.Empty &&
                IsFarEnough(pos))
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

    // ugly asf but ok for now
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
        // 1. forests
        for (int i = 0; i < forestClusterCount; i++)
        {
            GenerateForestCluster();
        }

        // 2. mines
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
        if (seed == new Vector2Int(-1, -1)) return; // no space for the forest

        List<Vector2Int> openList = new List<Vector2Int> { seed };
        int tilesSpawned = 0;

        // forests grows until goal size
        while (openList.Count > 0 && tilesSpawned < forestClusterSize)
        {
            int randomIndex = Random.Range(0, openList.Count);
            
            Vector2Int currentTile = openList[randomIndex];
            openList.RemoveAt(randomIndex);

            if (mapGrid[currentTile.x, currentTile.y].CurrentType == TileType.Empty)
            {
                mapGrid[currentTile.x, currentTile.y].CurrentType = TileType.Forest;
                tilesSpawned++;

                List<Vector2Int> neighbors = GetNeighbors(currentTile);
                
                foreach (var neighbor in neighbors)
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
                if (owner == 1) return Color.red;
                if (owner == 2) return Color.red;
                return Color.yellow;
            
            case TileType.Outpost: return Color.magenta;
            case TileType.Road: return Color.saddleBrown;
            case TileType.Forest: return Color.darkGreen;
            case TileType.Mine: return Color.black;
            
            default: return Color.lawnGreen;
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

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        var emptyPath = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        if (mapGrid == null) return emptyPath;

        queue.Enqueue(startPos);
        visited.Add(startPos);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == targetPos) break;

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;
                if (!IsWalkable(neighbor)) continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        if (startPos != targetPos && !cameFrom.ContainsKey(targetPos)) return emptyPath;
        var path = new List<Vector2Int>();
        Vector2Int pathCell = targetPos;
        while (pathCell != startPos)
        {
            path.Add(pathCell);
            pathCell = cameFrom[pathCell];
        }

        path.Reverse();
        return path;
    }

    public Vector2Int GetCastlePosition(int playerId) 
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = mapGrid[x, y];
                if (tile.CurrentType == TileType.Castle && tile.OwnerPlayerId == playerId) 
                    return new Vector2Int(x, y); 
            }
        }

        return new Vector2Int(-1, -1);
    }

    private bool IsWalkable(Vector2Int pos)
    {
        TileType tileType = mapGrid[pos.x, pos.y].CurrentType;
        return tileType != TileType.Forest && tileType != TileType.Mine;
    }

    public Vector3 GridToWorld(Vector2Int cell, float height = 0.35f)
    { // same as in InstantiateDebugGrid
        float offsetX = (mapWidth * tileSize) / 2f;
        float offsetZ = (mapHeight * tileSize) / 2f;
        float worldX = (cell.x * tileSize) - offsetX + (tileSize / 2f);
        float worldZ = (cell.y * tileSize) - offsetZ + (tileSize / 2f);
        return new Vector3(worldX, height, worldZ);

    }
}