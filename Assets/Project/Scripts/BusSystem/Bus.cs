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

    [Header("Boarding")]
    [SerializeField] private Transform boardingPoint;

    [Header("Elevator Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [SerializeField] private float doorOpenDistance = 0.6f;
    [SerializeField] private float doorSpeed = 2f;

    [Header("Elevator Movement")]
    [SerializeField] private float leaveHeight = 15f;
    [SerializeField] private float leaveSpeed = 4f;

    public int boarded = 0;

    /// <summary>
    /// False while the elevator is moving into position.
    /// </summary>
    public bool IsReady { get; private set; }

    public BusSpawner Spawner { get; set; }

    private readonly Queue<PassengerController> boardingQueue = new();

    private bool isProcessingBoarding;
    private bool leaving;

    private Vector3 leftClosedPos;
    private Vector3 leftOpenPos;

    private Vector3 rightClosedPos;
    private Vector3 rightOpenPos;

    private void Awake()
    {
        IsReady = false;

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

        StartCoroutine(MoveDoors(false));
    }

    public void SetReady(bool ready)
    {
        IsReady = ready;
    }

    public bool HasSpace()
    {
        return boarded < capacity;
    }

    public void SetCapacity(int newCapacity)
    {
        capacity = newCapacity;
        boarded = 0;
        leaving = false;
        UpdateUI();
    }

    public void Board(PassengerController passenger)
    {
        if (passenger == null)
            return;

        if (leaving)
            return;

        if (!IsReady)
            return;

        boardingQueue.Enqueue(passenger);

        if (!isProcessingBoarding)
            StartCoroutine(ProcessBoarding());
    }

    public void ApplyColor()
    {
        Color appliedColor = GetColorFromEnum(color);

        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = appliedColor;

        if (busModelBody != null)
        {
            foreach (Renderer r in busModelBody.GetComponentsInChildren<Renderer>())
                r.material.color = appliedColor;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (RemainingPassanger != null)
            RemainingPassanger.text = (capacity - boarded).ToString();
    }

    private IEnumerator ProcessBoarding()
    {
        isProcessingBoarding = true;

        yield return StartCoroutine(MoveDoors(true));

        List<PassengerController> waitingPassengers = new();
        List<PassengerController> gridPassengers = new();

        while (boardingQueue.Count > 0)
        {
            PassengerController passenger = boardingQueue.Dequeue();

            if (passenger == null)
                continue;

            if (passenger.isInWaiting)
                waitingPassengers.Add(passenger);
            else
                gridPassengers.Add(passenger);
        }

        foreach (PassengerController passenger in waitingPassengers)
        {
            if (boarded >= capacity)
                break;

            BoardPassenger(passenger);
            yield return null;
        }

        foreach (PassengerController passenger in gridPassengers)
        {
            if (boarded >= capacity)
                break;

            BoardPassenger(passenger);
            yield return null;
        }

        isProcessingBoarding = false;

        if (boarded >= capacity)
            StartCoroutine(FinishBoarding());
    }

    private void BoardPassenger(PassengerController passenger)
    {
        Vector3 target = boardingPoint != null
            ? boardingPoint.position
            : transform.position;

        passenger.MoveToPosition(target, () =>
        {
            if (boarded >= capacity)
            {
                GameManager.Instance.MoveToWaitingArea(passenger);
                return;
            }

            HandlePassengerBoarding(passenger);

            if (passenger.isInWaiting)
                GameManager.Instance._waitingArea.RemovePassenger(passenger);

            if (boarded >= capacity && !leaving)
                StartCoroutine(FinishBoarding());
        });
    }

    private void HandlePassengerBoarding(PassengerController passenger)
    {
        var spawner = GameManager.Instance._passengerSpawner;

        if (spawner != null &&
            spawner.colorCounts.ContainsKey(passenger.color))
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

    private IEnumerator FinishBoarding()
    {
        if (leaving)
            yield break;

        leaving = true;

        yield return StartCoroutine(MoveDoors(false));

        Leave();
    }

    public void Leave()
    {
        StartCoroutine(LeaveRoutine());
    }

    private IEnumerator LeaveRoutine()
    {
        GameManager.Instance.OnBusLeft(this);

        Vector3 target = transform.position + Vector3.up * leaveHeight;

        while (Vector3.Distance(transform.position, target) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                leaveSpeed * Time.deltaTime);

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator MoveDoors(bool open)
    {
        if (leftDoor == null || rightDoor == null)
            yield break;

        Vector3 targetLeft = open ? leftOpenPos : leftClosedPos;
        Vector3 targetRight = open ? rightOpenPos : rightClosedPos;

        while (Vector3.Distance(leftDoor.localPosition, targetLeft) > 0.01f ||
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

    private Color GetColorFromEnum(PassengerColor passengerColor)
    {
        switch (passengerColor)
        {
            case PassengerColor.Red:
                return new Color32(255, 70, 70, 255);

            case PassengerColor.Blue:
                return new Color32(50, 155, 255, 255);

            case PassengerColor.Green:
                return new Color32(70, 220, 90, 255);

            case PassengerColor.Yellow:
                return new Color32(255, 220, 40, 255);

            case PassengerColor.Orange:
                return new Color32(255, 145, 35, 255);

            case PassengerColor.Purple:
                return new Color32(170, 80, 255, 255);

            case PassengerColor.Pink:
                return new Color32(255, 95, 180, 255);

            case PassengerColor.Cyan:
                return new Color32(50, 225, 255, 255);

            case PassengerColor.Gray:
                return new Color32(145, 145, 160, 255);

            case PassengerColor.White:
                return new Color32(255, 255, 255, 255);

            default:
                return Color.white;
        }
    }
}