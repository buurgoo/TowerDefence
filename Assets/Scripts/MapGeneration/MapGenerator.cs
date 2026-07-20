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
    public List<TowerTile> towers = new List<TowerTile>();
    
    public int playerCount = 2;
    public int minDistanceBetweenObjects = 5;

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

    public void GenerateMap()
    {
        Random.InitState(generationSeed);
        InitializeGrid();

        // 1. Spawn forests and mines FIRST so they act as road obstacles
        GenerateForests();
        GenerateMines();

        // 2. Spawn Castles randomly on opposite sides
        GenerateOppositeCastles();

        // 3. Connect them with a winding road that navigates around decorations
        GenerateWindingRoad();

        // 4. Instantiation Phase
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
            int startX = Random.Range(1, mapWidth - 1);
            int startY = Random.Range(1, mapHeight - 1);
            
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
                        if (Random.value < 0.75f) 
                        {
                            openSet.Enqueue(neighbor);
                        }
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
            int x = Random.Range(1, mapWidth - 1);
            int y = Random.Range(1, mapHeight - 1);

            if (_mapGrid[x, y].CurrentType == TileType.Empty)
            {
                _mapGrid[x, y].CurrentType = TileType.Mine;
                placedMines++;
            }
        }
    }

    private void GenerateOppositeCastles()
    {
        // Player 0 Castle: Left side boundary zone
        int p0X = Random.Range(1, 3);
        int p0Y = Random.Range(2, mapHeight - 2);
        _castle0Pos = new Vector2Int(p0X, p0Y);

        // Player 1 Castle: Right side boundary zone
        int p1X = Random.Range(mapWidth - 3, mapWidth - 1);
        int p1Y = Random.Range(2, mapHeight - 2);
        _castle1Pos = new Vector2Int(p1X, p1Y);

        _mapGrid[_castle0Pos.x, _castle0Pos.y].CurrentType = TileType.Castle;
        _mapGrid[_castle0Pos.x, _castle0Pos.y].OwnerPlayerId = 0;

        _mapGrid[_castle1Pos.x, _castle1Pos.y].CurrentType = TileType.Castle;
        _mapGrid[_castle1Pos.x, _castle1Pos.y].OwnerPlayerId = 1;
    }

    private void GenerateWindingRoad()
    {
        // Internal generation path call using the custom boolean signature
        List<Vector2Int> roadPath = FindPathGeneration(_castle0Pos, _castle1Pos, onlyEmptyOrRoad: true);

        if (roadPath == null || roadPath.Count == 0)
        {
            Debug.LogWarning("Obstacles completely blocked the path! Generating fallback road.");
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
                    if (type != TileType.Empty && type != TileType.Road && neighbor != end)
                        continue;
                }
                
                if (onlyRoad)
                {
                    if (type != TileType.Road && type != TileType.Castle)
                        continue;
                }

                float tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    float fScore = tentativeGScore + Vector2Int.Distance(neighbor, end);
                    openSet.Enqueue(neighbor, fScore);
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

                if (!addedPositions.Contains(neighbor))
                {
                    addedPositions.Add(neighbor);
                    availablePositions.Add(neighbor);
                }
            }
        }
        return availablePositions;
    }

    private void InstantiateMapObjects()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = _mapGrid[x, y];
                Vector3 worldPos = GridToWorld(tile.GridPosition, 0);
                GameObject tileObj = null;

                switch (tile.CurrentType)
                {
                    case TileType.Empty:
                        tileObj = Instantiate(emptyTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.Road:
                        tileObj = Instantiate(roadTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.Forest:
                        tileObj = Instantiate(forestTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.Mine:
                        tileObj = Instantiate(mineTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.Castle:
                        tileObj = Instantiate(castleTilePrefab, worldPos, Quaternion.identity, transform);
                        if (castleModelPrefab != null)
                        {
                            GameObject model = Instantiate(castleModelPrefab, worldPos, Quaternion.identity, tileObj.transform);
                            Castle castleComponent = model.GetComponent<Castle>();
                            if (castleComponent != null)
                            {
                                castleComponent.setPlayerId(tile.OwnerPlayerId);
                                castleComponent.setMaxHP(1000);
                                tile.SpawnedObjectRef = model; 
                            }
                        }
                        break;
                }
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos, float height)
    {
        return new Vector3(gridPos.x * tileSize, height, gridPos.y * tileSize);
    }

    public Vector2Int GetCastlePosition(int playerId)
    {
        return playerId == 0 ? _castle0Pos : _castle1Pos;
    }

    public TileData GetTileDataAt(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight)
        {
            return _mapGrid[gridPos.x, gridPos.y];
        }
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

    private class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
    {
        private List<System.Tuple<TElement, TPriority>> elements = new List<System.Tuple<TElement, TPriority>>();

        public int Count => elements.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            elements.Add(System.Tuple.Create(element, priority));
        }

        public TElement Dequeue()
        {
            int bestIndex = 0;
            for (int i = 1; i < elements.Count; i++)
            {
                if (elements[i].Item2.CompareTo(elements[bestIndex].Item2) < 0)
                {
                    bestIndex = i;
                }
            }
            TElement bestItem = elements[bestIndex].Item1;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }
}