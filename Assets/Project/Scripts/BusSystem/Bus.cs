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

    [Header("Movement")]
    public float BusmoveSpeed = 5f;
     

    public int boarded = 0;

    private readonly Queue<PassengerController> boardingQueue = new();
    private bool isProcessingBoarding = false;
     

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
        if (renderer == null)
            return;

        Color appliedColor = GetColorFromEnum(color);
        renderer.material.color = appliedColor;

        if (busModelBody != null)
        {
            foreach (Renderer part in busModelBody.GetComponentsInChildren<Renderer>())
            {
                part.material.color = appliedColor;
            }
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (RemainingPassanger == null)
            return;

        int remaining = capacity - boarded;
        RemainingPassanger.text = remaining.ToString();
    }

    public void Leave()
    {
        Vector3 exitPos = transform.position + transform.right * 10f;
        StartCoroutine(ExitRoutine(exitPos));
    }

    public void Enter(Vector3 targetPos)
    {
        StartCoroutine(EnterRoutine(targetPos));
    }
     

    private IEnumerator ProcessBoarding()
    {
        isProcessingBoarding = true;

        List<PassengerController> waitingPassengers = new();
        List<PassengerController> gridPassengers = new();

        while (boardingQueue.Count > 0)
        {
            PassengerController p = boardingQueue.Dequeue();

            if (p.isInWaiting)
                waitingPassengers.Add(p);
            else
                gridPassengers.Add(p);
        } 
        foreach (var passenger in waitingPassengers)
        {
            if (boarded >= capacity)
                break;

            Vector3 boardingPos = transform.position + transform.right * 1f;

            passenger.MoveToPosition(boardingPos, () =>
            {
                HandlePassengerBoarding(passenger);

                GameManager.Instance._waitingArea.RemovePassenger(passenger);

                if (boarded >= capacity)
                    Leave();
            });

            yield return null;
        }
         
        foreach (var passenger in gridPassengers)
        {
            if (boarded >= capacity)
                break;

            Vector3 boardingPos = transform.position + transform.right * 1f;

            passenger.MoveToPosition(boardingPos, () =>
            {
                if (boarded >= capacity)
                {
                    GameManager.Instance.MoveToWaitingArea(passenger);
                    return;
                }

                HandlePassengerBoarding(passenger);

                if (boarded >= capacity)
                    Leave();
            });

            yield return null;
        }

        if (boarded >= capacity)
            Leave();

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
     

    private IEnumerator ExitRoutine(Vector3 exitPos)
    {
        GameManager.Instance.CurrentBus = null;
        GameManager.Instance.OnBusLeft();

        while (Vector3.Distance(transform.position, exitPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                exitPos,
                Time.deltaTime * BusmoveSpeed
            );
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator EnterRoutine(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                Time.deltaTime * BusmoveSpeed
            );
            yield return null;
        }

        transform.position = targetPos;

        GameManager.Instance.CurrentBus = this;
        GameManager.Instance.AutoBoardWaitingPassengers();
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