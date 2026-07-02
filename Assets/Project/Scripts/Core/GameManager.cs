using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEnums;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    [SerializeField] private int _maxWaiting = 6;
    public int MaxWaiting => _maxWaiting;

    private int _currentLevelIndex;
    public int CurrentLevelIndex => _currentLevelIndex;
     
    [Header("Managers")]
    [SerializeField] public LevelManager _levelManager;
    [SerializeField] public BusSpawner _busSpawner;
    [SerializeField] public UiManager _uiManager;

    [Header("Systems")]
    [SerializeField] public GridGenerator _grid;
    [SerializeField] public PassengerSpawner _passengerSpawner;
    [SerializeField] public WaitingAreaController _waitingArea;
     

     
    public Bus CurrentBus { get; set; }

    private Coroutine _autoBoardCoroutine;
      
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        _currentLevelIndex = PlayerPrefs.GetInt("LEVEL", 0);
    }
     


    #region State Management

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                EnterMenuState();
                break;

            case GameState.LevelSelect:
                EnterLevelSelectState();
                break;

            case GameState.Playing:
                EnterPlayingState();
                break;

            case GameState.Win:
                EnterWinState();
                break;

            case GameState.Lose:
                EnterLoseState();
                break;
        }
    }

    private void EnterMenuState()
    {
        Time.timeScale = 0f;
        _uiManager.ShowMenu();
    }

    private void EnterLevelSelectState()
    {
        Time.timeScale = 0f;
        _uiManager.ShowLevelSelect();
        _uiManager.UpdateLevelText(_currentLevelIndex + 1);
    }

    private void EnterPlayingState()
    {
        Time.timeScale = 1f;
        _uiManager.HideAllPanels();
        StartLevel();
    }

    private void EnterWinState()
    {
        Time.timeScale = 0f;
        _uiManager.ShowWinPanel();
    }

    private void EnterLoseState()
    {
        Time.timeScale = 0f;
        _uiManager.ShowLosePanel();
    }

    #endregion


    #region Level Flow

    private void StartLevel()
    {
        CurrentState = GameState.Playing;
        _levelManager.LoadLevel(_currentLevelIndex);
        _uiManager.UpdateLevelText(_currentLevelIndex + 1);
    }

    public void RestartLevel()
    {
        _levelManager.ClearLevel();
        SetState(GameState.Playing);
    }

    public void NextLevel()
    {
        _currentLevelIndex++;
        PlayerPrefs.SetInt("LEVEL", _currentLevelIndex);

        if (_currentLevelIndex >= _levelManager.levelDatabase.levels.Count)
        {
            _uiManager.ShowAllLevelCompleted();
            _currentLevelIndex = _levelManager.levelDatabase.levels.Count - 1;
            return;
        }

        SetState(GameState.Playing);
    }

    public void LoadSpecificLevel(int levelIndex)
    {
        if (!LevelProgressManager.Instance.IsLevelUnlocked(levelIndex))
            return;

        levelIndex = Mathf.Clamp(levelIndex, 0, _levelManager.levelDatabase.levels.Count - 1);

        _currentLevelIndex = levelIndex;
        SetState(GameState.Playing);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("LEVEL", _currentLevelIndex);
    }

    #endregion


    #region Passenger Handling

    public bool CanMovePassenger(PassengerController passenger)
    {
        if (passenger.currentCell == null || passenger.isMoving)
            return false;

        return _grid.IsPathClear(passenger.currentCell, passenger.arrowDirection);
    }

    public void TryMovePassenger(PassengerController passenger)
    {
        if (CurrentState != GameState.Playing)
            return;

        if (!_grid.IsPathClear(passenger.currentCell, passenger.arrowDirection))
            return;

        if (CurrentBus == null)
            return;

        passenger.MoveAlongPath(() =>
        {
            HandlePassengerArrival(passenger);
        });
    }

    private void HandlePassengerArrival(PassengerController passenger)
    {
        if (passenger.color == CurrentBus.color && CurrentBus.HasSpace())
        {
            CurrentBus.Board(passenger);
            return;
        }

        MoveToWaitingArea(passenger);
    }

    #endregion


    #region Waiting Logic

    public  void MoveToWaitingArea(PassengerController passenger)
    {
        if (_waitingArea.HasSpace() && _waitingArea.OccupiedCount < _maxWaiting - 1)
        {
            if (passenger.currentCell != null)
            {
                passenger.currentCell.CurrentPassanger = null;
                passenger.currentCell = null;
            }

            _waitingArea.AddPassenger(passenger);
        }
        else
        {
            CheckLoseCondition();
        }
    }

    public void AutoBoardWaitingPassengers()
    {
        if (CurrentBus == null)
            return;

        if (_autoBoardCoroutine != null)
            StopCoroutine(_autoBoardCoroutine);

        _autoBoardCoroutine = StartCoroutine(AutoBoardCoroutine());
    }

    private IEnumerator AutoBoardCoroutine()
    {
        while (CurrentBus != null)
        {
            List<PassengerController> matches =
                _waitingArea.GetMatchingPassengers(CurrentBus.color);

            if (matches.Count == 0 || !CurrentBus.HasSpace())
                break;

            CurrentBus.Board(matches[0]);
            yield return new WaitForSeconds(0.05f);
        }

        _autoBoardCoroutine = null;
    }

    #endregion

     

    public void OnBusLeft()
    {
        if (AllPassengersFinished())
        {
            SetState(GameState.Win);
            LevelProgressManager.Instance.CompleteLevel(_currentLevelIndex);
            SaveProgress();
            return;
        }

        _busSpawner.MoveNextBusToStop();

        if (_busSpawner.RemainingBusCount == 0)
            SetState(GameState.Lose);
    }

    private bool AllPassengersFinished()
    {
        foreach (var pair in _passengerSpawner.colorCounts)
        {
            if (pair.Value > 0)
                return false;
        }

        return true;
    }
     


    #region UI Button Hooks

    public void OnPlayButtonClicked() => SetState(GameState.LevelSelect);
    public void OnLevelStartButtonClicked() => SetState(GameState.Playing);
    public void OnMainMenuButtonClicked() => SetState(GameState.Menu);

    public void OnBackButtonClicked()
    {
        if (CurrentState == GameState.Playing)
            SetState(GameState.Menu);
    }

    #endregion

     

    private void CheckLoseCondition()
    {
        SetState(GameState.Lose);
    }
     
}



//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UIElements;
//using static GameEnums;
//using static UnityEngine.Rendering.DebugUI.Table;

//public class GameManager : MonoBehaviour
//{

//    public static GameManager Instance;
//    public GameState CurrentState { get; private set; }

//    [Header("Manager")]
//    [SerializeField] private LevelManager levelManager;

//    [SerializeField] public GridGenerator passengerGrid;


//    public PassengerSpawner PassengerSpawner;
//    public Bus currentBus;
//    [SerializeField] private BusSpawner BusSpawner;

//    [SerializeField] private Transform waitingParent; 

//    [SerializeField] public int maxWaiting = 6;
//    [HideInInspector]
//    public int currentLevelIndex;


//    [SerializeField] public WaitingAreaController waitingSlot;
//    [SerializeField] private UiManager uiManager;

//    private Coroutine autoBoardCoroutine;  

//    private void Awake()
//    {
//        Instance = this;
//    }

//    private void Start()
//    {
//        currentLevelIndex = PlayerPrefs.GetInt("LEVEL", 0);
//    }

//    public void NextLevelWithButton()
//    {
//        NextLevel();
//    }
//    public void SetState(GameState newState)
//    {
//        CurrentState = newState;
//        switch (newState)
//        {
//            case GameState.Menu:
//                Time.timeScale = 0f;
//                uiManager.ShowMenu();
//                break;
//            case GameState.LevelSelect:
//                Time.timeScale = 0f;
//                uiManager.ShowLevelSelect();
//                uiManager.UpdateLevelText(currentLevelIndex + 1);
//                break;
//            case GameState.Playing:
//                Time.timeScale = 1f;
//                uiManager.HideAllPanels();
//                StartLevel();
//                break;
//            case GameState.Win:
//                Time.timeScale = 0f;
//                uiManager.ShowWinPanel();
//                break;
//            case GameState.Lose:
//                Time.timeScale = 0f;
//                uiManager.ShowLosePanel();
//                break;
//        }
//    }
//    public void StartLevel()
//    {
//        CurrentState = GameState.Playing;
//        levelManager.LoadLevel(currentLevelIndex);
//        uiManager.UpdateLevelText(currentLevelIndex + 1);
//    }

//    public void TryMovePassenger(PassengerController p)
//    {
//        if (CurrentState != GameState.Playing)
//            return; 
//        if (!passengerGrid.IsPathClear(p.currentCell, p.arrowDirection))
//        {
//            Debug.Log("Path blocked");
//            return;
//        } 
//        if (currentBus == null)
//        {
//            Debug.Log("No active bus");
//            return;
//        } 
//        p.MoveAlongPath(() =>
//        {
//            if (p.color == currentBus.color)
//            {
//                if (currentBus.HasSpace())
//                {
//                    currentBus.Board(p);
//                    return;
//                } 
//            } 
//            MoveToWaitingArea(p);
//        });
//    }
//    public void MoveToWaitingArea(PassengerController p)
//    { 
//        Debug.Log("OccupiedCount: " + waitingSlot.OccupiedCount + ", HasSpace: " + waitingSlot.HasSpace());
//        if (waitingSlot.HasSpace() && waitingSlot.OccupiedCount < maxWaiting - 1)
//        { 
//            if (p.currentCell != null)
//            {
//                p.currentCell.CurrentPassanger = null;
//                p.currentCell = null;
//            }
//            waitingSlot.AddPassenger(p);
//        }
//        else
//        {
//            Debug.Log("Game over condition");
//            CheckLoseCondition();
//        }
//    }

//    public void AutoBoardWaitingPassengers()
//    {
//        if (currentBus == null)
//            return;
//        if (autoBoardCoroutine != null)
//            StopCoroutine(autoBoardCoroutine);  
//        autoBoardCoroutine = StartCoroutine(AutoBoardCoroutine());
//    }
//    IEnumerator AutoBoardCoroutine()
//    {
//        while (true)
//        {
//            if (currentBus == null)  
//                break;
//            List<PassengerController> toBoard = waitingSlot.GetMatchingPassengers(currentBus.color);
//            if (toBoard.Count == 0 || !currentBus.HasSpace())
//                break;
//            PassengerController p = toBoard[0];
//            currentBus.Board(p);
//            yield return new WaitForSeconds(0.05f);
//        }
//        autoBoardCoroutine = null;  
//    }
//    void SaveProgress()
//    {
//        PlayerPrefs.SetInt("LEVEL", currentLevelIndex);
//    }

//    public void RestartLevel()
//    {
//        levelManager.ClearLevel();
//        SetState(GameState.Playing); 
//    } 
//    public void NextLevel()
//    {
//        currentLevelIndex++;
//        PlayerPrefs.SetInt("LEVEL", currentLevelIndex);

//        if (currentLevelIndex >= levelManager.levelDatabase.levels.Count)
//        {
//            Debug.Log("ALL LEVELS COMPLETE");
//            uiManager.ShowAllLevelCompleted();
//            currentLevelIndex = levelManager.levelDatabase.levels.Count - 1;
//            return;
//        }
//        SetState(GameState.Playing);
//    }
//    public bool CanMovePassenger(PassengerController p)
//    {
//        if (p.currentCell == null || p.isMoving)
//            return false;

//        return passengerGrid.IsPathClear(p.currentCell, p.arrowDirection);
//    }

//    void CheckLoseCondition()
//    {
//        Debug.Log("GAME OVER");
//        SetState(GameState.Lose);
//    }

//    public void OnBusLeft()
//    {
//        if (AllPassengersFinished())
//        {
//            SetState(GameState.Win);
//            LevelProgressManager.Instance.CompleteLevel(currentLevelIndex);
//            SaveProgress();
//            return;
//        }
//        BusSpawner.MoveNextBusToStop();
//        if (BusSpawner.RemainingBusCount == 0)
//        {
//            SetState(GameState.Lose);
//            return;
//        } 
//    }

//    public void LoadSpecificLevel(int levelIndex)
//    {
//        if (!LevelProgressManager.Instance.IsLevelUnlocked(levelIndex))
//        {
//            Debug.Log("Level is locked!");
//            return;
//        }
//        if (levelIndex < 0 || levelIndex >= levelManager.levelDatabase.levels.Count)
//        {
//            Debug.LogError("Invalid level index: " + levelIndex + ". Max levels: " + levelManager.levelDatabase.levels.Count);
//            levelIndex = Mathf.Clamp(levelIndex, 0, levelManager.levelDatabase.levels.Count - 1);
//        } 
//        currentLevelIndex = levelIndex;
//        Debug.Log("Loading specific level: " + (currentLevelIndex + 1));
//        SetState(GameState.Playing);
//    }
//    bool AllPassengersFinished()
//    {
//        foreach (var kvp in PassengerSpawner.colorCounts)
//        {
//            if (kvp.Value > 0)
//                return false;
//        }
//        return true;
//    }

//    public void OnPlayButtonClicked()
//    {
//        SetState(GameState.LevelSelect);
//    }

//    public void OnLevelStartButtonClicked()
//    {
//        SetState(GameState.Playing);
//    }

//    public void OnMainMenuButtonClicked()
//    {
//        SetState(GameState.Menu);
//    }
//    public void OnBackButtonClicked()
//    {
//        if (CurrentState == GameState.Playing)
//        {
//            SetState(GameState.Menu);

//        }
//    }
//}
