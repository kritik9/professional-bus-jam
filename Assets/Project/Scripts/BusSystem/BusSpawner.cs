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
    public int RemainingBusCount => activeBuses.Count;
    private readonly List<Bus> activeBuses = new();

    public IReadOnlyList<Bus> ActiveBuses => activeBuses;

    private int currentBusCapacity;

    public void SetupBuses(int carCount, int carCapacity, List<PassengerColor> busOrder)
    {
        ClearCars();

        currentBusCapacity = carCapacity;

        Vector3 center = BusStopTransform.position;

        // Center the elevators around the spawn point.
        float totalWidth = (carCount - 1) * elevatorSpacing;
        float startX = center.x - totalWidth * 0.5f;

        for (int i = 0; i < carCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                startX + i * elevatorSpacing,
                center.y,
                center.z);

            GameObject obj = Instantiate(busPrefab, spawnPos, Quaternion.identity);

            Bus bus = obj.GetComponent<Bus>();

            bus.color = busOrder[i];
            bus.SetCapacity(currentBusCapacity);
            bus.ApplyColor();

            activeBuses.Add(bus);
        }

        Debug.Log($"Spawned {activeBuses.Count} elevators.");
    }
    public void BusLeft(Bus bus)
    {
        if (bus == null)
            return;

        activeBuses.Remove(bus);

        Vector3 target =
            bus.transform.position + Vector3.up * 15f;

        StartCoroutine(RemoveElevatorRoutine(bus, target));
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

    public void RemoveBus(Bus bus)
    {
        if (bus == null)
            return;

        activeBuses.Remove(bus);
    }

    public Coroutine MoveElevator(Bus elevator, Vector3 target)
    {
        return StartCoroutine(MoveElevatorRoutine(elevator, target));
    }

    private IEnumerator MoveElevatorRoutine(Bus elevator, Vector3 target)
    {
        if (elevator == null)
            yield break;

        while (Vector3.Distance(elevator.transform.position, target) > 0.01f)
        {
            elevator.transform.position = Vector3.MoveTowards(
                elevator.transform.position,
                target,
                elevatorMoveSpeed * Time.deltaTime);

            yield return null;
        }

        if (elevator != null)
            elevator.transform.position = target;
    }

    public void ClearCars()
    {
        GameObject[] buses = GameObject.FindGameObjectsWithTag("Bus");

        foreach (GameObject bus in buses)
        {
            Destroy(bus);
        }

        activeBuses.Clear();
    }
}