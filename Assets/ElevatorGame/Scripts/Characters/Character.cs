using System.Collections;
using System.Collections.Generic;
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
        
        [Header("Arrow Settings")]
        [SerializeField] private Transform arrowTransform;
        
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
                transform.position = _gridManager.GetWorldPosition(startPos) + Vector3.up * 0.35f;
                GridTile cell = _gridManager.GetCell(startPos);
                if (cell != null) cell.SetCharacter(this);
            }
            else
            {
                Debug.LogError("Character could not find GridManager via DependencyManager!");
            }
            
            // Visual updates (color/arrow)
            ApplyColorToRenderers(color);
            UpdateArrowRotation(dir);
        }

        private void ApplyColorToRenderers(CharacterColor colorType)
        {
            UnityEngine.Color unityColor = GetUnityColor(colorType);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = unityColor;
                }
            }
        }

        private void UpdateArrowRotation(MoveDirection dir)
        {
            if (arrowTransform != null)
            {
                float zRot = dir switch
                {
                    MoveDirection.Up => 270f,
                    MoveDirection.Right => 180f, 
                    MoveDirection.Down => 90f,
                    MoveDirection.Left => 0f,
                    _ => 0f
                };
                
                arrowTransform.localRotation = Quaternion.Euler(90, 0, zRot);
            }
        }

        public void HideArrow()
        {
            if (arrowTransform != null)
            {
                arrowTransform.gameObject.SetActive(false);
            }
        }

        private UnityEngine.Color GetUnityColor(CharacterColor characterColor)
        {
            switch (characterColor)
            {
                case CharacterColor.Red: return UnityEngine.Color.red;
                case CharacterColor.Blue: return UnityEngine.Color.blue;
                case CharacterColor.Green: return UnityEngine.Color.green;
                case CharacterColor.Yellow: return UnityEngine.Color.yellow;
                case CharacterColor.Purple: return new UnityEngine.Color(0.5f, 0, 0.5f);
                case CharacterColor.Orange: return new UnityEngine.Color(1f, 0.64f, 0f);
                case CharacterColor.Pink: return new UnityEngine.Color(1f, 0.75f, 0.8f);
                case CharacterColor.Cyan: return UnityEngine.Color.cyan;
                case CharacterColor.Gray: return UnityEngine.Color.gray;
                case CharacterColor.White: return UnityEngine.Color.white;
                default: return UnityEngine.Color.white;
            }
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
            
            GridTile currentCell = _gridManager.GetCell(GridPosition);
            if (currentCell != null) currentCell.ClearCharacter();
            
            List<Vector2Int> path = _gridManager.CalculatePath(GridPosition, Direction);
            
            // Trace the exact clear grid path
            foreach (var pos in path)
            {
                GridTile nextCell = _gridManager.GetCell(pos);
                if (nextCell != null)
                {
                    Vector3 targetWorldPos = nextCell.transform.position + Vector3.up * 0.35f;
                    while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                        yield return null;
                    }
                    transform.position = targetWorldPos;
                }
            }
            
            // Final step out of the grid in their movement direction
            Vector2Int dirVector = GridManager.GetDirectionVector(Direction);
            Vector3 exitWorldPos = transform.position + new Vector3(dirVector.x, 0, dirVector.y) * 1.5f;
            while (Vector3.Distance(transform.position, exitWorldPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, exitWorldPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // Character has successfully exited the grid!
            var elevatorManager = DependencyManager.Instance.Resolve<ElevatorGame.Elevators.ElevatorManager>();
            bool boarded = false;
            if (elevatorManager != null)
            {
                boarded = elevatorManager.TryBoardCharacterDirectly(this);
            }
            
            if (!boarded)
            {
                GameEvents.OnCharacterReachedExit?.Invoke(this);
                // Do NOT disable the character, as the SlotManager takes over its movement and visibility!
            }
            
            _isMoving = false;
        }
    }
}
