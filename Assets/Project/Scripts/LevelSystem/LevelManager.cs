using System.Collections.Generic;
using UnityEngine;
using static GameEnums;
public class LevelManager : MonoBehaviour
{
    public LevelDatabase levelDatabase;

    public static LevelManager Instance;
    public GridGenerator grid;
    public PassengerSpawner PassengerSpawner;
    public BusSpawner BusSpawner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
    }



    public void LoadLevel(int index)
    {
        ClearLevel();

        LevelConfig data = levelDatabase.levels[index];

        int expectedSize = data.gridRows * data.gridCols;
        if (data.passengerColors.Count != expectedSize || data.passengerDirections.Count != expectedSize)
        {
            Debug.LogError($"Level data mismatch at index {index}: Expected {expectedSize} items, but passengerColors has {data.passengerColors.Count} and passengerDirections has {data.passengerDirections.Count}. Skipping level load.");
            return;
        }

        List<PassengerColor> rotatedColors = RotateGridClockwise(data.passengerColors, data.gridRows, data.gridCols);
        List<Direction> rotatedDirections = RotateGridClockwise(data.passengerDirections, data.gridRows, data.gridCols);

        int newGridRows = data.gridCols;
        int newGridCols = data.gridRows;

        grid.SetGridSize(newGridRows + 2, newGridCols + 2);
        grid.GenerateGrid();

        int totalCharacters = data.gridRows * data.gridCols;

        int carCount = Mathf.CeilToInt((float)totalCharacters / data.carCapacity);

        //BusSpawner.SetupBuses(carCount, data.carCapacity, data.busOrder);
        BusSpawner.SetupBuses(data.carCapacity, data.busOrder);

        PassengerSpawner.SpawnPassengers(rotatedColors, rotatedDirections, newGridRows, newGridCols);

        Debug.Log($"Total Characters: {totalCharacters}");
    }

    // Function to rotate a list representing a grid 90 degrees clockwise
    // Works for any rows x cols grid
    private List<T> RotateGridClockwise<T>(List<T> original, int rows, int cols)
    {
        List<T> rotated = new List<T>(new T[original.Count]);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                int originalIndex = i * cols + j;
                int rotatedIndex = j * rows + (rows - 1 - i);
                // Added bounds check to prevent ArgumentOutOfRangeException
                if (originalIndex >= original.Count || rotatedIndex >= rotated.Count)
                {
                    Debug.LogError($"Index out of range in RotateGridClockwise: originalIndex={originalIndex}, rotatedIndex={rotatedIndex}, original.Count={original.Count}, rotated.Count={rotated.Count}");
                    continue; // Skip invalid indices
                }
                rotated[rotatedIndex] = original[originalIndex];
            }
        }
        return rotated;
    }
    public  void ClearLevel()
    {
        Debug.Log("Clearing level...");
        PassengerSpawner.ClearPassengers();
        BusSpawner.ClearCars();
        grid.ClearGrid();

    }
    Vector2Int GetBestGridSize(int total)
    {
        int bestRow = 1;
        int bestCol = total;
        int minDiff = total;  

        for (int i = 1; i <= Mathf.Sqrt(total); i++)
        {
            if (total % i == 0)
            {
                int row = i;
                int col = total / i;

                int diff = Mathf.Abs(row - col);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestRow = row;
                    bestCol = col;
                }
            }
        }
        Debug.Log($"Best Grid Size: {bestRow} rows x {bestCol} columns");
        return new Vector2Int(bestRow, bestCol);
    }

}
