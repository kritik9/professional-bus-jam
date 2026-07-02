using UnityEngine;
using ElevatorGame.Data;
using ElevatorGame.Grid;
using ElevatorGame.Slots;

namespace ElevatorGame.Core
{
    public class LevelController : MonoBehaviour
    {
        [Header("Level Setup")]
        [SerializeField] private LevelData testLevelData;

        private void Start()
        {
            DependencyManager.Instance.Register(this);
            
            // We wait a tiny bit to ensure all other managers (Grid, Slot, Camera) have finished Awake/Start and registered themselves
            Invoke(nameof(InitializeLevel), 0.1f);
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<LevelController>();
        }

        public LevelData GetCurrentLevel()
        {
            return testLevelData;
        }

        public void InitializeLevel()
        {
            if (testLevelData == null)
            {
                Debug.LogWarning("LevelController has no LevelData assigned!");
                return;
            }

            // 1. Setup Grid
            var gridManager = DependencyManager.Instance.Resolve<GridManager>();
            if (gridManager != null) gridManager.GenerateGrid();

            // 2. Setup Slots
            var slotManager = DependencyManager.Instance.Resolve<SlotManager>();
            if (slotManager != null) slotManager.GenerateSlots();

            // 3. Setup Camera
            var cameraController = DependencyManager.Instance.Resolve<CameraController>();
            if (cameraController != null) cameraController.SetupCamera();
            
            Debug.Log($"Level {testLevelData.name} successfully initialized by LevelController!");
        }
    }
}
