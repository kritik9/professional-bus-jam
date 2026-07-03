using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static GameEnums;
using UnityEngine.UI;

public class Bus : MonoBehaviour
{
    [Header("Bus Settings")]
    public PassengerColor color;
    public int capacity = 3;

    [Header("UI")]
    public TextMeshProUGUI RemainingPassanger;
    public Image lockImage;
    public bool IsLocked { get; private set; }
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

    private Coroutine doorRoutine;
    private bool doorsOpen;

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
    private Vector3 rightClosedPos;

    private Vector3 leftClosedScale;
    private Vector3 rightClosedScale;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private Vector3 leftOpenScale;
    private Vector3 rightOpenScale;

    private void Awake()
    {
        IsReady = false;

        if (leftDoor != null)
        {
            leftClosedPos = leftDoor.localPosition;
            leftClosedScale = leftDoor.localScale;

            leftOpenPos = new Vector3(
                0.5f,
                leftClosedPos.y,
                leftClosedPos.z);

            leftOpenScale = new Vector3(
                0f,
                leftClosedScale.y,
                leftClosedScale.z);
        }

        if (rightDoor != null)
        {
            rightClosedPos = rightDoor.localPosition;
            rightClosedScale = rightDoor.localScale;

            rightOpenPos = new Vector3(
                -0.5f,
                rightClosedPos.y,
                rightClosedPos.z);

            rightOpenScale = new Vector3(
                0f,
                rightClosedScale.y,
                rightClosedScale.z);
        }

        StartCoroutine(MoveDoors(false));
    }

    public void SetReady(bool ready)
    {
        IsReady = ready;
    }

    public bool HasSpace()
    {
        if (IsLocked)
            return false;

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

        if (IsLocked)
            return;
        
        if (leaving)
            return;

        if (!IsReady)
            return;

        boardingQueue.Enqueue(passenger);

        // Open the doors immediately when the first passenger starts coming
        if (!doorsOpen)
        {
            if (doorRoutine != null)
                StopCoroutine(doorRoutine);

            doorRoutine = StartCoroutine(OpenDoorsRoutine());
        }

        if (!isProcessingBoarding)
            StartCoroutine(ProcessBoarding());
    }
    private IEnumerator OpenDoorsRoutine()
    {
        doorsOpen = true;
        yield return MoveDoors(true);
        doorRoutine = null;
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

        //yield return StartCoroutine(MoveDoors(true));

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

        //yield return StartCoroutine(MoveDoors(false));
        doorsOpen = false;
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

    Vector3 startLeftPos = leftDoor.localPosition;
    Vector3 startRightPos = rightDoor.localPosition;

    Vector3 startLeftScale = leftDoor.localScale;
    Vector3 startRightScale = rightDoor.localScale;

    Vector3 targetLeftPos = open ? leftOpenPos : leftClosedPos;
    Vector3 targetRightPos = open ? rightOpenPos : rightClosedPos;

    Vector3 targetLeftScale = open ? leftOpenScale : leftClosedScale;
    Vector3 targetRightScale = open ? rightOpenScale : rightClosedScale;

    float duration = 1f / doorSpeed;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        leftDoor.localPosition = Vector3.Lerp(startLeftPos, targetLeftPos, t);
        rightDoor.localPosition = Vector3.Lerp(startRightPos, targetRightPos, t);

        leftDoor.localScale = Vector3.Lerp(startLeftScale, targetLeftScale, t);
        rightDoor.localScale = Vector3.Lerp(startRightScale, targetRightScale, t);

        yield return null;
    }

    leftDoor.localPosition = targetLeftPos;
    rightDoor.localPosition = targetRightPos;

    leftDoor.localScale = targetLeftScale;
    rightDoor.localScale = targetRightScale;
}
 

    public void SetLocked(bool locked)
    {
        IsLocked = locked;

        lockImage.gameObject.SetActive(locked);

        if (RemainingPassanger != null)
            RemainingPassanger.gameObject.SetActive(!locked);

        if (locked)
        {
            color = PassengerColor.White;
            ApplyColor();
            SetReady(false);
        }
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