using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public class EnemyMove : MonoBehaviour {
        [SerializeField] private MapGenerator mapGenerator;

        [Header("Dynamic Targeting")]
        [SerializeField] private int targetPlayerId = 1;
        private GameObject _targetCastleObject;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float heightAboveGround = 0.35f;

        private IEnumerator Start() {
            yield return null; 

            if (mapGenerator == null) yield break;

            if (_targetCastleObject == null)
            {
                _targetCastleObject = mapGenerator.GetCastleObject(targetPlayerId);
            }

            if (_targetCastleObject == null)
            {
                Debug.LogError($"Enemy could not find a valid castle object for Player {targetPlayerId}!");
                yield break;
            }

            Vector2Int startCell = mapGenerator.GetCastlePosition(0);
        
            Vector2Int targetCell = WorldToGrid(_targetCastleObject.transform.position);

            if (startCell.x < 0 || targetCell.x < 0) yield break;
        
            List<Vector2Int> path = mapGenerator.FindPath(startCell, targetCell);
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

        public void InitializeTarget(MapGenerator generator, GameObject opponentCastle)
        {
            this.mapGenerator = generator;
            this._targetCastleObject = opponentCastle;
        }

        // translate a physical object's 3D position back to its grid cell 
        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float offsetX = (mapGenerator.mapWidth * mapGenerator.tileSize) / 2f;
            float offsetZ = (mapGenerator.mapHeight * mapGenerator.tileSize) / 2f;

            int x = Mathf.FloorToInt((worldPos.x + offsetX) / mapGenerator.tileSize);
            int y = Mathf.FloorToInt((worldPos.z + offsetZ) / mapGenerator.tileSize);

            return new Vector2Int(x, y);
        }
    }
}