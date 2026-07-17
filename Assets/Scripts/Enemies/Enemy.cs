using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour {
    [SerializeField] 
    private MapGenerator mapGenerator;

    [Header("Health")]
    private int _maxHp = 0;
    private int _currentHp = 0;

    [Header("Target")]
    [SerializeField] 
    private int targetPlayerId = -1;

    [Header("Movement")]
    [SerializeField] 
    private float moveSpeed = 2f;
    [SerializeField] 
    private float heightAboveGround = 0.35f;

    public void SetTarget(int target) { targetPlayerId = target; }

    private IEnumerator Move() {
        yield return null;

        if (mapGenerator == null) yield break;
        
        Vector2Int startCell = mapGenerator.GetCastlePosition(0); 
        Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

        if (startCell.x < 0 || targetCell.x < 0) yield break;
    
        List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell, onlyRoad: true);
        Debug.Log("Enemy: path length = " + path.Count);
        if (path.Count == 0 && startCell != targetCell) yield break;

        transform.position = mapGenerator.GridToWorld(startCell, heightAboveGround); 

        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition = mapGenerator.GridToWorld(cell, heightAboveGround);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    targetPosition, 
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }
        }
    }
}
