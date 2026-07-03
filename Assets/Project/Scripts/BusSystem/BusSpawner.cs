using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEnums;

public class BusSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject busPrefab;

    [Header("Scene References")]
    [SerializeField] private Transform BusStopTransform;
    [SerializeField] private PassengerSpawner passengerSpawner;

    [Header("Layout")]
    [SerializeField] private float elevatorSpacing = 10f;

    [Header("Movement")]
    [SerializeField] private float elevatorMoveSpeed = 4f;

    [Header("Limits")]
    [SerializeField] private int maxActiveBuses = 3;

    public int RemainingBusCount => activeBuses.Count;
    public IReadOnlyList<Bus> ActiveBuses => activeBuses;

    private readonly List<Bus> activeBuses = new();
    private readonly Queue<PassengerColor> busQueue = new();

    private int currentBusCapacity;

    // -------------------- SETUP --------------------

    public void SetupBuses(int carCapacity, List<PassengerColor> busOrder)
    {
        ClearCars();

        currentBusCapacity = carCapacity;

        busQueue.Clear();

        foreach (var color in busOrder)
            busQueue.Enqueue(color);

        // spawn initial limited buses
        for (int i = 0; i < maxActiveBuses; i++)
        {
            SpawnBusOrPlaceholder(i);
        }
    }

    // -------------------- SPAWN --------------------
    private void SpawnBusOrPlaceholder(int slotIndex)
    {
        Vector3 targetPos = GetSpawnPosition(slotIndex);
        Vector3 startPos = targetPos + Vector3.down * 15f;

        GameObject obj = Instantiate(busPrefab, startPos, Quaternion.identity);

        Bus bus = obj.GetComponent<Bus>();

        if (busQueue.Count > 0)
        {
            bus.color = busQueue.Dequeue();
            bus.SetCapacity(currentBusCapacity);
            bus.SetLocked(false);
        }
        else
        {
            bus.SetCapacity(0);
            bus.SetLocked(true);
        }

        bus.ApplyColor();

        activeBuses.Insert(slotIndex, bus);

        StartCoroutine(MoveElevatorRoutine(bus, targetPos));
    }
    private void SpawnNextBus(int slotIndex = -1)
    {
        if (busQueue.Count == 0)
            return;

        PassengerColor color = busQueue.Dequeue();

        if (slotIndex < 0)
            slotIndex = activeBuses.Count;

        Vector3 targetPos = GetSpawnPosition(slotIndex);
        Vector3 startPos = targetPos + Vector3.down * 15f;

        GameObject obj = Instantiate(busPrefab, startPos, Quaternion.identity);

        Bus bus = obj.GetComponent<Bus>();

        bus.color = color;
        bus.SetCapacity(currentBusCapacity);
        bus.ApplyColor();

        activeBuses.Insert(slotIndex, bus);

        StartCoroutine(MoveElevatorRoutine(bus, targetPos));
    }

    private Vector3 GetSpawnPosition(int index)
    {
        return GetCenteredPosition(maxActiveBuses, index);
    }

    // -------------------- BUS EXIT --------------------
    public void BusLeft(Bus bus)
    {
        if (bus == null)
            return;

        int slot = activeBuses.IndexOf(bus);

        if (slot == -1)
            return;

        activeBuses.RemoveAt(slot);

        if (busQueue.Count > 0)
        {
            //SpawnNextBus(slot);
            SpawnBusOrPlaceholder(slot);
        }
    }

    private IEnumerator RemoveElevatorRoutine(Bus elevator, Vector3 target)
    {
        while (elevator != null)
        {
            if (Vector3.Distance(elevator.transform.position, target) <= 0.01f)
                break;

            elevator.transform.position = Vector3.MoveTowards(
                elevator.transform.position,
                target,
                elevatorMoveSpeed * Time.deltaTime);

            yield return null;
        }

        if (elevator != null)
            Destroy(elevator.gameObject);
    }

    // -------------------- OPTIONAL CLEAN ALIGN --------------------

    private void RepositionBuses()
    {
        int count = activeBuses.Count;

        for (int i = 0; i < count; i++)
        {
            if (activeBuses[i] == null)
                continue;

            activeBuses[i].transform.position = GetCenteredPosition(count, i);
        }
    }
    private Vector3 GetCenteredPosition(int count, int index)
    {
        Vector3 center = BusStopTransform.position;

        if (count == 1)
            return center;

        if (count == 2)
        {
            if (index == 0)
                return center + Vector3.left * elevatorSpacing;

            return center + Vector3.right * elevatorSpacing;
        }

        float totalWidth = (maxActiveBuses - 1) * elevatorSpacing;
        float startX = center.x - totalWidth * 0.5f;

        return new Vector3(
            startX + index * elevatorSpacing,
            center.y,
            center.z);
    }
    // -------------------- GET BUS --------------------

    public Bus GetBus(PassengerColor color)
    {
        foreach (Bus bus in activeBuses)
        {
            if (bus == null)
                continue;

            if (bus.color == color && bus.HasSpace())
                return bus;
        }

        return null;
    }
    public Coroutine MoveElevator(Bus bus, Vector3 target)
    {
        return StartCoroutine(MoveElevatorRoutine(bus, target));
    }

    private IEnumerator MoveElevatorRoutine(Bus bus, Vector3 target)
    {
        bus.SetReady(false);

        while (Vector3.Distance(bus.transform.position, target) > 0.01f)
        {
            bus.transform.position = Vector3.MoveTowards(
                bus.transform.position,
                target,
                elevatorMoveSpeed * Time.deltaTime);

            yield return null;
        }

        bus.transform.position = target;

        bus.SetReady(true);

        GameManager.Instance.AutoBoardWaitingPassengers();
    }
    public void RemoveBus(Bus bus)
    {
        if (bus == null)
            return;

        activeBuses.Remove(bus);
    }

    // -------------------- CLEANUP --------------------

    public void ClearCars()
    {
        GameObject[] buses = GameObject.FindGameObjectsWithTag("Bus");

        foreach (GameObject bus in buses)
        {
            Destroy(bus);
        }

        activeBuses.Clear();
        busQueue.Clear();
    }
}