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
            character.gameObject.SetActive(true);
            character.HideArrow();
            
            character.StartCoroutine(MoveToSlotRoutine(character));
        }

        private System.Collections.IEnumerator MoveToSlotRoutine(Character character)
        {
            Vector3 targetPos = transform.position + Vector3.up * 0.35f;
            Vector3 startPos = character.transform.position;
            
            float dist = Vector3.Distance(startPos, targetPos);
            if (dist > 0.01f)
            {
                float duration = dist / 8f; // Consistent speed
                float t = 0;
                while (t < 1)
                {
                    t += Time.deltaTime / duration;
                    character.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    yield return null;
                }
            }
            
            character.transform.position = targetPos;
        }

        public void ClearCharacter()
        {
            OccupyingCharacter = null;
        }

        public bool IsAvailable => !IsLocked && OccupyingCharacter == null;
    }
}
