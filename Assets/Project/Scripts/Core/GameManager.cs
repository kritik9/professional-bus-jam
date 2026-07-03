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
        //Time.timeScale = 0f;
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

        passenger.MoveAlongPath(() =>
        {
            HandlePassengerArrival(passenger);
        });
    }

    private void HandlePassengerArrival(PassengerController passenger)
    {
        Bus matchingBus = _busSpawner.GetBus(passenger.color);

        if (matchingBus != null && matchingBus.HasSpace())
        {
            matchingBus.Board(passenger);
            return;
        }

        MoveToWaitingArea(passenger);
    }

    #endregion

    #region Waiting Area

    public void MoveToWaitingArea(PassengerController passenger)
    {
        if (_waitingArea.HasSpace() && _waitingArea.OccupiedCount < _maxWaiting - 1)
        {
            if (passenger.currentCell != null)
            {
                passenger.currentCell.CurrentPassanger = null;
                passenger.currentCell = null;
            }

            _waitingArea.AddPassenger(passenger);
            passenger.SetWaiting(true);
            AutoBoardWaitingPassengers(passenger);
        }
        else
        {
            CheckLoseCondition();
        }
    }

    public void AutoBoardWaitingPassengers(PassengerController passenger)
    {
        if (_autoBoardCoroutine != null)
            StopCoroutine(_autoBoardCoroutine);

        passenger.SetWaiting(false);
        _autoBoardCoroutine = StartCoroutine(AutoBoardCoroutine());
    }
    public void AutoBoardWaitingPassengers()
    {
        if (_autoBoardCoroutine != null)
            StopCoroutine(_autoBoardCoroutine);

        _autoBoardCoroutine = StartCoroutine(AutoBoardCoroutine());
    }

    private IEnumerator AutoBoardCoroutine()
    {
        bool boardedSomeone;

        do
        {
            boardedSomeone = false;

            foreach (PassengerColor color in System.Enum.GetValues(typeof(PassengerColor)))
            {
                Bus bus = _busSpawner.GetBus(color);

                if (bus == null || !bus.HasSpace())
                    continue;

                List<PassengerController> passengers =
                    _waitingArea.GetMatchingPassengers(color);

                if (passengers.Count > 0)
                {
                    bus.Board(passengers[0]);
                    boardedSomeone = true;
                    yield return new WaitForSeconds(0.05f);
                }
            }

        } while (boardedSomeone);

        _autoBoardCoroutine = null;
    }

    #endregion

    #region Bus Events

    public void OnBusLeft(Bus bus)
    {
        _busSpawner.BusLeft(bus);

        AutoBoardWaitingPassengers();

        if (AllPassengersFinished())
        {
            SetState(GameState.Win);
            LevelProgressManager.Instance.CompleteLevel(_currentLevelIndex);
            SaveProgress();
            return;
        }

        if (_busSpawner.RemainingBusCount == 0)
        {
            SetState(GameState.Lose);
        }
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

    #endregion

    #region UI

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