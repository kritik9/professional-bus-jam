using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private float inputDelay = 0.2f;
    private float lastInputTime = -Mathf.Infinity;
    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (Time.time - lastInputTime < inputDelay)
            return; 

        if (Input.GetMouseButtonDown(0))
        {
            lastInputTime = Time.time;
            ProcessRay(Input.mousePosition);
        }


        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            lastInputTime = Time.time;
            ProcessRay(Input.GetTouch(0).position);
        }
    }

    void ProcessRay(Vector3 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            PassengerController p = hit.collider.GetComponent<PassengerController>();

            if (p != null)
            {
                if (GameManager.Instance.CanMovePassenger(p))
                { 
                        GameManager.Instance.TryMovePassenger(p);
                }
            }
        }
    }
}

