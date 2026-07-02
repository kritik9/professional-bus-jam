using System.Collections.Generic;
using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Data;

namespace ElevatorGame.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Setup")]
        [SerializeField] private GridTile cellPrefab;
        [SerializeField] private Transform gridParent;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellSpacing = 0.1f;

        private GridTile[,] _grid;
        private int _rows;
        private int _columns;

        private void Start()
        {
            DependencyManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<GridManager>();
        }

        /// <summary>
        /// Instantiates the grid based on the current LevelData.
        /// </summary>
        public void GenerateGrid()
        {
            LevelController levelController = DependencyManager.Instance.Resolve<LevelController>();
            if (levelController == null) return;
            
            LevelData levelData = levelController.GetCurrentLevel();
            if (levelData == null) return;

            _rows = levelData.rows;
            _columns = levelData.columns;
            _grid = new GridTile[_columns, _rows]; 

            ClearGrid();

            float step = cellSize + cellSpacing;
            // Center the grid at world origin
            Vector3 origin = new Vector3(-(_columns - 1) * step / 2f, 0, -(_rows - 1) * step / 2f);

            for (int x = 0; x < _columns; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    Vector3 pos = origin + new Vector3(x * step, 0, y * step);
                    GridTile cell = Instantiate(cellPrefab, pos, Quaternion.identity, gridParent);
                    cell.name = $"Cell_{x}_{y}";
                    cell.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                    cell.Initialize(new Vector2Int(x, y));
                    _grid[x, y] = cell;
                }
            }
        }

        public void ClearGrid()
        {
            foreach (Transform child in gridParent)
            {
                Destroy(child.gameObject);
            }
        }

        public GridTile GetCell(Vector2Int position)
        {
            if (position.x >= 0 && position.x < _columns && position.y >= 0 && position.y < _rows)
            {
                return _grid[position.x, position.y];
            }
            return null;
        }

        /// <summary>
        /// Checks if a character can move in a given direction until they exit the board.
        /// An exit is guaranteed if no obstacles or other characters block the path to the edge.
        /// </summary>
        public bool IsPathClear(Vector2Int startPos, MoveDirection direction)
        {
            Vector2Int currentPos = startPos;
            Vector2Int dirVector = GetDirectionVector(direction);
            
            while (true)
            {
                currentPos += dirVector;
                GridTile cell = GetCell(currentPos);
                
                // If we went out of bounds, the path is clear to the exit
                if (cell == null)
                    return true;
                
                // If there's another character, path is blocked
                if (cell.IsOccupied)
                    return false;
            }
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            GridTile cell = GetCell(gridPos);
            return cell != null ? cell.transform.position : Vector3.zero;
        }

        public static Vector2Int GetDirectionVector(MoveDirection dir)
        {
            return dir switch
            {
                MoveDirection.Up => new Vector2Int(0, 1),
                MoveDirection.Down => new Vector2Int(0, -1),
                MoveDirection.Left => new Vector2Int(-1, 0),
                MoveDirection.Right => new Vector2Int(1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}
