using System.Collections.Generic;
using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Data;
using ElevatorGame.Events;
using ElevatorGame.Characters;
using ElevatorGame.Slots;
using System.Collections;

namespace ElevatorGame.Elevators
{
    public class ElevatorManager : MonoBehaviour
    {
        [Header("Elevator Setup")]
        [SerializeField] private Elevator elevatorPrefab;
        [SerializeField] private Transform elevatorParent;
        [SerializeField] private int maxActiveElevators = 3;
        [SerializeField] private float elevatorSpacing = 2.5f;
        [Tooltip("Margin to keep from the grid edges")]
        [SerializeField] private float marginFromGrid = 1.6f;

        private Queue<ElevatorData> _elevatorQueue = new Queue<ElevatorData>();
        private Elevator[] _activeElevators;
        
        private SlotManager _slotManager;

        private void Start()
        {
            DependencyManager.Instance.Register(this);
            GameEvents.OnSlotFilled += HandleSlotFilled;
            _activeElevators = new Elevator[maxActiveElevators];
        }

        private void OnDestroy()
        {
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.Unregister<ElevatorManager>();
                
            GameEvents.OnSlotFilled -= HandleSlotFilled;
        }

        public void GenerateElevators()
        {
            _slotManager = DependencyManager.Instance.Resolve<SlotManager>();
            
            LevelController levelController = DependencyManager.Instance.Resolve<LevelController>();
            if (levelController == null) return;
            
            LevelData levelData = levelController.GetCurrentLevel();
            if (levelData == null) return;
            
            _elevatorQueue.Clear();
            foreach (var ed in levelData.elevatorSequence)
            {
                _elevatorQueue.Enqueue(ed);
            }
            
            ClearElevators();
            
            var gridManager = DependencyManager.Instance.Resolve<ElevatorGame.Grid.GridManager>();
            if (gridManager != null)
            {
                bool placeAboveGrid = elevatorParent.position.z >= 0;
                float targetZ = placeAboveGrid ? gridManager.GetGridTopZ() + marginFromGrid : gridManager.GetGridBottomZ() - marginFromGrid;
                elevatorParent.position = new Vector3(elevatorParent.position.x, elevatorParent.position.y, targetZ);
            }

            for (int i = 0; i < Mathf.Min(maxActiveElevators, _elevatorQueue.Count); i++)
            {
                SpawnElevatorAtSlot(i);
            }
            
            CheckAllSlots();
        }

        private void ClearElevators()
        {
            for (int i = 0; i < _activeElevators.Length; i++)
            {
                if (_activeElevators[i] != null)
                {
                    Destroy(_activeElevators[i].gameObject);
                    _activeElevators[i] = null;
                }
            }
        }

        private void SpawnElevatorAtSlot(int index)
        {
            if (_elevatorQueue.Count == 0) return;
            
            ElevatorData data = _elevatorQueue.Dequeue();
            
            // ElevatorParent is already placed at the correct dynamic world Z in GenerateElevators.
            // We just need to center them locally along X.
            float totalWidth = (maxActiveElevators - 1) * elevatorSpacing;
            Vector3 startPos = new Vector3(-totalWidth / 2f, 0, 0);
            Vector3 pos = startPos + new Vector3(index * elevatorSpacing, 0, 0);
            
            Elevator el = Instantiate(elevatorPrefab, elevatorParent);
            el.transform.localPosition = pos;
            el.transform.localRotation = Quaternion.identity;
            el.Initialize(data, index);
            el.OnElevatorFull += HandleElevatorFull;
            _activeElevators[index] = el;
        }

        private void HandleSlotFilled(MonoBehaviour characterMono)
        {
            Character character = characterMono as Character;
            if (character == null) return;
            
            TryBoardCharacter(character);
        }
        
        private void CheckAllSlots()
        {
            if (_slotManager == null) return;
            
            List<Character> waitingCharacters = _slotManager.GetCharactersInSlots();
            foreach (var character in waitingCharacters)
            {
                TryBoardCharacter(character);
            }
        }

        private bool TryBoardCharacter(Character character)
        {
            foreach (var elevator in _activeElevators)
            {
                if (elevator != null && elevator.TryBoard(character))
                {
                    _slotManager.RemoveCharacterFromSlot(character);
                    return true;
                }
            }
            return false;
        }

        public bool TryBoardCharacterDirectly(Character character)
        {
            foreach (var elevator in _activeElevators)
            {
                if (elevator != null && elevator.TryBoard(character))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleElevatorFull(Elevator elevator)
        {
            elevator.OnElevatorFull -= HandleElevatorFull;
            int index = elevator.SlotIndex;
            
            elevator.Depart(() => 
            {
                _activeElevators[index] = null;
                SpawnElevatorAtSlot(index);
                
                // Check if any waiting characters can board the new elevator
                StartCoroutine(CheckSlotsNextFrame());
            });
        }
        
        private IEnumerator CheckSlotsNextFrame()
        {
            yield return null;
            CheckAllSlots();
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            // If elevator queue is empty and no active elevators remain, we might have won.
            // Also need to check if grid is empty and slots are empty.
            bool elevatorsDone = _elevatorQueue.Count == 0;
            if (elevatorsDone)
            {
                foreach (var el in _activeElevators)
                {
                    if (el != null) elevatorsDone = false;
                }
            }

            if (elevatorsDone)
            {
                // Level won! (Grid should be empty if elevators are done correctly, but can verify)
                GameEvents.OnLevelWon?.Invoke();
            }
        }
    }
}
