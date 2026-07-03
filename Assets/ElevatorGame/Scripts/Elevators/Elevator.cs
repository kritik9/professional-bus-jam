using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Data;
using ElevatorGame.Characters;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

namespace ElevatorGame.Elevators
{
    public class Elevator : MonoBehaviour
    {
        public CharacterColor RequiredColor { get; private set; }
        public int Capacity { get; private set; }
        public int CurrentCount { get; private set; }
        public bool IsLocked { get; private set; }
        public int SlotIndex { get; private set; }
        
        public bool IsFull => CurrentCount >= Capacity;
        
        public Action<Elevator> OnElevatorFull;
        
        [Header("UI Elements")]
        [SerializeField] private TMP_Text capacityText;
        
        private List<Character> _boardedCharacters = new List<Character>();
        private Dictionary<Character, Coroutine> _moveCoroutines = new Dictionary<Character, Coroutine>();

        public void Initialize(ElevatorData data, int slotIndex)
        {
            RequiredColor = data.requiredColor;
            Capacity = data.requiredCapacity;
            IsLocked = data.isLockedInitially;
            CurrentCount = 0;
            SlotIndex = slotIndex;
            
            ApplyColorToRenderers(RequiredColor);
            UpdateCapacityText();
        }

        private void UpdateCapacityText()
        {
            if (capacityText != null)
            {
                capacityText.text = $"{CurrentCount} / {Capacity}";
            }
        }

        private void ApplyColorToRenderers(CharacterColor colorType)
        {
            Color unityColor = GetUnityColor(colorType);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null && renderer.GetComponent<TMP_Text>() == null)
                {
                    renderer.material.color = unityColor;
                }
            }
        }

        private Color GetUnityColor(CharacterColor characterColor)
        {
            switch (characterColor)
            {
                case CharacterColor.Red: return Color.red;
                case CharacterColor.Blue: return Color.blue;
                case CharacterColor.Green: return Color.green;
                case CharacterColor.Yellow: return Color.yellow;
                case CharacterColor.Purple: return new Color(0.5f, 0, 0.5f);
                case CharacterColor.Orange: return new Color(1f, 0.64f, 0f);
                case CharacterColor.Pink: return new Color(1f, 0.75f, 0.8f);
                case CharacterColor.Cyan: return Color.cyan;
                case CharacterColor.Gray: return Color.gray;
                case CharacterColor.White: return Color.white;
                default: return Color.white;
            }
        }

        public bool TryBoard(Character character)
        {
            if (IsLocked || IsFull || character.Color != RequiredColor) return false;
            
            CurrentCount++;
            UpdateCapacityText();
            
            character.HideArrow();
            Vector3 targetWorldScale = character.transform.lossyScale * GetTargetScale(Capacity);
            character.transform.SetParent(transform, true);
            
            Vector3 endScale = new Vector3(
                targetWorldScale.x / transform.lossyScale.x,
                targetWorldScale.y / transform.lossyScale.y,
                targetWorldScale.z / transform.lossyScale.z
            );
            
            _boardedCharacters.Add(character);
            
            // Reposition ALL characters (including the new one) to their updated dice patterns
            for (int i = 0; i < _boardedCharacters.Count; i++)
            {
                Character c = _boardedCharacters[i];
                
                if (_moveCoroutines.ContainsKey(c) && _moveCoroutines[c] != null)
                {
                    StopCoroutine(_moveCoroutines[c]);
                }
                
                bool isNew = (c == character);
                _moveCoroutines[c] = StartCoroutine(MoveToSlotRoutine(c, i, CurrentCount, endScale, isNew));
            }
            
            if (IsFull)
            {
                StartCoroutine(WaitAndDepartRoutine());
            }
            
            return true;
        }
        
        private float GetTargetScale(int capacity)
        {
            if (capacity <= 6) return 1f;
            if (capacity <= 9) return 0.7f;
            return 0.5f;
        }

        private Vector3 GetSlotLocalPosition(int index, int capacity)
        {
            float yPos = 0.35f;
            
            if (capacity <= 6)
            {
                float spacing = 0.2f; // Tighter margin so they don't clip the walls
                if (capacity == 1) return new Vector3(0, yPos, 0);
                if (capacity == 2)
                {
                    if (index == 0) return new Vector3(-spacing, yPos, 0);
                    return new Vector3(spacing, yPos, 0);
                }
                if (capacity == 3)
                {
                    // Triangle: first 2 at front corners (-Z), 3rd at back center (+Z)
                    if (index == 0) return new Vector3(-spacing, yPos, -spacing);
                    if (index == 1) return new Vector3(spacing, yPos, -spacing);
                    return new Vector3(0, yPos, spacing);
                }
                if (capacity == 4)
                {
                    if (index == 0) return new Vector3(-spacing, yPos, -spacing);
                    if (index == 1) return new Vector3(spacing, yPos, -spacing);
                    if (index == 2) return new Vector3(-spacing, yPos, spacing);
                    return new Vector3(spacing, yPos, spacing);
                }
                if (capacity == 5)
                {
                    if (index == 0) return new Vector3(-spacing, yPos, -spacing);
                    if (index == 1) return new Vector3(spacing, yPos, -spacing);
                    if (index == 2) return new Vector3(-spacing, yPos, spacing);
                    if (index == 3) return new Vector3(spacing, yPos, spacing);
                    return new Vector3(0, yPos, 0);
                }
                if (capacity == 6)
                {
                    if (index == 0) return new Vector3(-spacing, yPos, -0.25f);
                    if (index == 1) return new Vector3(spacing, yPos, -0.25f);
                    if (index == 2) return new Vector3(-spacing, yPos, 0);
                    if (index == 3) return new Vector3(spacing, yPos, 0);
                    if (index == 4) return new Vector3(-spacing, yPos, 0.25f);
                    return new Vector3(spacing, yPos, 0.25f);
                }
            }
            
            int cols = Mathf.CeilToInt(Mathf.Sqrt(capacity));
            int rows = Mathf.CeilToInt((float)capacity / cols);
            int r = index / cols;
            int c = index % cols;
            
            float width = 0.5f; 
            float height = 0.5f;
            float xPos = -width/2f + (cols > 1 ? (c / (float)(cols - 1)) * width : 0);
            float zPos = -height/2f + (rows > 1 ? (r / (float)(rows - 1)) * height : 0);
            
            return new Vector3(xPos, yPos, zPos);
        }

        private IEnumerator MoveToSlotRoutine(Character character, int index, int currentTotal, Vector3 endScale, bool isNew)
        {
            Vector3 startLocal = character.transform.localPosition;
            Vector3 endLocal = GetSlotLocalPosition(index, currentTotal);
            Vector3 startScale = character.transform.localScale;
            
            float moveSpeed = 8f; // Consistent speed for boarding
            
            if (isNew)
            {
                // The gate is at the front (local Z = -1.25f)
                Vector3 gateLocal = new Vector3(0, 0.35f, -1.25f);
                
                float dist1 = Vector3.Distance(startLocal, gateLocal);
                if (dist1 > 0.01f)
                {
                    float duration1 = dist1 / moveSpeed;
                    float t1 = 0;
                    while (t1 < 1)
                    {
                        t1 += Time.deltaTime / duration1; 
                        character.transform.localPosition = Vector3.Lerp(startLocal, gateLocal, t1);
                        character.transform.localScale = Vector3.Lerp(startScale, endScale, t1); 
                        yield return null;
                    }
                }
                
                startLocal = gateLocal;
                startScale = endScale;
            }
            
            float dist2 = Vector3.Distance(startLocal, endLocal);
            if (dist2 > 0.01f)
            {
                float duration2 = dist2 / moveSpeed;
                float t2 = 0;
                while(t2 < 1)
                {
                    t2 += Time.deltaTime / duration2;
                    character.transform.localPosition = Vector3.Lerp(startLocal, endLocal, t2);
                    character.transform.localScale = Vector3.Lerp(startScale, endScale, t2);
                    yield return null;
                }
            }
            
            character.transform.localPosition = endLocal;
            character.transform.localScale = endScale;
        }

        private IEnumerator WaitAndDepartRoutine()
        {
            yield return new WaitForSeconds(1f);
            OnElevatorFull?.Invoke(this);
        }
        
        public void Depart(Action onComplete)
        {
            StartCoroutine(DepartRoutine(onComplete));
        }
        
        private IEnumerator DepartRoutine(Action onComplete)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 10f;
            float t = 0;
            while(t < 1)
            {
                t += Time.deltaTime * 2f;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
