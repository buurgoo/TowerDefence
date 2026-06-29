using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMove : MonoBehaviour {
    [SerializeField] private MapGenerator mapGenerator;

    [Header("Path")]
    [SerializeField] private int targetPlayerId = 0;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float heightAboveGround = 0.35f;

    private IEnumerator Move() {
        if (mapGenerator == null) yield break;

        Vector2Int startCell = new Vector2Int(0, 0);
        Vector2Int targetCell = mapGenerator.GetCastlePosition(targetPlayerId);

        if (startCell.x < 0) yield break;
        List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell);
        if (path.Count == 0 && startCell != targetCell) yield break;

        transform.position = mapGenerator.GridToWorld(startCell, heightAboveGround); 

        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition = mapGenerator.GridToWorld(cell, heightAboveGround);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                    transform.position = Vector3.MoveTowards(
                        transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    yield return null;
                
                transform.position = targetPosition;
            }        
        }

    }
}
