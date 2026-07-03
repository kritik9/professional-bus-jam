using System.Collections.Generic;
using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Events;
using ElevatorGame.Data;
using ElevatorGame.Characters;

namespace ElevatorGame.Slots
{
    public class SlotManager : MonoBehaviour
    {
        [Header("Slot Setup")]
        [SerializeField] private BufferSlot slotPrefab;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private float slotSpacing = 1.2f;
        [Tooltip("Margin to keep from the grid edges")]
        [SerializeField] private float marginFromGrid = 1.0f;

        private List<BufferSlot> _slots = new List<BufferSlot>();

        private void Start()
        {
            DependencyManager.Instance.Register(this);
            GameEvents.OnCharacterReachedExit += HandleCharacterArrived;
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<SlotManager>();
                
            GameEvents.OnCharacterReachedExit -= HandleCharacterArrived;
        }

        public void GenerateSlots()
        {
            LevelController levelController = DependencyManager.Instance.Resolve<LevelController>();
            if (levelController == null) return;
            
            LevelData levelData = levelController.GetCurrentLevel();
            if (levelData == null) return;

            ClearSlots();

            int total = levelData.totalBufferSlots;
            int locked = levelData.initiallyLockedSlots;

            var gridManager = DependencyManager.Instance.Resolve<ElevatorGame.Grid.GridManager>();
            if (gridManager != null)
            {
                bool placeAboveGrid = slotsParent.position.z >= 0;
                float targetZ = placeAboveGrid ? gridManager.GetGridTopZ() + marginFromGrid : gridManager.GetGridBottomZ() - marginFromGrid;
                slotsParent.position = new Vector3(slotsParent.position.x, slotsParent.position.y, targetZ);
            }

            // Center slots horizontally (local to the parent)
            float totalWidth = (total - 1) * slotSpacing;
            Vector3 startPos = new Vector3(-totalWidth / 2f, 0, 0);

            for (int i = 0; i < total; i++)
            {
                BufferSlot slot = Instantiate(slotPrefab, slotsParent);
                slot.transform.localPosition = startPos + new Vector3(i * slotSpacing, 0, 0);
                
                // Typically the last slots on the right are the locked ones
                bool isLocked = i >= (total - locked);
                slot.Initialize(isLocked);
                
                _slots.Add(slot);
            }
        }

        public void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _slots.Clear();
        }

        private void HandleCharacterArrived(MonoBehaviour characterMono)
        {
            Character character = characterMono as Character;
            if (character == null) return;

            BufferSlot availableSlot = GetFirstAvailableSlot();
            if (availableSlot != null)
            {
                availableSlot.SetCharacter(character);
                Debug.Log($"Character {character.Color} entered a slot!");
                
                // Notify the Elevator Manager to check if any elevators match this character
                GameEvents.OnSlotFilled?.Invoke(character);
            }
            else
            {
                // Overflow! No space left.
                Debug.LogWarning("Waiting slots overflow! Level Lost.");
                GameEvents.OnLevelLost?.Invoke();
            }
        }

        private BufferSlot GetFirstAvailableSlot()
        {
            foreach (var slot in _slots)
            {
                if (slot.IsAvailable) return slot;
            }
            return null;
        }

        /// <summary>
        /// Called by the ElevatorManager when a character successfully boards a lift.
        /// </summary>
        public void RemoveCharacterFromSlot(Character character)
        {
            foreach (var slot in _slots)
            {
                if (slot.OccupyingCharacter == character)
                {
                    slot.ClearCharacter();
                    GameEvents.OnSlotFreed?.Invoke(character);
                    break;
                }
            }
        }

        public List<Character> GetCharactersInSlots()
        {
            List<Character> list = new List<Character>();
            foreach (var slot in _slots)
            {
                if (slot.OccupyingCharacter != null)
                {
                    list.Add(slot.OccupyingCharacter);
                }
            }
            return list;
        }
    }
}
