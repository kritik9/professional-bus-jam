using UnityEditor;
using UnityEngine;
using ElevatorGame.Core;
using ElevatorGame.Data;
using System.Collections.Generic;
using System.Linq;

namespace ElevatorGame.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private int rows = 5;
        private int columns = 6;
        private int bufferSlots = 6;
        
        private CharacterData[,] grid;
        private List<ElevatorData> elevatorQueue = new List<ElevatorData>();
        
        private CharacterColor selectedColor = CharacterColor.Red;
        private MoveDirection selectedDirection = MoveDirection.Up;
        private bool eraseMode = false;
        
        private LevelData currentLevelData;
        
        private string validationMessage = "";
        private MessageType validationMessageType = MessageType.Info;

        Vector2 scrollPos;

        [MenuItem("Tools/Elevator Game/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            if (grid == null || grid.GetLength(0) != columns || grid.GetLength(1) != rows)
            {
                grid = new CharacterData[columns, rows];
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("Level Settings", EditorStyles.boldLabel);
            
            currentLevelData = (LevelData)EditorGUILayout.ObjectField("Load Level", currentLevelData, typeof(LevelData), false);
            if (GUILayout.Button("Load Data from Selected Level"))
            {
                LoadFromLevelData();
            }
            
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            rows = EditorGUILayout.IntField("Rows (Y)", rows);
            columns = EditorGUILayout.IntField("Columns (X)", columns);
            if (EditorGUI.EndChangeCheck())
            {
                rows = Mathf.Clamp(rows, 1, 20);
                columns = Mathf.Clamp(columns, 1, 20);
                CharacterData[,] newGrid = new CharacterData[columns, rows];
                
                // copy over old data if possible
                if (grid != null)
                {
                    for (int x = 0; x < Mathf.Min(columns, grid.GetLength(0)); x++)
                    {
                        for (int y = 0; y < Mathf.Min(rows, grid.GetLength(1)); y++)
                        {
                            newGrid[x, y] = grid[x, y];
                        }
                    }
                }
                grid = newGrid;
            }
            
            bufferSlots = EditorGUILayout.IntField("Buffer Slots", bufferSlots);
            
            EditorGUILayout.Space();
            GUILayout.Label("Painting Tools", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            selectedColor = (CharacterColor)EditorGUILayout.EnumPopup("Color", selectedColor);
            selectedDirection = (MoveDirection)EditorGUILayout.EnumPopup("Direction", selectedDirection);
            eraseMode = GUILayout.Toggle(eraseMode, "Erase Mode", "Button");
            if (GUILayout.Button("Fill Grid")) FillGrid();
            if (GUILayout.Button("Clear Grid")) ClearGrid();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            GUILayout.Label("Grid Layout", EditorStyles.boldLabel);
            
            DrawGrid();
            
            EditorGUILayout.Space();
            GUILayout.Label("Elevators Queue", EditorStyles.boldLabel);
            
            DrawElevatorQueue();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Check Solvability"))
            {
                CheckSolvability();
            }
            
            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, validationMessageType);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Save to LevelData Asset", GUILayout.Height(40)))
            {
                SaveToLevelData();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void FillGrid()
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (grid[x,y] == null)
                    {
                        grid[x,y] = new CharacterData { position = new Vector2Int(x, y), color = selectedColor, moveDirection = selectedDirection };
                    }
                }
            }
        }
        
        private void ClearGrid()
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    grid[x,y] = null;
                }
            }
        }

        private void DrawGrid()
        {
            if (grid == null) InitializeGrid();
            
            // Draw grid upside down so Y=0 is at the bottom visually
            for (int y = rows - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int x = 0; x < columns; x++)
                {
                    CharacterData cell = grid[x, y];
                    string btnText = cell != null ? GetCharIcon(cell) : "·";
                    
                    UnityEngine.Color prevColor = GUI.backgroundColor;
                    if (cell != null)
                    {
                        GUI.backgroundColor = GetGUIColor(cell.color);
                    }
                    else
                    {
                        GUI.backgroundColor = UnityEngine.Color.gray;
                    }
                    
                    if (GUILayout.Button(btnText, GUILayout.Width(40), GUILayout.Height(40)))
                    {
                        if (eraseMode)
                        {
                            grid[x, y] = null;
                        }
                        else
                        {
                            grid[x, y] = new CharacterData 
                            { 
                                position = new Vector2Int(x, y), 
                                color = selectedColor, 
                                moveDirection = selectedDirection 
                            };
                        }
                    }
                    
                    GUI.backgroundColor = prevColor;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private string GetCharIcon(CharacterData data)
        {
            return data.moveDirection switch
            {
                MoveDirection.Up => "▲",
                MoveDirection.Down => "▼",
                MoveDirection.Left => "◀",
                MoveDirection.Right => "▶",
                _ => "●"
            };
        }

        private UnityEngine.Color GetGUIColor(CharacterColor c)
        {
            return c switch
            {
                CharacterColor.Red => UnityEngine.Color.red,
                CharacterColor.Blue => new UnityEngine.Color(0.2f, 0.5f, 1f),
                CharacterColor.Green => UnityEngine.Color.green,
                CharacterColor.Yellow => UnityEngine.Color.yellow,
                CharacterColor.Purple => new UnityEngine.Color(0.6f, 0.2f, 0.8f),
                CharacterColor.Orange => new UnityEngine.Color(1f, 0.6f, 0f),
                CharacterColor.Pink => new UnityEngine.Color(1f, 0.4f, 0.7f),
                CharacterColor.Cyan => UnityEngine.Color.cyan,
                CharacterColor.Gray => UnityEngine.Color.gray,
                CharacterColor.White => UnityEngine.Color.white,
                _ => UnityEngine.Color.white
            };
        }

        private void DrawElevatorQueue()
        {
            for (int i = 0; i < elevatorQueue.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Elevator {i + 1}", GUILayout.Width(80));
                elevatorQueue[i].requiredColor = (CharacterColor)EditorGUILayout.EnumPopup(elevatorQueue[i].requiredColor, GUILayout.Width(100));
                elevatorQueue[i].requiredCapacity = EditorGUILayout.IntField(elevatorQueue[i].requiredCapacity, GUILayout.Width(50));
                
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    elevatorQueue.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Elevator"))
            {
                elevatorQueue.Add(new ElevatorData { requiredColor = CharacterColor.Red, requiredCapacity = 5 });
            }
        }

        private void CheckSolvability()
        {
            // 1. Check mathematical limits (do capacities match character counts?)
            Dictionary<CharacterColor, int> charCounts = new Dictionary<CharacterColor, int>();
            foreach (var c in grid)
            {
                if (c != null)
                {
                    if (!charCounts.ContainsKey(c.color)) charCounts[c.color] = 0;
                    charCounts[c.color]++;
                }
            }
            
            Dictionary<CharacterColor, int> elevatorCapacities = new Dictionary<CharacterColor, int>();
            foreach (var e in elevatorQueue)
            {
                if (!elevatorCapacities.ContainsKey(e.requiredColor)) elevatorCapacities[e.requiredColor] = 0;
                elevatorCapacities[e.requiredColor] += e.requiredCapacity;
            }
            
            List<string> errors = new List<string>();
            foreach (var kvp in charCounts)
            {
                int needed = elevatorCapacities.ContainsKey(kvp.Key) ? elevatorCapacities[kvp.Key] : 0;
                if (needed != kvp.Value)
                {
                    errors.Add($"{kvp.Key}: {kvp.Value} on board, but elevators need {needed}.");
                }
            }
            foreach (var kvp in elevatorCapacities)
            {
                int onBoard = charCounts.ContainsKey(kvp.Key) ? charCounts[kvp.Key] : 0;
                if (onBoard == 0)
                {
                    errors.Add($"Elevator needs {kvp.Key}, but 0 on board.");
                }
            }
            
            if (errors.Count > 0)
            {
                validationMessage = "UNSOLVABLE! Math Mismatch:\n" + string.Join("\n", errors);
                validationMessageType = MessageType.Error;
                return;
            }
            
            // Note: A full game simulation could be added here for paths, but mathematical verification ensures basic solvability.
            validationMessage = "Mathematically Solvable! All color counts perfectly match the elevator capacities.";
            validationMessageType = MessageType.Info;
        }

        private void LoadFromLevelData()
        {
            if (currentLevelData == null) return;
            
            rows = currentLevelData.rows;
            columns = currentLevelData.columns;
            bufferSlots = currentLevelData.totalBufferSlots;
            
            InitializeGrid();
            ClearGrid();
            
            if (currentLevelData.initialCharacters != null)
            {
                foreach (var c in currentLevelData.initialCharacters)
                {
                    if (c.position.x >= 0 && c.position.x < columns && c.position.y >= 0 && c.position.y < rows)
                    {
                        grid[c.position.x, c.position.y] = new CharacterData 
                        { 
                            position = c.position, 
                            color = c.color, 
                            moveDirection = c.moveDirection 
                        };
                    }
                }
            }
            
            elevatorQueue.Clear();
            if (currentLevelData.elevatorSequence != null)
            {
                foreach (var e in currentLevelData.elevatorSequence)
                {
                    elevatorQueue.Add(new ElevatorData { requiredColor = e.requiredColor, requiredCapacity = e.requiredCapacity, isLockedInitially = e.isLockedInitially });
                }
            }
            
            validationMessage = "Loaded successfully!";
            validationMessageType = MessageType.Info;
        }

        private void SaveToLevelData()
        {
            if (currentLevelData == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Level Data", "NewLevelData", "asset", "Save Level Data");
                if (string.IsNullOrEmpty(path)) return;
                
                currentLevelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(currentLevelData, path);
            }
            
            currentLevelData.rows = rows;
            currentLevelData.columns = columns;
            currentLevelData.totalBufferSlots = bufferSlots;
            
            currentLevelData.initialCharacters = new List<CharacterData>();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (grid[x, y] != null)
                    {
                        currentLevelData.initialCharacters.Add(new CharacterData 
                        {
                            position = new Vector2Int(x, y),
                            color = grid[x, y].color,
                            moveDirection = grid[x, y].moveDirection
                        });
                    }
                }
            }
            
            currentLevelData.elevatorSequence = new List<ElevatorData>();
            foreach (var e in elevatorQueue)
            {
                currentLevelData.elevatorSequence.Add(new ElevatorData { requiredColor = e.requiredColor, requiredCapacity = e.requiredCapacity, isLockedInitially = e.isLockedInitially });
            }
            
            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            
            validationMessage = "Saved successfully to " + currentLevelData.name;
            validationMessageType = MessageType.Info;
        }
    }
}
