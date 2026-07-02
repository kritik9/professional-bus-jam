using UnityEngine;
using ElevatorGame.Characters;

namespace ElevatorGame.Slots
{
    public class BufferSlot : MonoBehaviour
    {
        public bool IsLocked { get; private set; }
        public Character OccupyingCharacter { get; private set; }

        public void Initialize(bool isLocked)
        {
            IsLocked = isLocked;
            // TODO: Update visual state (e.g., show a padlock icon if locked)
        }

        public void Unlock()
        {
            IsLocked = false;
            // TODO: Update visual state to show it's open
        }

        public void SetCharacter(Character character)
        {
            OccupyingCharacter = character;
            
            // Snap the character into the slot visually
            character.transform.position = transform.position + Vector3.up * 0.5f;
            character.gameObject.SetActive(true);
        }

        public void ClearCharacter()
        {
            OccupyingCharacter = null;
        }

        public bool IsAvailable => !IsLocked && OccupyingCharacter == null;
    }
}
