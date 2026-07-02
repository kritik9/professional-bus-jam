using System;
using ElevatorGame.Core;
using UnityEngine;

namespace ElevatorGame.Events
{
    /// <summary>
    /// A decoupled event bus system for the Elevator Game.
    /// Managers subscribe to these events instead of calling each other directly.
    /// </summary>
    public static class GameEvents
    {
        // Character Events
        public static Action<MonoBehaviour> OnCharacterReachedExit; // Passes the character object
        
        // Slot Events
        public static Action<MonoBehaviour> OnSlotFilled;
        public static Action<MonoBehaviour> OnSlotFreed;
        
        // Elevator Events
        public static Action<CharacterColor> OnElevatorReady;
        public static Action<CharacterColor> OnElevatorFull;
        
        // Game State Events
        public static Action OnLevelStarted;
        public static Action OnLevelWon;
        public static Action OnLevelLost;
    }
}
