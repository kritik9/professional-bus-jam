using UnityEngine;
using UnityEngine.UI;
using static GameEnums;
using TMPro;
public class UiManager : MonoBehaviour
{  
    public TextMeshProUGUI CurrentLevelText;
    public TextMeshProUGUI NextLevelText;
    public Button PlayButton;
    public Button NextLevelButtonInGameWin;
    public Button StartMainGameButton;
    public Button RestartButton;
    public Button MainMenuButtonInWin;
    public Button MainMenuButtonInLose;
    public Button BackButton;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject LevelScreen;
    public GameObject MenuScreen;
    public GameObject AllLevelCompletedScreen; 

    private void Start()
    { 
        PlayButton.onClick.AddListener(() => GameManager.Instance.OnPlayButtonClicked());
        //StartMainGameButton.onClick.AddListener(() => GameManager.Instance.OnLevelStartButtonClicked());   
        NextLevelButtonInGameWin.onClick.AddListener(() => GameManager.Instance.NextLevel());   
        RestartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        MainMenuButtonInWin.onClick.AddListener(() => GameManager.Instance.OnMainMenuButtonClicked());
        MainMenuButtonInLose.onClick.AddListener(() => GameManager.Instance.OnMainMenuButtonClicked());
        BackButton.onClick.AddListener(() => GameManager.Instance.OnBackButtonClicked());


    }
    public void ShowMenu()
    {
        HideAllPanels();
        MenuScreen.SetActive(true);  
    }
    public void ShowAllLevelCompleted()
    {
        HideAllPanels();
        AllLevelCompletedScreen.SetActive(true);
    }
    public void ShowLevelSelect()
    {
        HideAllPanels();
        LevelScreen.SetActive(true); 
        
        if (LevelSelectionGenerator.Instance != null)
        {
            LevelSelectionGenerator.Instance.GenerateLevels();
        }
         else
        {
            Debug.LogError("LevelSpawner not found in scene!");
        }
    }

    public void ShowWinPanel()
    {
        HideAllPanels();
        winPanel.SetActive(true);
        NextLevelText.text = "Next Level " + (GameManager.Instance.CurrentLevelIndex + 2);
    }

    public void ShowLosePanel()
    {
        HideAllPanels();
        losePanel.SetActive(true);
    }

    public void HideAllPanels()
    {
        LevelScreen.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        if (MenuScreen != null) MenuScreen.SetActive(false);
    }

    public void UpdateLevelText(int level)
    {
        CurrentLevelText.text = "Level " + level;
    }
    
     
}
 
