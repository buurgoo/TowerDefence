using UnityEngine;

public enum TileType { Empty, Road, Forest, Mine, Castle, Outpost }

[System.Serializable]
public class TileData
{
    public Vector2Int GridPosition;
    public TileType CurrentType;
    public int OwnerPlayerId = -1;
    public bool IsSocketOccupied = false;
    public GameObject SpawnedObjectRef;
}