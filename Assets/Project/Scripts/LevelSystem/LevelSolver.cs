using System.Collections.Generic; 
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine; 
using static GameEnums;

public class GameStateJam
{
    public int rows = 9;
    public int cols = 9;

    public PassengerData?[,] grid;  

    public List<int> waiting = new List<int>();
    public int maxWaiting = 6;

    public BusData currentBus;
    public Queue<int> busQueue = new Queue<int>();  

    public int busCapacity = 3;

    public int moveCount = 0;

    public bool TryApplyMove(int x, int y)
    {
        if (!CanMove(x, y))
            return false;

        PassengerData passenger = grid[x, y].Value;
         
        grid[x, y] = null;

        moveCount++;
         
        if (passenger.color == currentBus.color)
        {
            currentBus.currentCount++;
             
            if (currentBus.currentCount >= busCapacity)
            {
                LoadNextBus();
            }
        }
        else
        { 
            if (waiting.Count >= maxWaiting)
            {
                return false;  
            }

            waiting.Add(passenger.color);
        }

        return true;

    }
    private void LoadNextBus()
    {
        if (busQueue.Count == 0)
        {
            currentBus = null;
            return;
        }

        int nextColor = busQueue.Dequeue();

        currentBus = new BusData
        {
            color = nextColor,
            currentCount = 0
        };
         
        for (int i = waiting.Count - 1; i >= 0; i--)
        {
            if (waiting[i] == currentBus.color)
            {
                currentBus.currentCount++;
                waiting.RemoveAt(i);

                if (currentBus.currentCount >= busCapacity)
                {
                    LoadNextBus();
                    break;
                }
            }
        }
         
    }
    public GameStateJam Clone()
    {
        GameStateJam clone = new GameStateJam();

        clone.rows = rows;
        clone.cols = cols;
         
        clone.grid = new PassengerData?[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                clone.grid[i, j] = grid[i, j];
         
        clone.waiting = new List<int>(waiting);

        clone.maxWaiting = maxWaiting;
        clone.busCapacity = busCapacity;
        clone.moveCount = moveCount;
         
        if (currentBus != null)
        {
            clone.currentBus = new BusData
            {
                color = currentBus.color,
                currentCount = currentBus.currentCount
            };
        }
         
        clone.busQueue = new Queue<int>(busQueue);

        return clone;
    }
    public string GetStateKey()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
         
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (grid[i, j].HasValue)
                {
                    var p = grid[i, j].Value;
                    sb.Append(p.color);
                    sb.Append(",");
                    sb.Append(p.direction.x);
                    sb.Append(",");
                    sb.Append(p.direction.y);
                }
                else
                {
                    sb.Append("X");
                }

                sb.Append("|");
            }
        }

        sb.Append("#");
         
        for (int i = 0; i < waiting.Count; i++)
        {
            sb.Append(waiting[i]);
            sb.Append(",");
        } 
        sb.Append("#");
         
        if (currentBus != null)
        {
            sb.Append(currentBus.color);
            sb.Append(",");
            sb.Append(currentBus.currentCount);
        }

        sb.Append("#"); 
        foreach (var color in busQueue)
        {
            sb.Append(color);
            sb.Append(",");
        }

        return sb.ToString();
    }
    public bool HasAnyValidMove()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (!grid[i, j].HasValue)
                    continue;

                if (CanMove(i, j))
                    return true;
            }
        }

        return false;
    }

    public bool IsWin()
    { 
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (grid[i, j].HasValue)
                    return false;
         
        if (waiting.Count > 0)
            return false;
         
        if (currentBus != null && currentBus.currentCount > 0)
            return false;

        return true;
    }

    public bool CanMove(int x, int y)
    { 
        if (x < 0 || y < 0 || x >= rows || y >= cols)
            return false;
         
        if (!grid[x, y].HasValue)
            return false;

        PassengerData passenger = grid[x, y].Value;
        Vector2Int dir = passenger.direction;

        int nx = x + dir.x;
        int ny = y + dir.y;
         
        while (nx >= 0 && ny >= 0 && nx < rows && ny < cols)
        {
            if (grid[nx, ny].HasValue)
                return false;   

            nx += dir.x;
            ny += dir.y;
        }
         
        return true;
    }
    public bool IsColorImpossible()
    {
        if (currentBus == null)
            return false;

        int needed = busCapacity - currentBus.currentCount;
        if (needed <= 0)
            return false;

        int available = 0;
         
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (grid[i, j].HasValue && grid[i, j].Value.color == currentBus.color)
                    available++;
         
        foreach (var c in waiting)
            if (c == currentBus.color)
                available++;

        return available < needed;
    }

}
public struct PassengerData
{
    public int color;
    public Vector2Int direction;
}
public class BusData
{
    public int color;
    public int currentCount;
}
 
public class DFSSolver
{
    private HashSet<string> visited = new HashSet<string>();
    private List<Move> currentPath = new List<Move>();
    private List<Move> finalPath = new List<Move>();

    //public bool Solve(GameStateJam state)
    //{ 

    //    return DFS(state);
    //}
    public (bool solved, List<Move> path) Solve(GameStateJam state)
    {
        bool result = DFS(state);

        return (result, finalPath);
    }

    private bool DFS(GameStateJam state)
    {
        if (state.IsWin())
        {
            finalPath = new List<Move>(currentPath);
            return true;
        }
         
        if (state.IsColorImpossible())
            return false;

        if (!state.HasAnyValidMove())
            return false;
        string key = state.GetStateKey();
         
        if (visited.Contains(key))
            return false;

        visited.Add(key);
         
        for (int i = 0; i < state.rows; i++)
        {
            for (int j = 0; j < state.cols; j++)
            {
                if (!state.grid[i, j].HasValue)
                    continue;

                if (!state.CanMove(i, j))
                    continue;

                GameStateJam newState = state.Clone();
                if (!newState.TryApplyMove(i, j))
                    continue;
                 
                currentPath.Add(new Move(i, j));

                if (DFS(newState))
                    return true; 
                currentPath.RemoveAt(currentPath.Count - 1); 
            }
        }

        return false;
    }

}


public static class LevelSolver
{
    public struct SolveResult
    {
        public bool isSolvable;
        public string message;
    }

    public static SolveResult SolveLevel(LevelConfig config)
    {
        GameStateJam state = ConvertToGameStateJam(config);

        DFSSolver solver = new DFSSolver();

        var Result = solver.Solve(state);

        return new SolveResult
        {
            isSolvable = Result.solved,
            message = Result.solved ? "Level is Solvable ✅\nPath:\n" + string.Join(" -> ", Result.path) : "No solution found ❌"
        };
    }
    private static GameStateJam ConvertToGameStateJam(LevelConfig config)
    {
        GameStateJam state = new GameStateJam();

        state.rows = config.gridRows;
        state.cols = config.gridCols;

        state.busCapacity = config.carCapacity;
        state.maxWaiting = 5;

        state.grid = new PassengerData?[state.rows, state.cols];
         
        for (int row = 0; row < state.rows; row++)
        {
            for (int col = 0; col < state.cols; col++)
            {
                int index = row * state.cols + col;

                PassengerColor pColor = config.passengerColors[index];
                Direction dir = config.passengerDirections[index];

                PassengerData data = new PassengerData
                {
                    color = (int)pColor,
                    direction = ConvertDirection(dir)
                };

                state.grid[row, col] = data;
            }
        } 
        foreach (var busColor in config.busOrder)
        {
            state.busQueue.Enqueue((int)busColor);
        } 
        if (state.busQueue.Count > 0)
        {
            int firstColor = state.busQueue.Dequeue();

            state.currentBus = new BusData
            {
                color = firstColor,
                currentCount = 0
            };
        }

        return state;
    }
    private static Vector2Int ConvertDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Right: return new Vector2Int(0, 1);
            case Direction.Left: return new Vector2Int(0, -1);
            case Direction.Forward: return new Vector2Int(-1, 0);    
            case Direction.Backward: return new Vector2Int(1, 0);   
            default: return Vector2Int.zero;
        }
    }
}
public struct Move
{
    public int row;
    public int col;

    public Move(int r, int c)               
    {
        row = r;
        col = c;
    }

    public override string ToString()
    {
        return $"({row},{col})";
    }
}