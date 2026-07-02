using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEnums;

public class BusSpawner : MonoBehaviour
{ 

    [Header("Prefabs")]
    [SerializeField] private GameObject busPrefab;

    [Header("Scene References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform BusStopTransform;
    [SerializeField] private GameObject RoadTransform;
    [SerializeField] private PassengerSpawner passengerSpawner;

    [Header("Movement Settings")]
    [SerializeField] private float busSpacing = 5f;
    [SerializeField] private float BusMovingSpeed = 4f;
     

    private readonly List<Bus> activeBuses = new();
    private readonly Queue<PassengerColor> busQueue = new();

    private int currentBusIndex = 0;
    private int currentBusCapacity = 0;

    public int RemainingBusCount => activeBuses.Count - currentBusIndex;
     

    private void Start()
    {
        GetCameraWorldSize(); // Keeping original behaviour
    }
     

    public void SetupBuses(int carCount, int carCapacity, List<PassengerColor> busOrder)
    {
        ClearCars();

        currentBusCapacity = carCapacity;
        activeBuses.Clear();
        currentBusIndex = 0;

        Vector3 busStopPos = new Vector3(0f, 0f, BusStopTransform.position.z);

        for (int i = 0; i < carCount; i++)
        {
            Vector3 spawnPos = (i == 0)
                ? busStopPos
                : busStopPos + new Vector3(i * -busSpacing, 0f, 0f);

            GameObject obj = Instantiate(busPrefab, spawnPos, Quaternion.identity);
            Bus bus = obj.GetComponent<Bus>();

            bus.color = busOrder[i];
            bus.SetCapacity(currentBusCapacity);
            bus.ApplyColor();

            activeBuses.Add(bus);

            if (i == 0)
            {
                GameManager.Instance.CurrentBus = bus;
            }
        }

        Debug.Log($"Spawned {activeBuses.Count} buses in order: {string.Join(", ", busOrder)}");
    }

    public void MoveNextBusToStop()
    {
        if (currentBusIndex + 1 < activeBuses.Count)
        {
            currentBusIndex++;

            Bus nextBus = activeBuses[currentBusIndex];
            Vector3 stopPos = new Vector3(0f, 0f, BusStopTransform.position.z);

            nextBus.Enter(stopPos);
            GameManager.Instance.CurrentBus = nextBus;

            for (int i = currentBusIndex + 1; i < activeBuses.Count; i++)
            {
                Bus bus = activeBuses[i];
                Vector3 newPos = bus.transform.position + new Vector3(busSpacing, 0f, 0f);
                StartCoroutine(MoveBusSmoothly(bus, newPos));
            }
        }
        else
        {
            Debug.Log("No more buses to move.");
        }
    }

    public void SpawnBus()
    {
        if (busQueue.Count == 0)
            return;

        Vector2 camSize = GetCameraWorldSize();
        float leftEdge = -camSize.x / 2f;

        Vector3 targetPosition = new Vector3(0f, 0f, BusStopTransform.position.z);
        Vector3 startPosition = new Vector3(leftEdge - 3f, 0f, BusStopTransform.position.z);

        PassengerColor color = busQueue.Dequeue();

        GameObject obj = Instantiate(busPrefab, startPosition, Quaternion.identity);
        Bus bus = obj.GetComponent<Bus>();

        bus.color = color;
        bus.SetCapacity(currentBusCapacity);
        bus.ApplyColor();

        bus.Enter(targetPosition);
    }

    public void ClearCars()
    {
        GameObject[] buses = GameObject.FindGameObjectsWithTag("Bus");

        Debug.Log("Found " + buses.Length + " buses to clear.");

        foreach (var bus in buses)
        {
            Destroy(bus);
            Debug.Log("Destroyed bus: " + bus.name);
        }

        busQueue.Clear();
        Debug.Log("All buses cleared and queue reset.");
    }
     

    private IEnumerator MoveBusSmoothly(Bus bus, Vector3 targetPos)
    {
        while (Vector3.Distance(bus.transform.position, targetPos) > 0.01f)
        {
            bus.transform.position = Vector3.MoveTowards(
                bus.transform.position,
                targetPos,
                BusMovingSpeed * Time.deltaTime);

            yield return null;
        }

        bus.transform.position = targetPos;
        Debug.Log("Bus shifted to: " + targetPos);
    }

    private Vector2 GetCameraWorldSize()
    {
        Camera cam = Camera.main;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        return new Vector2(width, height);
    }
     
}