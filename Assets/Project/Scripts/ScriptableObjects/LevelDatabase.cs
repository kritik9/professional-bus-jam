using System.Collections.Generic;
using UnityEngine;
using static GameEnums;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelConfig> levels;
}

[System.Serializable]
public class ColorCarCount
{
    public PassengerColor color;
    public int count;
}


[System.Serializable]
public class LevelConfig
{
    public int gridRows = 3;
    public int gridCols = 3;
    public int carCapacity = 3;
    [Range(1, 10)]
    public int colorCount = 2;
    public List<PassengerColor> busOrder = new List<PassengerColor>();
    public List<PassengerColor> passengerColors = new List<PassengerColor>();  
    public List<Direction> passengerDirections = new List<Direction>(); 

    public Difficulty difficulty = Difficulty.Easy;
}


