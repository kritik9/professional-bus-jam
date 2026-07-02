using System.Collections.Generic;
using UnityEngine;
using static GameEnums;

public class LevelProgressManager : MonoBehaviour
{
   
    public  static LevelProgressManager Instance; 

    public Dictionary<int, LevelState> levelStates = new Dictionary<int, LevelState>();
    private const string LevelStateKeyPrefix = "LevelState_";   

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
    }
      private void Start()
    {
        LoadLevelStates();
    }
    private void LoadLevelStates()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.levelDatabase == null)
        {
            Debug.LogError("LevelManager or levelDatabase is null! Ensure LevelManager is in scene.");
            return;
        }
        int totalLevels = LevelManager.Instance.levelDatabase.levels.Count;   
        for (int i = 0; i < totalLevels; i++)
        {
            string key = LevelStateKeyPrefix + i;
            if (PlayerPrefs.HasKey(key))
            {
                levelStates[i] = (LevelState)PlayerPrefs.GetInt(key);
            }
            else
            {
                levelStates[i] = (i == 0) ? LevelState.Unlocked : LevelState.Locked;
            }
        }
    }


    private void SaveLevelState(int levelIndex, LevelState state)
    {
        levelStates[levelIndex] = state;
        PlayerPrefs.SetInt(LevelStateKeyPrefix + levelIndex, (int)state);
        PlayerPrefs.Save();
    }


    public LevelState GetLevelState(int levelIndex)
    {
        return levelStates.ContainsKey(levelIndex) ? levelStates[levelIndex] : LevelState.Locked;
    }


    public void CompleteLevel(int levelIndex)
    {
        SaveLevelState(levelIndex, LevelState.Completed);


        int nextLevel = levelIndex + 1;
        if (nextLevel < levelStates.Count && GetLevelState(nextLevel) == LevelState.Locked)
        {
            SaveLevelState(nextLevel, LevelState.Unlocked);
        }
    }
        

    public bool IsLevelUnlocked(int levelIndex)
    {
        return GetLevelState(levelIndex) == LevelState.Unlocked || GetLevelState(levelIndex) == LevelState.Completed;
    }
} 
