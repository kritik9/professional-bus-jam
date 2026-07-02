using System.Collections;
using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Grid;
using ElevatorGame.Events;

namespace ElevatorGame.Characters
{
    public class Character : MonoBehaviour
    {
        public CharacterColor Color { get; private set; }
        public MoveDirection Direction { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        
        private GridManager _gridManager;
        private bool _isMoving = false;

        public void Initialize(CharacterColor color, MoveDirection dir, Vector2Int startPos)
        {
            Color = color;
            Direction = dir;
            GridPosition = startPos;
            
            _gridManager = DependencyManager.Instance.Resolve<GridManager>();
            
            // Set initial position and register to cell
            if (_gridManager != null)
            {
                transform.position = _gridManager.GetWorldPosition(startPos) + Vector3.up * 0.5f;
                GridTile cell = _gridManager.GetCell(startPos);
                if (cell != null) cell.SetCharacter(this);
            }
            else
            {
                Debug.LogError("Character could not find GridManager via DependencyManager!");
            }
            
            // Note: Visual updates (color/arrow) should be hooked up here or in a separate view component
        }

        // Call this when the player clicks/taps the character
        public void OnTapped()
        {
            if (_isMoving) return;

            if (_gridManager != null && _gridManager.IsPathClear(GridPosition, Direction))
            {
                StartCoroutine(MoveRoutine());
            }
            else
            {
                Debug.Log($"Path is blocked for {Color} character at {GridPosition}");
                // Optional: Play a "wobble" or "denied" animation here
            }
        }

        private IEnumerator MoveRoutine()
        {
            _isMoving = true;
            
            // Clear current cell immediately so others can move through/into it if needed
            GridTile currentCell = _gridManager.GetCell(GridPosition);
            if (currentCell != null) currentCell.ClearCharacter();
            
            Vector2Int dirVector = GridManager.GetDirectionVector(Direction);
            Vector2Int currentLogicPos = GridPosition;
            
            while (true)
            {
                currentLogicPos += dirVector;
                GridTile nextCell = _gridManager.GetCell(currentLogicPos);
                
                Vector3 targetWorldPos;
                if (nextCell == null)
                {
                    // Moving off the grid. Calculate a point slightly outside the board.
                    targetWorldPos = transform.position + new Vector3(dirVector.x, 0, dirVector.y) * 1.5f; 
                }
                else
                {
                    targetWorldPos = nextCell.transform.position + Vector3.up * 0.5f;
                }

                // Move smoothly to the next position tile
                while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetWorldPos;

                if (nextCell == null)
                {
                    // Character has successfully exited the grid!
                    GameEvents.OnCharacterReachedExit?.Invoke(this);
                    
                    // Hide character and stop moving. The SlotManager will take over from here.
                    gameObject.SetActive(false); 
                    break;
                }
            }
            
            _isMoving = false;
        }
    }
}
