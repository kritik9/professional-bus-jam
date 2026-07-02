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


    //public void SpawnPassengers(List<PassengerColor> busOrder, int capacity)
    //{

    //    Dictionary<PassengerColor, int> passengerCounts = new Dictionary<PassengerColor, int>();
    //    foreach (var color in busOrder)
    //    {
    //        if (!passengerCounts.ContainsKey(color))
    //            passengerCounts[color] = 0;
    //        passengerCounts[color] += capacity;
    //    }

    //    this.colorCounts = new Dictionary<PassengerColor, int>(passengerCounts);

    //    if (passengerCounts.Count == 0 || passengerCounts.All(kvp => kvp.Value == 0))
    //    {
    //        Debug.LogError("passengerCounts is empty or all counts are 0! No passengers to spawn. Set cars in Level Designer.");
    //        return;
    //    }

    //    List<GridCell> cells = grid.GetAllCells(); 

    //    if (cells.Count == 0)
    //    {
    //        Debug.LogError("No cells available for spawning passengers!");
    //        return;
    //    }

    //    List<PassengerColor> shuffledColors = BuildShuffledColorList(passengerCounts); 

    //    for (int i = 0; i < shuffledColors.Count && i < cells.Count; i++)
    //    {
    //        GridCell cell = cells[i];
    //        PassengerColor color = shuffledColors[i];
    //        Direction dir = GetValidDirection(cell);
    //        SpawnPassenger(cell, color, dir); 
    //    }
    //}

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

    Direction GetValidDirection(GridCell cell)
    {
        // 1 For Random Direction


        //List<Direction> validDirections = new();

        //foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        //{
        //    if (grid.IsPathClear(cell, dir))
        //        validDirections.Add(dir);
        //}

        //if (validDirections.Count == 0)
        //    return Direction.Forward;

        //return validDirections[Random.Range(0, validDirections.Count)];


        //3 For Mid Level To Add Ring
        if (GameManager.Instance.CurrentLevelIndex == 4)
        {
            if (cell.z == grid.MaxZ - 1) return Direction.Right;
            if (cell.x == grid.MaxX - 1) return Direction.Backward;
            if (cell.z == 1) return Direction.Left;
            return Direction.Forward;

        }

        // 2 For Easy Level To edge Corners

        int centerX = grid.MaxX / 2;
        int centerZ = grid.MaxZ / 2;

        if (cell.x < centerX) return Direction.Left;
        if (cell.x > centerX) return Direction.Right;
        if (cell.z < centerZ) return Direction.Backward;
        return Direction.Forward;



        // 4 For Hard Level To (Quadrants)

        //bool left = cell.x < grid.MaxX / 2;
        //bool bottom = cell.z < grid.MaxZ / 2;

        //if (left && bottom) return Direction.Right;
        //if (!left && bottom) return Direction.Forward;
        //if (left && !bottom) return Direction.Backward;
        //return Direction.Left;
    }


    List<PassengerColor> BuildShuffledColorList(
    Dictionary<PassengerColor, int> colorCounts)
    {
        List<PassengerColor> list = new();

        foreach (var kvp in colorCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
                list.Add(kvp.Key);
        }

        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }

        return list;
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

