using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Data;
using ElevatorGame.Grid;

namespace ElevatorGame.Characters
{
    public class CharacterManager : MonoBehaviour
    {
        [Header("Character Setup")]
        [SerializeField] private Character characterPrefab;
        [SerializeField] private Transform charactersParent;

        private void Start()
        {
            DependencyManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<CharacterManager>();
        }

        public void SpawnCharacters()
        {
            LevelController levelController = DependencyManager.Instance.Resolve<LevelController>();
            if (levelController == null) return;

            LevelData levelData = levelController.GetCurrentLevel();
            if (levelData == null) return;

            GridManager gridManager = DependencyManager.Instance.Resolve<GridManager>();
            if (gridManager == null) return;

            ClearCharacters();

            foreach (var charData in levelData.initialCharacters)
            {
                Character newChar = Instantiate(characterPrefab, charactersParent);
                newChar.Initialize(charData.color, charData.moveDirection, charData.position);
            }
        }

        private void ClearCharacters()
        {
            foreach (Transform child in charactersParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
