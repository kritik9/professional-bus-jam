using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameEnums;

public class LevelSelectionGenerator : MonoBehaviour
{
    public GameObject levelPrefab;
    public Transform contentParent; 
      public static LevelSelectionGenerator Instance;

    private void Awake()
        {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    

    public void GenerateLevels()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        if (LevelManager.Instance == null || LevelManager.Instance.levelDatabase == null)
        {
            Debug.LogError("LevelManager or levelDatabase is null!");
            return;
        }
        int totalLevels = LevelManager.Instance.levelDatabase.levels.Count;   

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject levelObj = Instantiate(levelPrefab, contentParent);
            LevelSelectionItemView levelData = levelObj.GetComponent<LevelSelectionItemView>();
            levelData.levelText.text = i.ToString(); 

            Button btn = levelObj.GetComponent<Button>();
            if (levelData == null)
            {
                Debug.LogError("LevelDataItem component missing on prefab!");
                return;
            }
            if(LevelProgressManager.Instance == null) { Debug.LogError("LevelProgressManager instance not found!"); return; }
            LevelState state = LevelProgressManager.Instance.GetLevelState(i - 1);  

            if (state == LevelState.Unlocked || state == LevelState.Completed)
            {
                levelData.lockIcon.SetActive(false);
                btn.interactable = true;
                 
                if (state == LevelState.Completed)
                {
                    levelData.completedIcon.SetActive(true);
                }
                else
                {
                    levelData.completedIcon.SetActive(false);
                }

                int levelIndex = i - 1;  
                btn.onClick.AddListener(() => LoadLevel(levelIndex));
            }
            else  
            {
                levelData.lockIcon.SetActive(true);
                levelData.completedIcon.SetActive(false);
                btn.interactable = false; 
            }
        }
    }

  
    void LoadLevel(int levelIndex)
    { 
        if (LevelProgressManager.Instance.IsLevelUnlocked(levelIndex))
        {
             
            
            GameManager.Instance.LoadSpecificLevel(levelIndex);  
        }
        else
        {
            Debug.Log("Level is locked!");
            // Optional: Show UI message like "Unlock previous levels first"
        }
    }
     
}