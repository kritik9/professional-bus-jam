using UnityEngine;
using UnityEditor;
using ElevatorGame.Data;
using System.Collections.Generic;
namespace ElevatorGame.Core
{
    public class LevelDataGenerator
    {
        [MenuItem("ElevatorGame/Generate Test Levels")]
        public static void Generate()
        {
            string path = "Assets/ElevatorGame/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/ElevatorGame/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/ElevatorGame", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/ElevatorGame/Resources", "Levels");
            }

            // LEVEL 1: Waiting Area Test
            LevelData level1 = ScriptableObject.CreateInstance<LevelData>();
            level1.rows = 4;
            level1.columns = 3;
            level1.totalBufferSlots = 5;
            level1.initiallyLockedSlots = 0;

            // Sequence: Red, Blue, Yellow (These 3 spawn immediately). Green spawns only after one leaves.
            level1.elevatorSequence = new List<ElevatorData>
        {
            new ElevatorData { requiredColor = CharacterColor.Red, requiredCapacity = 3, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Blue, requiredCapacity = 3, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Yellow, requiredCapacity = 3, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Green, requiredCapacity = 3, isLockedInitially = false }
        };

            level1.initialCharacters = new List<CharacterData>();

            // Row 3 (Back): Yellow
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Yellow, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 3) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Yellow, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 3) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Yellow, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 3) });

            // Row 2: Blue
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Blue, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 2) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Blue, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 2) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Blue, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 2) });

            // Row 1: Red
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Red, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 1) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Red, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 1) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Red, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 1) });

            // Row 0 (Front): Green (There is no Green elevator spawned yet! So tapping these moves them to waiting area!)
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Green, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 0) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Green, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 0) });
            level1.initialCharacters.Add(new CharacterData { color = CharacterColor.Green, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 0) });

            AssetDatabase.CreateAsset(level1, path + "/TestLevel_WaitingArea.asset");

            // LEVEL 2: Complex Mixed Test
            LevelData level2 = ScriptableObject.CreateInstance<LevelData>();
            level2.rows = 4;
            level2.columns = 4;
            level2.totalBufferSlots = 5;
            level2.initiallyLockedSlots = 0;

            level2.elevatorSequence = new List<ElevatorData>
        {
            new ElevatorData { requiredColor = CharacterColor.Pink, requiredCapacity = 4, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Purple, requiredCapacity = 4, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Cyan, requiredCapacity = 4, isLockedInitially = false },
            new ElevatorData { requiredColor = CharacterColor.Orange, requiredCapacity = 4, isLockedInitially = false }
        };

            level2.initialCharacters = new List<CharacterData>();

            // Row 3: Orange
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Orange, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 3) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Orange, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 3) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Orange, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 3) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Orange, moveDirection = MoveDirection.Up, position = new Vector2Int(3, 3) });

            // Row 2: Cyan
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Cyan, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 2) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Cyan, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 2) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Cyan, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 2) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Cyan, moveDirection = MoveDirection.Up, position = new Vector2Int(3, 2) });

            // Row 1: Purple
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Purple, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 1) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Purple, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 1) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Purple, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 1) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Purple, moveDirection = MoveDirection.Up, position = new Vector2Int(3, 1) });

            // Row 0: Pink (Notice Orange is not spawned initially since Pink, Purple, Cyan fill the 3 active spots)
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Pink, moveDirection = MoveDirection.Up, position = new Vector2Int(0, 0) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Pink, moveDirection = MoveDirection.Up, position = new Vector2Int(1, 0) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Pink, moveDirection = MoveDirection.Up, position = new Vector2Int(2, 0) });
            level2.initialCharacters.Add(new CharacterData { color = CharacterColor.Pink, moveDirection = MoveDirection.Up, position = new Vector2Int(3, 0) });

            AssetDatabase.CreateAsset(level2, path + "/TestLevel_Complex.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Generated Test Levels inside Assets/ElevatorGame/Resources/Levels!");
        }
    }
}
