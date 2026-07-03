using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameEnums;
public class PassengerSpawner : MonoBehaviour
{
    public GridGenerator grid;
    public GameObject passengerPrefab;
    public int passengerCount = 20;

    public Dictionary<PassengerColor, int> colorCounts = new();


    private List<PassengerController> spawnedPassengers = new();

    public void SpawnPassengers(List<PassengerColor> passengerColors, List<Direction> passengerDirections, int gridRows, int gridCols)
    {
        colorCounts.Clear();
         
        foreach (var color in passengerColors)
        {
            if (!colorCounts.ContainsKey(color))
                colorCounts[color] = 0;
            colorCounts[color]++;
        }

        if (colorCounts.Count == 0 || colorCounts.All(kvp => kvp.Value == 0))
        {
            Debug.LogError("No passengers to spawn. Design the grid in Level Designer.");
            return;
        }

        List<GridCell> cells = grid.GetAllCells();

        if (cells.Count == 0)
        {
            Debug.LogError("No cells available for spawning passengers!");
            return;
        }
         
        for (int i = 0; i < passengerColors.Count && i < cells.Count; i++)
        {
            GridCell cell = cells[i];
            PassengerColor color = passengerColors[i];
            Direction dir = passengerDirections[i];
            SpawnPassenger(cell, color, dir);
        }
    }

    void SpawnPassenger(GridCell cell, PassengerColor color, Direction dir)
    {
        GameObject obj = Instantiate(passengerPrefab);
        PassengerController p = obj.GetComponent<PassengerController>();

        p.color = color;
        p.arrowDirection = dir;
        p.arrowPrefab.SetActive(true);
        p.ApplyColor();
        p.SetCell(cell);
        p.InitializeArrow();
        spawnedPassengers.Add(p);
        Debug.Log($"Spawned {color} passenger at ({cell.x}, {cell.z}) facing {dir}");
    }
    public void ClearPassengers()
    {
        foreach (var passenger in spawnedPassengers)
        {
            if (passenger != null)
            {
                if (passenger.currentCell != null)
                    passenger.currentCell.CurrentPassanger = null;

                Destroy(passenger.gameObject);
            }
        }

        spawnedPassengers.Clear();
    }

}

