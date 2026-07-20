//using System.Collections;
//using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class TowerTile : TileData
{
    public int attackRange = 2;
    public int attackDamage = 20;
    public float attackCooldown = 1f;

    public Tower tower;
}
