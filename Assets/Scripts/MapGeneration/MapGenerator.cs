using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MapGenerator : NetworkBehaviour
{
    [Header("Generation Parameters")]
    [SerializeField] public GameObject castle; 
    [SerializeField] public GameObject towerPrefab;
    public int generationSeed = 42;

    [Header("Map Parameters")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float tileSize = 1f;

    private TileData[,] _mapGrid;
    public List<TowerTile> towers = new List<TowerTile>();
    
    public int playerCount = 2;
    public int minDistanceBetweenObjects = 5;
    [SerializeField] private int castleTerritoryRange = 8;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject emptyTilePrefab;
    [SerializeField] private GameObject roadTilePrefab;
    [SerializeField] private GameObject forestTilePrefab;
    [SerializeField] private GameObject mineTilePrefab;
    [SerializeField] private GameObject castleTilePrefab;
    
    [Header("Structure Prefabs")]
    [SerializeField] private GameObject castleModelPrefab;

[Header("Resource Settings (Bigger Forests)")]
    [SerializeField] private int forestClusterCount = 8; 
    [SerializeField] private int forestClusterSize = 18;  
    [SerializeField] private int mineCount = 4;

    private Vector2Int _castle0Pos = new Vector2Int(-1, -1);
    private Vector2Int _castle1Pos = new Vector2Int(-1, -1);

    /// <summary>
    /// Network RPC: Broadcasts the random map seed to ALL clients and triggers identical map generation.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    public void RegenerateMapRpc(int seed)
    {
        ClearExistingMap();
        generationSeed = seed;
        GenerateMap();
    }

    /// <summary>
    /// Destroys existing instantiated tile visuals before regenerating.
    /// </summary>
    private void ClearExistingMap()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        towers.Clear();
    }

    public void GenerateMap()
    {
        Random.InitState(generationSeed);
        InitializeGrid();
        GenerateForests();
        GenerateMines();
        GenerateDiagonalCastles();
        GenerateWindingRoad();
        InstantiateMapObjects();
    }

    private void InitializeGrid()
    {
        _mapGrid = new TileData[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                _mapGrid[x, y] = new TileData
                {
                    GridPosition = new Vector2Int(x, y),
                    CurrentType = TileType.Empty
                };
            }
        }
        towers.Clear();
    }

    private void GenerateForests()
    {
        for (int i = 0; i < forestClusterCount; i++)
        {
            int startX = Random.Range(mapWidth / 4, (3 * mapWidth) / 4);
            int startY = Random.Range(mapHeight / 4, (3 * mapHeight) / 4);
            
            Queue<Vector2Int> openSet = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            openSet.Enqueue(new Vector2Int(startX, startY));
            int cellsGrown = 0;

            while (openSet.Count > 0 && cellsGrown < forestClusterSize)
            {
                Vector2Int current = openSet.Dequeue();
                if (_mapGrid[current.x, current.y].CurrentType == TileType.Empty)
                {
                    _mapGrid[current.x, current.y].CurrentType = TileType.Forest;
                    cellsGrown++;
                }

                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor) && _mapGrid[neighbor.x, neighbor.y].CurrentType == TileType.Empty)
                    {
                        visited.Add(neighbor);
                        if (Random.value < 0.85f) openSet.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private void GenerateMines()
    {
        int placedMines = 0;
        int attempts = 0;
        while (placedMines < mineCount && attempts < 100)
        {
            attempts++;
            int x = Random.Range(2, mapWidth - 3);
            int y = Random.Range(2, mapHeight - 3);
            if (_mapGrid[x, y].CurrentType == TileType.Empty)
            {
                _mapGrid[x, y].CurrentType = TileType.Mine;
                placedMines++;
            }
        }
    }

    private void GenerateDiagonalCastles()
    {
        bool diagonalAxisFlip = Random.value > 0.5f;
        if (diagonalAxisFlip)
        {
            _castle0Pos = new Vector2Int(Random.Range(1, 4), Random.Range(mapHeight - 4, mapHeight - 1));
            _castle1Pos = new Vector2Int(Random.Range(mapWidth - 4, mapWidth - 1), Random.Range(1, 4));
        }
        else
        {
            _castle0Pos = new Vector2Int(Random.Range(1, 4), Random.Range(1, 4));
            _castle1Pos = new Vector2Int(Random.Range(mapWidth - 4, mapWidth - 1), Random.Range(mapHeight - 4, mapHeight - 1));
        }
        SetupCastleTile(_castle0Pos, 0);
        SetupCastleTile(_castle1Pos, 1);
    }

    private void SetupCastleTile(Vector2Int pos, int playerId)
    {
        bool isHostOrServer = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer);
    
        if (isHostOrServer && castle != null)
        {
            Vector3 worldCoords = GridToWorld(pos, height: 0.1f);
            GameObject instance = Instantiate(castle, worldCoords, Quaternion.identity);

            Tower tower = gameObject.AddComponent(typeof(Tower)) as Tower;
            TowerTile towerTile = new TowerTile
            {
                GridPosition = pos,
                CurrentType = TileType.Castle,
                OwnerPlayerId = playerId,
                IsSocketOccupied = true,
                SpawnedObjectRef = instance,
                tower = tower,
            };
            _mapGrid[pos.x, pos.y] = towerTile;
            towers.Add(towerTile);

            Castle instanceCastle = instance.GetComponent<Castle>();
            if (instanceCastle != null)
            {
                //TowerTile tile = _mapGrid[pos.x, pos.y] as TowerTile;
                //tile.towerObject = instance;
                instanceCastle.setPlayerId(playerId);
                instanceCastle.setMaxHP(20);
            }
        
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("Castle prefab is missing a NetworkObject component!");
            }

            Color playerColor;
            if (playerId == 0) { playerColor = new Color(1f, 0f, 0f); }
            else { playerColor = new Color(0f, 0f, 1f); }
            SetColor(playerColor, pos);
        }
    }

    public bool IsValidTowerPlacement(Vector2Int pos, int playerId)
    {
        if (_mapGrid == null) return false;

        if (pos.x < 0 || pos.x >= mapWidth || pos.y < 0 || pos.y >= mapHeight) return false;
    
        TileData tile = _mapGrid[pos.x, pos.y];
        if (tile == null || tile.CurrentType != TileType.Empty || tile.IsSocketOccupied) return false;

        Vector2Int castlePos = GetCastlePosition(playerId);
        if (castlePos == new Vector2Int(-1, -1)) return false;
        //if (Mathf.Abs(pos.x - castlePos.x) + Mathf.Abs(pos.y - castlePos.y) > castleTerritoryRange) return false;

        foreach (Vector2Int neighbor in GetNeighbors(pos))
        {
            TileData neighborTile = _mapGrid[neighbor.x, neighbor.y];
            if (neighborTile != null && neighborTile.CurrentType == TileType.Road)
            {
                return true;
            }
        }
        return false;
    }

    [Rpc(SendTo.Server)]
    public void PlaceTowerServerRpc(Vector2Int pos, int playerId)
    {
        if (!IsValidTowerPlacement(pos, playerId)) return;

        Debug.Log($"Server: Spawning tower at {pos} for Player {playerId}");

        if (towerPrefab != null)
        {
            Vector3 worldPos = GridToWorld(pos, height: 0.35f);
            GameObject spawnedTower = Instantiate(towerPrefab, worldPos, Quaternion.identity);

            NetworkObject netObj = spawnedTower.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(destroyWithScene: true);
            }
            else
            {
                Debug.LogError("MapGenerator: towerPrefab is missing a NetworkObject component!");
            }

            TowerTile towerTile = new TowerTile
            {
                GridPosition = pos,
                CurrentType = TileType.Tower,
                OwnerPlayerId = playerId,
                IsSocketOccupied = true,
                SpawnedObjectRef = spawnedTower
            };

            Tower towerComponent = spawnedTower.GetComponent<Tower>();
            if (towerComponent == null)
            {
                towerComponent = spawnedTower.AddComponent<Tower>();
            }
            towerComponent.createTower(range: 5, damage: 25, cooldown: 1.2f);

            towerTile.tower = towerComponent;
            _mapGrid[pos.x, pos.y] = towerTile;
            towers.Add(towerTile);

            Color playerColor;
            if (playerId == 0) { playerColor = new Color(1f, 0f, 0f); }
            else { playerColor = new Color(0f, 0f, 1f); }

            SetColor(playerColor, pos);
            NotifyTowerBuiltClientRpc(pos, playerId, netObj.NetworkObjectId);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyTowerBuiltClientRpc(Vector2Int pos, int playerId, ulong networkObjectId)
    {
        if (IsServer || IsHost) return;

        GameObject clientTowerObj = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            clientTowerObj = netObj.gameObject;
        }

        TowerTile towerTile = new TowerTile
        {
            GridPosition = pos,
            CurrentType = TileType.Tower,
            OwnerPlayerId = playerId,
            IsSocketOccupied = true,
            SpawnedObjectRef = clientTowerObj
        };

        if (clientTowerObj != null)
        {
            Tower towerComponent = clientTowerObj.GetComponent<Tower>();
            if (towerComponent == null)
            {
                towerComponent = clientTowerObj.AddComponent<Tower>();
            }
            towerTile.tower = towerComponent;
        }

        _mapGrid[pos.x, pos.y] = towerTile;
        towers.Add(towerTile);

        Color playerColor;
        if (playerId == 0) { playerColor = new Color(1f, 0f, 0f); }
        else { playerColor = new Color(0f, 0f, 1f); }

        SetColor(playerColor, pos);
    }

    private void SetColor(Color color, Vector2Int pos)
    {
        TowerTile tile = _mapGrid[pos.x, pos.y] as TowerTile;
        if (tile == null) { Debug.Log($"Tower Tile not found"); }
        bool isCastle = (tile.CurrentType == TileType.Tower) ? false : true;

        GameObject obj = tile.SpawnedObjectRef;
        if (obj == null) { Debug.Log($"Tower Object not found"); }
        if (!isCastle)
        {
            Renderer objectRenderer = obj.GetComponent<Renderer>();
            objectRenderer.material.SetColor("_BaseColor", color);
        }
        else
        {
            Renderer[] objectRenderers = obj.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < objectRenderers.Length; i++)
            {
                objectRenderers[i].material.SetColor("_BaseColor", color);
            }
        }
    }

    private void GenerateWindingRoad()
    {
        List<Vector2Int> roadPath = FindPathGeneration(_castle0Pos, _castle1Pos, onlyEmptyOrRoad: true);
        if (roadPath == null || roadPath.Count == 0)
        {
            roadPath = FindPathGeneration(_castle0Pos, _castle1Pos, onlyEmptyOrRoad: false);
        }

        foreach (Vector2Int cell in roadPath)
        {
            if (_mapGrid[cell.x, cell.y].CurrentType != TileType.Castle)
            {
                _mapGrid[cell.x, cell.y].CurrentType = TileType.Road;
            }
        }
    }

    private List<Vector2Int> FindPathGeneration(Vector2Int start, Vector2Int end, bool onlyEmptyOrRoad)
    {
        return FindPathCore(start, end, onlyEmptyOrRoad, onlyRoad: false);
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool onlyRoad = false)
    {
        return FindPathCore(start, end, onlyEmptyOrRoad: false, onlyRoad: onlyRoad);
    }

    private List<Vector2Int> FindPathCore(Vector2Int start, Vector2Int end, bool onlyEmptyOrRoad, bool onlyRoad)
    {
        PriorityQueue<Vector2Int, float> openSet = new PriorityQueue<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0;

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();
            if (current == end)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                while (current != start)
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Reverse();
                return path;
            }

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                TileType type = _mapGrid[neighbor.x, neighbor.y].CurrentType;
                if (onlyEmptyOrRoad)
                {
                    if (type != TileType.Empty && type != TileType.Road && neighbor != end) continue;
                }
                if (onlyRoad)
                {
                    if (type != TileType.Road && type != TileType.Castle) continue;
                }

                float tentativeGScore = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    openSet.Enqueue(neighbor, tentativeGScore + Vector2Int.Distance(neighbor, end));
                }
            }
        }
        return new List<Vector2Int>();
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

    private void InstantiateMapObjects()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = _mapGrid[x, y];
                Vector3 worldPos = GridToWorld(tile.GridPosition, 0f);
                GameObject tileObj = null;

                GameObject prefabToSpawn = GetPrefabForType(tile.CurrentType);
                if (prefabToSpawn != null)
                {
                    tileObj = Instantiate(prefabToSpawn, worldPos, Quaternion.identity, transform);
                    tileObj.name = $"Tile_{tile.CurrentType}_{x}_{y}";
                    tile.SpawnedObjectRef = tileObj;
                }

                /*if (tile.CurrentType == TileType.Castle && castleModelPrefab != null && tileObj != null)
                {
                    Vector3 structurePos = GridToWorld(tile.GridPosition, height: 0.2f);
                    GameObject model = Instantiate(castleModelPrefab, structurePos, Quaternion.identity, tileObj.transform);
                    model.name = $"Castle_Structure_P{tile.OwnerPlayerId}";
                }*/
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
            default: return emptyTilePrefab;
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos, float height = 0.35f)
    {
        float offsetX = (mapWidth * tileSize) / 2f;
        float offsetZ = (mapHeight * tileSize) / 2f;
        float worldX = (gridPos.x * tileSize) - offsetX + (tileSize / 2f);
        float worldZ = (gridPos.y * tileSize) - offsetZ + (tileSize / 2f);
        return new Vector3(worldX, height, worldZ);
    }

    public Vector2Int GetCastlePosition(int playerId) => playerId == 0 ? _castle0Pos : _castle1Pos;
    public TileData GetTileDataAt(Vector2Int gridPos) => (gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight) ? _mapGrid[gridPos.x, gridPos.y] : null;

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
            
                if (addedPositions.Add(neighbor)) 
                {
                    availablePositions.Add(neighbor);
                }
            }
        }
        return availablePositions;
    }

    private class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
    {
        private List<System.Tuple<TElement, TPriority>> elements = new List<System.Tuple<TElement, TPriority>>();
        public int Count => elements.Count;
        public void Enqueue(TElement element, TPriority priority) => elements.Add(System.Tuple.Create(element, priority));
        public TElement Dequeue()
        {
            int bestIndex = 0;
            for (int i = 1; i < elements.Count; i++)
                if (elements[i].Item2.CompareTo(elements[bestIndex].Item2) < 0) bestIndex = i;
            TElement bestItem = elements[bestIndex].Item1;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }
}