using System.Collections.Generic;
using UnityEngine;
using ElevatorGame.Core;

namespace ElevatorGame.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "ElevatorGame/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Grid Settings")]
        public int rows = 8;
        public int columns = 8;
        
        [Header("Initial Grid Layout")]
        [Tooltip("The characters placed on the grid initially.")]
        public List<CharacterData> initialCharacters;

        [Header("Elevator Settings")]
        [Tooltip("The sequence of elevators that will appear in this level.")]
        public List<ElevatorData> elevatorSequence;
        
        [Header("Slot Settings")]
        public int totalBufferSlots = 6;
        public int initiallyLockedSlots = 2;
    }

    [System.Serializable]
    public class CharacterData
    {
        public Vector2Int position; // x is column, y is row
        public CharacterColor color;
        public MoveDirection moveDirection;
    }

    [System.Serializable]
    public class ElevatorData
    {
        public CharacterColor requiredColor;
        public int requiredCapacity;
        public bool isLockedInitially = false;
    }
}
