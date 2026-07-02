using UnityEngine;

namespace ElevatorGame.Grid
{
    public class GridTile : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }
        
        // We will define Character later
        public MonoBehaviour OccupyingCharacter { get; private set; }

        public void Initialize(Vector2Int position)
        {
            GridPosition = position;
        }

        public void SetCharacter(MonoBehaviour character)
        {
            OccupyingCharacter = character;
        }

        public void ClearCharacter()
        {
            OccupyingCharacter = null;
        }

        public bool IsOccupied => OccupyingCharacter != null;
    }
}
