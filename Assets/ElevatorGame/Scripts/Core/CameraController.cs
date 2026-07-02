using UnityEngine;
using ElevatorGame.Data;

namespace ElevatorGame.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Framing Settings")]
        [Tooltip("Extra space on the left and right of the grid.")]
        [SerializeField] private float marginX = 1f;
        
        [Tooltip("Extra space on the top and bottom. Set this higher to leave room for elevators and slots.")]
        [SerializeField] private float marginY = 4f; 
        
        [Tooltip("Shift the camera's focus point along the Z axis (up/down on screen). Positive values shift camera up to frame elevators better.")]
        [SerializeField] private float focusZOffset = 2f; 
        
        [SerializeField] private float minOrthoSize = 5f;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            DependencyManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<CameraController>();
        }

        public void SetupCamera()
        {
            LevelController levelController = DependencyManager.Instance.Resolve<LevelController>();
            if (levelController == null) return;
            
            LevelData levelData = levelController.GetCurrentLevel();
            if (levelData == null) return;

            if (targetCamera == null || !targetCamera.orthographic)
            {
                Debug.LogWarning("CameraController requires an Orthographic Camera!");
                return;
            }

            // Standard cell step size (matches GridManager defaults)
            float step = 1f + 0.1f; // cellSize + cellSpacing

            float gridWidth = levelData.columns * step;
            float gridHeight = levelData.rows * step;

            float requiredHalfHeight = (gridHeight / 2f) + marginY;
            float requiredHalfWidth = (gridWidth / 2f) + marginX;

            float requiredSize = Mathf.Max(requiredHalfHeight, requiredHalfWidth / targetCamera.aspect);
            targetCamera.orthographicSize = Mathf.Max(minOrthoSize, requiredSize);

            // Center the camera over the grid, but apply the focusZOffset to frame the UI/Elevators at the top
            Vector3 currentPos = targetCamera.transform.position;
            targetCamera.transform.position = new Vector3(0f, currentPos.y, focusZOffset);
        }
    }
}
