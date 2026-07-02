using UnityEngine;
using ElevatorGame.Characters;

namespace ElevatorGame.Core
{
    public class InputManager : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ProcessTap(Input.mousePosition);
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ProcessTap(Input.GetTouch(0).position);
            }
        }

        private void ProcessTap(Vector2 screenPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Character character = hit.collider.GetComponent<Character>();
                if (character != null)
                {
                    character.OnTapped();
                }
            }
        }
    }
}
