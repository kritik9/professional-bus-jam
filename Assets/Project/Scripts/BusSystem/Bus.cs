using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static GameEnums;

public class Bus : MonoBehaviour
{
    [Header("Bus Settings")]
    public PassengerColor color;
    public int capacity = 3;

    [Header("UI")]
    public TextMeshProUGUI RemainingPassanger;

    [Header("Visuals")]
    public GameObject busModelBody;

    public int boarded = 0;

    private readonly Queue<PassengerController> boardingQueue = new();
    private bool isProcessingBoarding = false;

    [Header("Elevator Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [SerializeField] private float doorOpenDistance = 0.6f;
    [SerializeField] private float doorSpeed = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private void Awake()
    {
        if (leftDoor != null)
        {
            leftClosedPos = leftDoor.localPosition;
            leftOpenPos = leftClosedPos + Vector3.left * doorOpenDistance;
        }

        if (rightDoor != null)
        {
            rightClosedPos = rightDoor.localPosition;
            rightOpenPos = rightClosedPos + Vector3.right * doorOpenDistance;
        }
    }
    public bool HasSpace()
    {
        return boarded < capacity;
    }

    public void SetCapacity(int newCapacity)
    {
        capacity = newCapacity;
        boarded = 0;
        UpdateUI();
    }
    private IEnumerator MoveDoors(bool open)
    {
        if (leftDoor == null || rightDoor == null)
            yield break;

        Vector3 targetLeft = open ? leftOpenPos : leftClosedPos;
        Vector3 targetRight = open ? rightOpenPos : rightClosedPos;

        while (
            Vector3.Distance(leftDoor.localPosition, targetLeft) > 0.01f ||
            Vector3.Distance(rightDoor.localPosition, targetRight) > 0.01f)
        {
            leftDoor.localPosition = Vector3.MoveTowards(
                leftDoor.localPosition,
                targetLeft,
                doorSpeed * Time.deltaTime);

            rightDoor.localPosition = Vector3.MoveTowards(
                rightDoor.localPosition,
                targetRight,
                doorSpeed * Time.deltaTime);

            yield return null;
        }

        leftDoor.localPosition = targetLeft;
        rightDoor.localPosition = targetRight;
    }
    public void Board(PassengerController passenger)
    {
        if (passenger == null)
            return;

        boardingQueue.Enqueue(passenger);

        if (!isProcessingBoarding)
            StartCoroutine(ProcessBoarding());
    }

    public void ApplyColor()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = GetColorFromEnum(color);

        if (busModelBody != null)
        {
            foreach (Renderer part in busModelBody.GetComponentsInChildren<Renderer>())
                part.material.color = GetColorFromEnum(color);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (RemainingPassanger == null)
            return;

        RemainingPassanger.text = (capacity - boarded).ToString();
    }

    private IEnumerator ProcessBoarding()
    {
        yield return StartCoroutine(MoveDoors(true));
        isProcessingBoarding = true;

        List<PassengerController> waitingPassengers = new();
        List<PassengerController> gridPassengers = new();

        while (boardingQueue.Count > 0)
        {
            PassengerController passenger = boardingQueue.Dequeue();

            if (passenger.isInWaiting)
                waitingPassengers.Add(passenger);
            else
                gridPassengers.Add(passenger);
        }

        foreach (PassengerController passenger in waitingPassengers)
        {
            if (boarded >= capacity)
                break;

            Vector3 boardingPos = transform.position + transform.right;

            passenger.MoveToPosition(boardingPos, () =>
            {
                HandlePassengerBoarding(passenger);

                GameManager.Instance._waitingArea.RemovePassenger(passenger);

                if (boarded >= capacity)
                {
                    StartCoroutine(FinishBoarding());
                }
            });

            yield return null;
        }

        foreach (PassengerController passenger in gridPassengers)
        {
            if (boarded >= capacity)
                break;

            Vector3 boardingPos = transform.position + transform.right;

            passenger.MoveToPosition(boardingPos, () =>
            {
                if (boarded >= capacity)
                {
                    GameManager.Instance.MoveToWaitingArea(passenger);
                    return;
                }

                HandlePassengerBoarding(passenger);

                if (boarded >= capacity)
                {
                    StartCoroutine(FinishBoarding());
                }
            });

            yield return null;
        }

        if (boarded >= capacity)
        {
            StartCoroutine(FinishBoarding());
        }

        isProcessingBoarding = false;
    }

    private void HandlePassengerBoarding(PassengerController passenger)
    {
        var spawner = GameManager.Instance._passengerSpawner;

        if (spawner != null && spawner.colorCounts.ContainsKey(passenger.color))
        {
            spawner.colorCounts[passenger.color]--;

            if (spawner.colorCounts[passenger.color] <= 0)
                spawner.colorCounts.Remove(passenger.color);
        }

        boarded++;
        UpdateUI();

        if (passenger.currentCell != null)
            passenger.currentCell.CurrentPassanger = null;

        passenger.gameObject.SetActive(false);
    }
    private bool leaving = false;

    private IEnumerator FinishBoarding()
    {
        if (leaving)
            yield break;

        leaving = true;

        yield return StartCoroutine(MoveDoors(false));

        GameManager.Instance.CurrentBus = null;
        GameManager.Instance.OnBusLeft();
    }
    private Color GetColorFromEnum(PassengerColor passengerColor)
    {
        switch (passengerColor)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case PassengerColor.Pink: return new Color(1f, 0.75f, 0.8f);
            case PassengerColor.Cyan: return Color.cyan;
            case PassengerColor.Gray: return Color.gray;
            case PassengerColor.White: return Color.white;
            default: return Color.white;
        }
    }
}