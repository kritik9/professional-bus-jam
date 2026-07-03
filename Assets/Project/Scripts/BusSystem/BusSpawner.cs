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

    [Header("Elevator Settings")]
    [SerializeField] private float elevatorSpawnOffset = 12f;
    [SerializeField] private float elevatorExitHeight = 15f;
    [SerializeField] private float elevatorMoveSpeed = 4f;

    private readonly List<Bus> activeBuses = new();

    private int currentBusIndex = 0;
    private int currentBusCapacity = 0;

    public int RemainingBusCount => Mathf.Max(0, activeBuses.Count - currentBusIndex);

    public void SetupBuses(int carCount, int carCapacity, List<PassengerColor> busOrder)
    {
        ClearCars();

        currentBusCapacity = carCapacity;
        currentBusIndex = 0;
        activeBuses.Clear();

        Vector3 stopPos = BusStopTransform.position;

        for (int i = 0; i < carCount; i++)
        {
            Vector3 spawnPos = (i == 0)
                ? stopPos
                : stopPos + Vector3.down * elevatorSpawnOffset;

            GameObject obj = Instantiate(busPrefab, spawnPos, Quaternion.identity);

            Bus bus = obj.GetComponent<Bus>();

            bus.color = busOrder[i];
            bus.SetCapacity(currentBusCapacity);
            bus.ApplyColor();

            activeBuses.Add(bus);

            if (i == 0)
                GameManager.Instance.CurrentBus = bus;
        }

        Debug.Log($"Spawned {activeBuses.Count} elevators.");
    }

    public void MoveNextBusToStop()
    {
        if (currentBusIndex >= activeBuses.Count)
            return;

        Bus current = activeBuses[currentBusIndex];

        Vector3 exitPos = current.transform.position + Vector3.up * elevatorExitHeight;

        // Current elevator leaves
        StartCoroutine(MoveElevator(current, exitPos, true));

        // Next elevator arrives
        if (currentBusIndex + 1 < activeBuses.Count)
        {
            currentBusIndex++;

            Bus next = activeBuses[currentBusIndex];

            Vector3 stopPos = BusStopTransform.position;

            next.transform.position = stopPos + Vector3.down * elevatorSpawnOffset;

            StartCoroutine(BringElevator(next, stopPos));
        }
        else
        {
            GameManager.Instance.CurrentBus = null;
            Debug.Log("No more elevators.");
        }
    }

    private IEnumerator BringElevator(Bus elevator, Vector3 stopPos)
    {
        yield return MoveElevator(elevator, stopPos);

        if (elevator != null)
        {
            GameManager.Instance.CurrentBus = elevator;
            GameManager.Instance.AutoBoardWaitingPassengers();
        }
    }

    private IEnumerator MoveElevator(Bus elevator, Vector3 target, bool destroyOnFinish = false)
    {
        if (elevator == null)
            yield break;

        while (elevator != null &&
               Vector3.Distance(elevator.transform.position, target) > 0.01f)
        {
            elevator.transform.position = Vector3.MoveTowards(
                elevator.transform.position,
                target,
                elevatorMoveSpeed * Time.deltaTime);

            yield return null;
        }

        if (elevator == null)
            yield break;

        elevator.transform.position = target;

        if (destroyOnFinish)
        {
            activeBuses.Remove(elevator);
            Destroy(elevator.gameObject);
        }
    }

    public void ClearCars()
    {
        GameObject[] buses = GameObject.FindGameObjectsWithTag("Bus");

        foreach (GameObject bus in buses)
            Destroy(bus);

        activeBuses.Clear();
        currentBusIndex = 0;
    }
}