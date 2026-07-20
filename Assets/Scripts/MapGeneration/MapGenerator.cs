using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MapGenerator : MonoBehaviour
{
    [Header("Generation Parameters")]
    [SerializeField] public GameObject castle;
    public int generationSeed = 42;

    [Header("Map Parameters")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float tileSize = 1f;

    private TileData[,] _mapGrid;
    public List<TowerTile> towers;
    
    public int playerCount = 2;
    public int neutralBasesCount = 3;
    public int minDistanceBetweenObjects = 5;

    private readonly List<Vector2Int> _criticalNodes = new List<Vector2Int>();
    private readonly List<Vector2Int> _spacedObjects = new List<Vector2Int>();

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject emptyTilePrefab;
    [SerializeField] private GameObject roadTilePrefab;
    [SerializeField] private GameObject forestTilePrefab;
    [SerializeField] private GameObject mineTilePrefab;
    [SerializeField] private GameObject castleTilePrefab;
    [SerializeField] private GameObject outpostTilePrefab;
    
    [Header("Structure Prefabs")]
    [SerializeField] private GameObject castleModelPrefab;

    [Header("Resource Settings")]
    [SerializeField] private int forestClusterCount = 5;
    [SerializeField] private int forestClusterSize = 10;
    [SerializeField, Range(0f, 1f)] private float mineSpawnChance = 0.005f;

    private Vector2Int[] _playerCastlePositions;

    public void GenerateMap()
    {
        Random.InitState(generationSeed);

        _mapGrid = new TileData[mapWidth, mapHeight];
        _criticalNodes.Clear();
        _spacedObjects.Clear();
        _playerCastlePositions = new Vector2Int[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            _playerCastlePositions[i] = new Vector2Int(-1, -1);
        }

        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                _mapGrid[x, y] = new TileData { GridPosition = new Vector2Int(x, y), CurrentType = TileType.Empty };
            }
        }

        bool isHostOrServer = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer);

        for (int i = 0; i < playerCount; i++) {
            Vector2Int pos = FindValidPosition();
            if (pos == new Vector2Int(-1, -1))
            {
                Debug.LogError("Failed to place castle.");
                continue;
            }
            buildTower(pos);
            _mapGrid[pos.x, pos.y].CurrentType = TileType.Castle;
            _mapGrid[pos.x, pos.y].OwnerPlayerId = i;
            _criticalNodes.Add(pos);
            _playerCastlePositions[i] = pos;
            
            if (isHostOrServer && castle != null)
            {
                Vector3 coords = GridToWorld(pos);
                var instance = Instantiate(castle, coords, Quaternion.identity);
                var instanceCastle = instance.GetComponent<Castle>();
                if (instanceCastle != null)
                {
                    instanceCastle.setPlayerId(i);
                    instanceCastle.setMaxHP(20);
                }
                if (instance.GetComponent<NetworkObject>() != null)
                {
                    instance.GetComponent<NetworkObject>().Spawn();
                }
            }
        }

        for (int i = 0; i < neutralBasesCount; i++)
        {
            Vector2Int pos = FindValidPosition();
            if (pos == new Vector2Int(-1, -1))
            {
                Debug.LogError("Failed to place outpost.");
                continue;
            }
            _mapGrid[pos.x, pos.y].CurrentType = TileType.Outpost;
            _criticalNodes.Add(pos);
        }

        GenerateRoadNetwork();
        ScatterResources();
        InstantiateMapTiles();
    }

    private void buildTower(Vector2Int pos)
    {
        Tower tower = gameObject.AddComponent(typeof(Tower)) as Tower;
        TileData castleTile = new TowerTile();
        TowerTile towerTile = castleTile as TowerTile;
        towerTile.tower = tower;
        _mapGrid[pos.x, pos.y] = castleTile;
        towers.Add(towerTile);
    }

    private void InstantiateMapTiles()
    {
        for (int x = 0; x < mapWidth; x++) 
        {
            for (int y = 0; y < mapHeight; y++) 
            {
                TileData tile = _mapGrid[x, y];
                GameObject prefabToSpawn = GetPrefabForType(tile.CurrentType);

                if (prefabToSpawn != null)
                {
                    Vector3 worldPos = GridToWorld(new Vector2Int(x, y), height: 0f); 
                    GameObject spawnedTile = Instantiate(prefabToSpawn, worldPos, Quaternion.identity, this.transform);
                    spawnedTile.name = $"Tile_{tile.CurrentType}_{x}_{y}";
                
                    tile.SpawnedObjectRef = spawnedTile;

                    if (tile.CurrentType == TileType.Castle)
                    {
                        if (castleModelPrefab != null)
                        {
                            Vector3 structurePos = GridToWorld(new Vector2Int(x, y), height: 0.2f); 
                            GameObject castleBuilding = Instantiate(castleModelPrefab, structurePos, Quaternion.identity, spawnedTile.transform);
                            castleBuilding.name = $"Castle_Structure_P{tile.OwnerPlayerId}";
                        }
                        else
                        {
                            Debug.LogWarning("You forgot to assign the castleModelPrefab in the Inspector!");
                        }
                    }
                }
            }
        }
    }

    private GameObject GetPrefabForType(TileType type)
    {
        switch (type) 
        {
            case TileType.Empty: return emptyTilePrefab;
            case TileType.Road: return roadTilePrefab;
            case TileType.Forest: return forestTilePrefab;
            case TileType.Mine: return mineTilePrefab;
            case TileType.Castle: return castleTilePrefab;
            case TileType.Outpost: return outpostTilePrefab;
            default: return emptyTilePrefab;
        }
    }

    private Vector2Int FindValidPosition()
    {
        int attempts = 0;
        while (attempts < 500)
        {
            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);
            Vector2Int pos = new Vector2Int(x, y);

            if (_mapGrid[x, y].CurrentType == TileType.Empty && IsFarEnough(pos))
            {
                _spacedObjects.Add(pos);
                return pos;
            }
            attempts++;
        }
        return new Vector2Int(-1, -1);
    }

    private void GenerateRoadNetwork()
    {
        for (int i = 0; i < _criticalNodes.Count - 1; i++) {
            CreateLineRoad(_criticalNodes[i], _criticalNodes[i + 1]);
        }
    }

    private void CreateLineRoad(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        while (current.x != end.x) {
            current.x += (end.x > current.x) ? 1 : -1;
            if (_mapGrid[current.x, current.y].CurrentType == TileType.Empty)
                _mapGrid[current.x, current.y].CurrentType = TileType.Road;
        }
        while (current.y != end.y) {
            current.y += (end.y > current.y) ? 1 : -1;
            if (_mapGrid[current.x, current.y].CurrentType == TileType.Empty)
                _mapGrid[current.x, current.y].CurrentType = TileType.Road;
        }
    }

    private void ScatterResources()
    {
        for (int i = 0; i < forestClusterCount; i++)
        {
            GenerateForestCluster();
        }

        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                if (_mapGrid[x, y].CurrentType == TileType.Empty) {
                    if (Random.value < mineSpawnChance)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsFarEnough(pos))
                        {
                            _mapGrid[x, y].CurrentType = TileType.Mine;
                            _spacedObjects.Add(pos);
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

            if (_mapGrid[currentTile.x, currentTile.y].CurrentType == TileType.Empty)
            {
                _mapGrid[currentTile.x, currentTile.y].CurrentType = TileType.Forest;
                tilesSpawned++;

                List<Vector2Int> neighbors = GetNeighbors(currentTile);
                foreach (var neighbor in neighbors)
                {
                    if (_mapGrid[neighbor.x, neighbor.y].CurrentType == TileType.Empty && !openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }
    }

    private Vector2Int GetRandomEmptyTile()
    {
        int attempts = 0;
        while (attempts < 300)
        {
            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);
            if (_mapGrid[x, y].CurrentType == TileType.Empty) return new Vector2Int(x, y);
            attempts++;
        }
        return new Vector2Int(-1, -1);
    }

    private bool IsFarEnough(Vector2Int pos)
    {
        float minDistSq = minDistanceBetweenObjects * minDistanceBetweenObjects;
        foreach (var other in _spacedObjects)
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

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos, bool onlyRoad)
    {
        var emptyPath = new List<Vector2Int>();
        if (_mapGrid == null) return emptyPath;
        if (startPos == targetPos) return emptyPath;

        List<Vector2Int> openSet = new List<Vector2Int> { startPos };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float> { [startPos] = 0 };
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float> { [startPos] = GetManhattanDistance(startPos, targetPos) };

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet[0];
            float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
            int lowestIndex = 0;

            for (int i = 1; i < openSet.Count; i++)
            {
                float score = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
                if (score < lowestF)
                {
                    lowestF = score;
                    current = openSet[i];
                    lowestIndex = i;
                }
            }

            if (current == targetPos)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.RemoveAt(lowestIndex);
            closedSet.Add(current);

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor)) continue;

                if (onlyRoad)
                {
                    TileType type = _mapGrid[neighbor.x, neighbor.y].CurrentType;
                    if (type != TileType.Road && neighbor != targetPos && neighbor != startPos) continue;
                }
                else if (!IsWalkable(neighbor))
                {
                    continue;
                }

                float tentativeGScore = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + GetManhattanDistance(neighbor, targetPos);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return emptyPath;
    }

    private float GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> totalPath = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }

    public Vector2Int GetCastlePosition(int playerId) 
    {
        if (_playerCastlePositions != null && playerId >= 0 && playerId < _playerCastlePositions.Length)
        {
            if (_playerCastlePositions[playerId] != new Vector2Int(-1, -1))
                return _playerCastlePositions[playerId];
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = _mapGrid[x, y];
                if (tile.CurrentType == TileType.Castle && tile.OwnerPlayerId == playerId) 
                    return new Vector2Int(x, y); 
            }
        }
        return new Vector2Int(-1, -1);
    }

    private bool IsWalkable(Vector2Int pos)
    {
        TileType tileType = _mapGrid[pos.x, pos.y].CurrentType;
        return tileType != TileType.Forest && tileType != TileType.Mine;
    }

    public Vector3 GridToWorld(Vector2Int cell, float height = 0.35f)
    {
        float offsetX = (mapWidth * tileSize) / 2f;
        float offsetZ = (mapHeight * tileSize) / 2f;
        float worldX = (cell.x * tileSize) - offsetX + (tileSize / 2f);
        float worldZ = (cell.y * tileSize) - offsetZ + (tileSize / 2f);
        return new Vector3(worldX, height, worldZ);
    }

    public TileData GetTileDataAt(int x, int y)
    {
        if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
            return _mapGrid[x, y];
        return null;
    }

    public TileData GetTileDataAt(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight)
            return _mapGrid[pos.x, pos.y];
        return null;
    }

    public GameObject GetCastleObject(int playerId)
    {
        if (_mapGrid == null) return null;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = _mapGrid[x, y];
                if (tile.CurrentType == TileType.Castle && tile.OwnerPlayerId == playerId)
                {
                    return tile.SpawnedObjectRef; 
                }
            }
        }
        return null;
    }

    public List<Vector2Int> GetTowerPosBtwPlayers(int playerId, int targetPlayerId)
    {
        var availablePositions = new List<Vector2Int>();
        var addedPositions = new HashSet<Vector2Int>();

        if (_mapGrid == null) return availablePositions;

        Vector2Int aiCastlePosition = GetCastlePosition(playerId);
        Vector2Int enemyCastlePosition = GetCastlePosition(targetPlayerId);

        if (aiCastlePosition == new Vector2Int(-1, -1) || enemyCastlePosition == new Vector2Int(-1, -1))
            return availablePositions;

        List<Vector2Int> roadPath = FindPath(aiCastlePosition, enemyCastlePosition, onlyRoad: true);

        foreach (Vector2Int roadCell in roadPath)
        {
            foreach (Vector2Int neighbor in GetNeighbors(roadCell))
            {
                if (_mapGrid[neighbor.x, neighbor.y].CurrentType != TileType.Empty) continue;
                if (addedPositions.Add(neighbor)) availablePositions.Add(neighbor);
            }
        }
        return availablePositions;
    }
}