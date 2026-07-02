using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using static GameEnums;

public class LevelDesignerWindow : EditorWindow
{
    private LevelDatabase levelData;
    private int currentLevelIndex = 0;
    private Vector2 scrollPos;

    private PassengerColor selectedToolColor = PassengerColor.Red;
    private Direction selectedToolDirection = Direction.Forward;

    [MenuItem("Tools/Level Designer")]
    static void OpenWindow()
    {
        GetWindow<LevelDesignerWindow>("Level Designer");
    }

    void OnEnable()
    {
        levelData = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Resources/LevelData.asset");

        if (levelData == null)
        {
            levelData = CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(levelData, "Assets/Resources/LevelData.asset");
            AssetDatabase.SaveAssets();
        }
    }

    void OnGUI()
    {
        if (levelData == null) return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader();
        DrawLevelNavigation();

        if (levelData.levels.Count > 0)
        {
            DrawCurrentLevel();
        }

        DrawBottomButtons();

        EditorGUILayout.EndScrollView();
    }

    #region UI Sections

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Level Designer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
    }

    void DrawLevelNavigation()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Prev"))
            currentLevelIndex = Mathf.Max(0, currentLevelIndex - 1);
         
        EditorGUILayout.LabelField(
            $"Level {currentLevelIndex + 1}/{levelData.levels.Count}",
            GUILayout.Width(120));

        if (GUILayout.Button("Next"))
            currentLevelIndex = Mathf.Min(levelData.levels.Count - 1, currentLevelIndex + 1);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    void DrawCurrentLevel()
    {
        LevelConfig config = levelData.levels[currentLevelIndex];

        config.gridRows = EditorGUILayout.IntField("Grid Rows ", config.gridRows);
        config.gridCols = EditorGUILayout.IntField("Grid Cols ", config.gridCols);
        //config.carCapacity = EditorGUILayout.IntField("Car Capacity", config.carCapacity);

        if (config.gridRows <= 0) config.gridRows = 1;
        if (config.gridCols <= 0) config.gridCols = 1;
        if (config.carCapacity <= 0) config.carCapacity = 3;

        //config.gridRows = Mathf.Clamp(config.gridRows, 1, 8);
        //config.gridCols = Mathf.Clamp(config.gridCols, 1, 7);


        int totalCells = config.gridRows * config.gridCols;

        bool isDivisibleBy3 = totalCells % 3 == 0;
        if (!isDivisibleBy3)
        {
            EditorGUILayout.HelpBox("Grid size (Rows * Cols) must be divisible by 3 (Car Capacity). Current total: " + totalCells, MessageType.Error);
        }

        ResizeList(ref config.passengerColors, totalCells, PassengerColor.Red);
        ResizeList(ref config.passengerDirections, totalCells, Direction.Forward);

        int carCount = Mathf.CeilToInt((float)totalCells / config.carCapacity);
        EditorGUILayout.LabelField($"Calculated Car Count: {carCount}");

        ResizeList(ref config.busOrder, carCount, PassengerColor.Red);

        DrawBusQueue(config);
        DrawTools();
        Debug.Log(totalCells);
        DrawGrid(config);

        EditorGUILayout.LabelField("Difficulty: " + config.difficulty.ToString());
         
        //var solveResult = LevelSolver.SolveLevel(config);
        //bool isSolvable = solveResult.isSolvable;
        //if (!isSolvable)
        //{
        //    EditorGUILayout.HelpBox("Level is not solvable: " + solveResult.message, MessageType.Warning);
        //}

        //bool canSave = isDivisibleBy3 && isSolvable;
        //GUI.enabled = canSave;
        if (GUILayout.Button("Save Level"))
        {
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
            Debug.Log("Level Saved");
        }
        GUI.enabled = true;
        EditorGUILayout.Space();
    }

    void DrawBusQueue(LevelConfig config)
    {
        EditorGUILayout.LabelField("Bus Spawn Queue:", EditorStyles.boldLabel);

        float cellSize = 40f;
        float spacing = 8f;

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Space(5);

        for (int i = 0; i < config.busOrder.Count; i++)
        {
            Rect rect = GUILayoutUtility.GetRect(
                cellSize,
                cellSize,
                GUILayout.Width(cellSize),
                GUILayout.Height(cellSize)
            );

            if (GUI.Button(rect, GUIContent.none))
                config.busOrder[i] = selectedToolColor;
             

            EditorGUI.DrawRect(rect, GetUnityColor(config.busOrder[i]));
             
            GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel);
            indexStyle.alignment = TextAnchor.MiddleCenter;
            indexStyle.normal.textColor = Color.black;

            GUI.Label(rect, (i + 1).ToString(), indexStyle);

            GUILayout.Space(spacing);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    void DrawTools()
    {
        EditorGUILayout.LabelField("Tools:", EditorStyles.boldLabel);

        float cellSize = 40f;
        float spacing = 8f;

        EditorGUILayout.BeginVertical("box");
         

        EditorGUILayout.LabelField("Colors:");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);

        foreach (PassengerColor color in System.Enum.GetValues(typeof(PassengerColor)))
        {
            Rect rect = GUILayoutUtility.GetRect(
                cellSize,
                cellSize,
                GUILayout.Width(cellSize),
                GUILayout.Height(cellSize)
            );

            if (GUI.Button(rect, GUIContent.none))
                selectedToolColor = color;

            EditorGUI.DrawRect(rect, GetUnityColor(color));
             
            if (selectedToolColor == color)
            {
                DrawSelectionBorder(rect);
            }

            GUILayout.Space(spacing);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);



        EditorGUILayout.LabelField("Directions:");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);

        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            Rect rect = GUILayoutUtility.GetRect(
                cellSize,
                cellSize,
                GUILayout.Width(cellSize),
                GUILayout.Height(cellSize)
            );

            if (GUI.Button(rect, GUIContent.none))
                selectedToolDirection = dir;

            EditorGUI.DrawRect(rect, Color.white);

            GUIStyle arrowStyle = new GUIStyle(EditorStyles.boldLabel);
            arrowStyle.alignment = TextAnchor.MiddleCenter;
            arrowStyle.fontSize = 20;
            arrowStyle.normal.textColor = Color.black;

            GUI.Label(rect, GetArrowSymbol(dir), arrowStyle);
             

            if (selectedToolDirection == dir)
            {
                DrawSelectionBorder(rect);
            }

            GUILayout.Space(spacing);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    void DrawGrid(LevelConfig config)
    {
        EditorGUILayout.LabelField("Passenger Grid:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        float cellSize = 40f;
        float spacingX = 6f;
        float spacingY = 6f;

        GUIStyle arrowStyle = new GUIStyle(EditorStyles.boldLabel);
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.fontSize = 18;
        arrowStyle.normal.textColor = Color.black;
         
        for (int row = 0; row < config.gridRows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(5);

            for (int col = 0; col < config.gridCols; col++)
            {
                int index = row * config.gridCols + col;

                Rect rect = GUILayoutUtility.GetRect(
                    cellSize,
                    cellSize,
                    GUILayout.Width(cellSize),
                    GUILayout.Height(cellSize)
                );

                if (GUI.Button(rect, GUIContent.none))
                {
                    config.passengerColors[index] = selectedToolColor;
                    config.passengerDirections[index] = selectedToolDirection;
                }

                EditorGUI.DrawRect(rect, GetUnityColor(config.passengerColors[index]));

                GUI.Label(rect, GetArrowSymbol(config.passengerDirections[index]), arrowStyle);

                GUILayout.Space(spacingX);
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(spacingY);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    void DrawBottomButtons()
    {
        //if (GUILayout.Button("Regenerate Selected Level"))
        //{
        //    LevelGenerator.Generate(levelData.levels[currentLevelIndex].gridRows, levelData.levels[currentLevelIndex].gridCols, levelData.levels[currentLevelIndex].colorCount, levelData.levels[currentLevelIndex].difficulty);

        //    //LevelGenerator.Generate(levelData.levels[currentLevelIndex]);
        //    EditorUtility.SetDirty(levelData);
        //    AssetDatabase.SaveAssets();
        //}
        if (GUILayout.Button("Regenerate Selected Level"))
        {
            LevelConfig old = levelData.levels[currentLevelIndex];

            LevelConfig generated = LevelGenerator.Generate(
                old.gridRows,
                old.gridCols,
                old.colorCount,
                old.difficulty);

            if (generated != null)
            {
                levelData.levels[currentLevelIndex] = generated;

                EditorUtility.SetDirty(levelData);
                AssetDatabase.SaveAssets();
                Debug.Log("Level Regenerated & Saved.");
            }
            else
            {
                Debug.LogError("Generation Failed.");
            }
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Solve Level"))
        {
            var result = LevelSolver.SolveLevel(levelData.levels[currentLevelIndex]);
            EditorUtility.DisplayDialog("Solver Result", result.message, "OK");
        }
        if (GUILayout.Button("Add New Level"))
        {
            levelData.levels.Add(new LevelConfig());
            currentLevelIndex = levelData.levels.Count - 1;

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
        }

        if (levelData.levels.Count > 0 &&
            GUILayout.Button("Delete Current Level"))
        {
            levelData.levels.RemoveAt(currentLevelIndex);
            currentLevelIndex = Mathf.Max(0, currentLevelIndex - 1);

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
        }
    }

    #endregion

    #region Helpers
    void DrawSelectionBorder(Rect rect)
    {
        float thickness = 3f;

        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), Color.black); // top
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Color.black); // bottom
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), Color.black); // left
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Color.black); // right
    }

    void ResizeList<T>(ref List<T> list, int targetSize, T defaultValue)
    {
        if (list == null)
            list = new List<T>();

        while (list.Count < targetSize)
            list.Add(defaultValue);

        if (list.Count > targetSize)
            list.RemoveRange(targetSize, list.Count - targetSize);
    }

    string GetArrowSymbol(Direction dir)
    {
        switch (dir)
        {
            case Direction.Right: return "→";
            case Direction.Left: return "←";
            case Direction.Forward: return "↑";
            case Direction.Backward: return "↓";
            default: return "";
        }
    }

    Color GetUnityColor(PassengerColor color)
    {
        switch (color)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Yellow: return Color.yellow;
                case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
                case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
                case PassengerColor.Pink: return new Color(1f, 0.75f, 0.8f);
                case PassengerColor.Cyan: return Color.cyan; 
                case PassengerColor.Gray: return Color.gray; 
                case PassengerColor.White:

            default: return Color.white;
        }
    }

    #endregion
}
