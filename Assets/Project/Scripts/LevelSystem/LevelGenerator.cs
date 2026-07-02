using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameEnums;

public static class LevelGenerator
{
    private const int BUS_CAPACITY = 3;
    private const int WAITING_SLOTS = 6;
    private const int SAFE_LIMIT = 5; // 6th slot = fail
    private const int MAX_RETRIES = 150;

    private static readonly Direction[] ALL_DIRS =
    {
        Direction.Right,
        Direction.Left,
        Direction.Forward,
        Direction.Backward
    };

    // ============================================================
    // PUBLIC
    // ============================================================

    public static LevelConfig Generate(
        int rows,
        int cols,
        int colorCount,
        Difficulty difficulty)
    {
        colorCount = Mathf.Clamp(colorCount, 2, 8);

        for (int i = 0; i < MAX_RETRIES; i++)
        {
            var cfg = TryGenerate(rows, cols, colorCount, difficulty);
            if (cfg != null)
                return cfg;
        }

        Debug.LogError("Generation failed.");
        return null;
    }

    // ============================================================
    // MAIN PIPELINE
    // ============================================================

    private static LevelConfig TryGenerate(
        int rows,
        int cols,
        int colorCount,
        Difficulty difficulty)
    {
        int total = rows * cols;
        if (total < BUS_CAPACITY * colorCount)
            return null;

        int[] depth = BuildDepthMap(rows, cols);

        int totalBuses = total / BUS_CAPACITY;
        colorCount = Mathf.Min(colorCount, totalBuses);

        List<PassengerColor> colors = GetShuffledColors(colorCount);
        int[] passCounts = ExactPassengerCounts(totalBuses, colorCount);

        if (!AssignColors(rows, cols, depth, colors, passCounts,
            out PassengerColor[] grid))
            return null;

        Direction[] dirs = AssignAcyclicDirections(rows, cols, depth);
        if (dirs == null)
            return null;

        List<PassengerColor> busOrder =
            BuildBusOrder(colors, passCounts, difficulty);

        // 🔥 FULL BFS VALIDATION
        if (!IsSolvableBFS(rows, cols,
            grid.ToList(), dirs.ToList(), busOrder))
            return null;

        return new LevelConfig
        {
            gridRows = rows,
            gridCols = cols,
            carCapacity = BUS_CAPACITY,
            colorCount = colorCount,
            busOrder = busOrder,
            passengerColors = grid.ToList(),
            passengerDirections = dirs.ToList(),
            difficulty = difficulty
        };
    }

    // ============================================================
    // DEPTH (DAG BASE)
    // ============================================================

    private static int[] BuildDepthMap(int rows, int cols)
    {
        int[] depth = new int[rows * cols];

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < cols; z++)
            {
                depth[x * cols + z] =
                    Mathf.Min(x, rows - 1 - x, z, cols - 1 - z);
            }
        }

        return depth;
    }

    // ============================================================
    // COLOR DISTRIBUTION
    // ============================================================

    private static int[] ExactPassengerCounts(int totalBuses, int colorCount)
    {
        int baseCount = totalBuses / colorCount;
        int extra = totalBuses % colorCount;

        int[] result = new int[colorCount];

        for (int i = 0; i < colorCount; i++)
            result[i] = (baseCount + (i < extra ? 1 : 0)) * BUS_CAPACITY;

        return result;
    }

    private static bool AssignColors(
        int rows,
        int cols,
        int[] depth,
        List<PassengerColor> colors,
        int[] passCounts,
        out PassengerColor[] grid)
    {
        int total = rows * cols;
        grid = new PassengerColor[total];

        int maxDepth = depth.Max();
        int colorCount = colors.Count;

        int bandSize = (maxDepth + 1) / colorCount;

        int[] allocated = new int[colorCount];

        for (int i = 0; i < total; i++)
        {
            int band = Mathf.Min(depth[i] / Mathf.Max(1, bandSize),
                                 colorCount - 1);

            grid[i] = colors[band];
            allocated[band]++;
        }

        // enforce exact counts
        for (int c = 0; c < colorCount; c++)
        {
            while (allocated[c] > passCounts[c])
            {
                int target =
                    System.Array.FindIndex(allocated,
                        a => a < passCounts[
                            System.Array.IndexOf(allocated, a)]);

                if (target < 0)
                    return false;

                int idx =
                    System.Array.FindIndex(grid,
                        g => g == colors[c]);

                grid[idx] = colors[target];
                allocated[c]--;
                allocated[target]++;
            }
        }

        return true;
    }

    // ============================================================
    // DEADLOCK IMPOSSIBLE DIRECTIONS
    // ============================================================

    private static Direction[] AssignAcyclicDirections(
        int rows,
        int cols,
        int[] depth)
    {
        Direction[] dirs = new Direction[rows * cols];

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < cols; z++)
            {
                int idx = x * cols + z;
                int d0 = depth[idx];

                List<Direction> valid = new List<Direction>();

                foreach (var dir in ALL_DIRS)
                {
                    int nx = x, nz = z;
                    Step(dir, ref nx, ref nz);

                    if (nx < 0 || nx >= rows ||
                        nz < 0 || nz >= cols)
                    {
                        if (d0 == 0)
                            valid.Add(dir);
                        continue;
                    }

                    if (depth[nx * cols + nz] < d0)
                        valid.Add(dir);
                }

                if (valid.Count == 0)
                    return null;

                dirs[idx] =
                    valid[Random.Range(0, valid.Count)];
            }
        }

        return dirs;
    }

    // ============================================================
    // 🔥 FULL BFS SOLVER (WAITING SAFE)
    // ============================================================

    private class State
    {
        public ulong clearedMask;
        public int waiting;
        public int busIndex;
        public int boarded;
    }

    private static bool IsSolvableBFS(
        int rows,
        int cols,
        List<PassengerColor> colors,
        List<Direction> dirs,
        List<PassengerColor> busOrder)
    {
        int total = rows * cols;

        Queue<State> queue = new Queue<State>();
        HashSet<ulong> visited = new HashSet<ulong>();

        State start = new State
        {
            clearedMask = 0,
            waiting = 0,
            busIndex = 0,
            boarded = 0
        };

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var s = queue.Dequeue();

            if (s.busIndex >= busOrder.Count)
                return true;

            if (!visited.Add(s.clearedMask ^
                ((ulong)s.waiting << 48) ^
                ((ulong)s.busIndex << 56)))
                continue;

            var movable =
                ComputeMovable(rows, cols, s.clearedMask, dirs);

            foreach (int idx in movable)
            {
                bool match =
                    colors[idx] == busOrder[s.busIndex];

                State next = new State
                {
                    clearedMask =
                        s.clearedMask | (1UL << idx),
                    waiting = s.waiting,
                    busIndex = s.busIndex,
                    boarded = s.boarded
                };

                if (match)
                {
                    next.boarded++;
                }
                else
                {
                    next.waiting++;
                    if (next.waiting > SAFE_LIMIT)
                        continue;
                }

                if (next.boarded >= BUS_CAPACITY)
                {
                    next.busIndex++;
                    next.boarded = 0;

                    if (next.busIndex < busOrder.Count)
                    {
                        int drain =
                            Mathf.Min(next.waiting, BUS_CAPACITY);
                        next.waiting -= drain;
                    }
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static List<int> ComputeMovable(
        int rows,
        int cols,
        ulong mask,
        List<Direction> dirs)
    {
        List<int> result = new List<int>();

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < cols; z++)
            {
                int idx = x * cols + z;
                if ((mask & (1UL << idx)) != 0)
                    continue;

                if (IsPathClear(x, z, dirs[idx],
                                rows, cols, mask))
                    result.Add(idx);
            }
        }

        return result;
    }

    private static bool IsPathClear(
        int x,
        int z,
        Direction dir,
        int rows,
        int cols,
        ulong mask)
    {
        int cx = x, cz = z;

        while (true)
        {
            Step(dir, ref cx, ref cz);

            if (cx < 0 || cx >= rows ||
                cz < 0 || cz >= cols)
                return true;

            if ((mask & (1UL << (cx * cols + cz))) == 0)
                return false;
        }
    }

    // ============================================================

    private static void Step(Direction d, ref int x, ref int z)
    {
        switch (d)
        {
            case Direction.Right: x++; break;
            case Direction.Left: x--; break;
            case Direction.Forward: z++; break;
            case Direction.Backward: z--; break;
        }
    }

    private static List<PassengerColor> GetShuffledColors(int count)
    {
        var all = new List<PassengerColor>
        {
            PassengerColor.Red,
            PassengerColor.Blue,
            PassengerColor.Green,
            PassengerColor.Yellow,
            PassengerColor.Orange,
            PassengerColor.Purple,
            PassengerColor.Pink,
            PassengerColor.Cyan
        };

        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        return all.Take(count).ToList();
    }

    private static List<PassengerColor> BuildBusOrder(
        List<PassengerColor> colors,
        int[] passCounts,
        Difficulty difficulty)
    {
        List<PassengerColor> result = new List<PassengerColor>();

        for (int i = 0; i < colors.Count; i++)
        {
            int buses = passCounts[i] / BUS_CAPACITY;
            for (int b = 0; b < buses; b++)
                result.Add(colors[i]);
        }

        if (difficulty != Difficulty.Easy)
        {
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
        }

        return result;
    }
}


//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using static GameEnums;
//using static UnityEngine.Rendering.DebugUI.Table;


//public static class LevelGenerator
//{ 
//    private const int BUS_CAPACITY = 3;
//    private const int WAITING_SLOTS = 6;
//    private const int WAITING_BUFFER = 1;  
//    private const int MAX_RETRIES = 700; 



//    public static LevelConfig Generate(int gridRows, int gridCols,
//                                       int colorCount, Difficulty difficulty)
//    {
//        colorCount = Mathf.Clamp(colorCount, 2, 10);

//        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
//        {
//            LevelConfig cfg = TryGenerate(gridRows, gridCols, colorCount, difficulty);
//            if (cfg != null)
//            {
//                Debug.Log($"[LevelGenerator] Generated in {attempt + 1} attempt(s). " +
//                          $"Grid={gridRows}x{gridCols}, Colors={colorCount}, " +
//                          $"Difficulty={difficulty}, Buses={cfg.busOrder.Count}");
//                LevelConfig so = new LevelConfig();

//                so.gridRows = cfg.gridRows;
//                so.gridCols = cfg.gridCols;
//                so.carCapacity = BUS_CAPACITY;
//                so.colorCount = colorCount;
//                so.busOrder = new List<PassengerColor>(cfg.busOrder);
//                so.passengerColors = cfg.passengerColors;
//                so.passengerDirections = cfg.passengerDirections;
//                so.difficulty = difficulty;

//                return so;
//            }
//        }

//        Debug.LogError("[LevelGenerator] Failed to generate a valid level after " +
//                       $"{MAX_RETRIES} attempts. Grid={gridRows}x{gridCols} Colors={colorCount}");
//        return null;
//    }


//    private static LevelConfig TryGenerate(int rows, int cols,
//                                           int colorCount, Difficulty difficulty)
//    {
//        int totalCells = rows * cols;

//        List<PassengerColor> busOrder = BuildBusOrder(totalCells, colorCount, difficulty);
//        if (busOrder == null) return null;

//        Dictionary<PassengerColor, int> colorCounts = new();
//        foreach (var c in busOrder)
//        {
//            if (!colorCounts.ContainsKey(c)) colorCounts[c] = 0;
//            colorCounts[c] += BUS_CAPACITY;
//        }

//        int[,] layerMap = BuildLayerMap(rows, cols);

//        List<PassengerColor> placedColors =
//            PlaceColors(rows, cols, layerMap, colorCounts, busOrder, difficulty);
//        if (placedColors == null) return null;

//        List<Direction> placedDirs =
//            AssignDirections(rows, cols, layerMap, difficulty);

//        if (!ValidateWaitingSafety(rows, cols, placedColors, placedDirs,
//                                   layerMap, busOrder))
//            return null;

//        LevelConfig cfg = new LevelConfig
//        {
//            gridRows = rows,
//            gridCols = cols,
//            carCapacity = BUS_CAPACITY,
//            colorCount = colorCount,
//            busOrder = busOrder,
//            passengerColors = placedColors,
//            passengerDirections = placedDirs,
//            difficulty = difficulty
//        };

//        return cfg;
//    }

//    private static List<PassengerColor> BuildBusOrder(int totalCells,
//                                                       int colorCount,
//                                                       Difficulty difficulty)
//    {
//        // passengers per color must be multiple of BUS_CAPACITY
//        int passengersPerColor = Mathf.CeilToInt((float)totalCells / colorCount);
//        // round up to nearest multiple of BUS_CAPACITY
//        passengersPerColor = RoundUpToMultiple(passengersPerColor, BUS_CAPACITY);

//        int busesPerColor = passengersPerColor / BUS_CAPACITY;
//        int totalBuses = busesPerColor * colorCount;

//        // pick colors
//        List<PassengerColor> allColors = GetShuffledColors(colorCount);

//        // build flat bus list: [R,R, B,B, G,G ...]
//        List<PassengerColor> flat = new();
//        foreach (var color in allColors)
//            for (int i = 0; i < busesPerColor; i++)
//                flat.Add(color);

//        // interleave based on difficulty
//        List<PassengerColor> ordered = InterleaveColors(flat, allColors,
//                                                         busesPerColor, difficulty);
//        return ordered;
//    }


//    private static List<PassengerColor> InterleaveColors(
//        List<PassengerColor> flat,
//        List<PassengerColor> colors,
//        int busesPerColor,
//        Difficulty difficulty)
//    {
//        if (difficulty == Difficulty.Easy)
//        {
//            // mild: group but rotate starting color each time
//            List<PassengerColor> mild = new(flat);
//            Shuffle(mild);
//            return mild;
//        }

//        // round-robin interleave
//        List<PassengerColor> result = new();
//        List<Queue<PassengerColor>> queues = new();

//        foreach (var c in colors)
//        {
//            var q = new Queue<PassengerColor>();
//            for (int i = 0; i < busesPerColor; i++) q.Enqueue(c);
//            queues.Add(q);
//        }

//        int idx = 0;
//        while (result.Count < flat.Count)
//        {
//            for (int i = 0; i < queues.Count; i++)
//            {
//                int qi = (idx + i) % queues.Count;
//                if (queues[qi].Count > 0)
//                {
//                    result.Add(queues[qi].Dequeue());
//                    break;
//                }
//            }
//            idx++;
//        }

//        // Hard: shuffle the middle 60% to add extra unpredictability
//        if (difficulty == Difficulty.Hard && result.Count > 4)
//        {
//            int start = result.Count / 5;
//            int end = result.Count - result.Count / 5;
//            List<PassengerColor> mid = result.GetRange(start, end - start);
//            Shuffle(mid);
//            for (int i = 0; i < mid.Count; i++)
//                result[start + i] = mid[i];
//        }

//        return result;
//    }

//    // ─── step 2 : layer map ───────────────────────────────────────────────────

//    /// <summary>
//    /// Assign each inner cell a "layer" value.
//    /// Layer 0 = closest to exit edge → can move immediately.
//    /// Layer N = N steps away from exit.
//    ///
//    /// Exit edge = right edge (x == rows-1) by convention,
//    /// matching your BuildOutwardPath Direction.Right logic.
//    /// </summary>
//    private static int[,] BuildLayerMap(int rows, int cols)
//    {
//        int[,] map = new int[rows, cols];

//        for (int x = 0; x < rows; x++)
//            for (int z = 0; z < cols; z++)
//                map[x, z] = (rows - 1) - x; // 0 at right edge, max at left edge

//        return map;
//    }

//    private static int MaxLayer(int rows) => rows - 1;

//    // ─── step 3 : color placement ─────────────────────────────────────────────

//    /// <summary>
//    /// Place colors into cells respecting layer-awareness:
//    ///  • Current bus color passengers go in earlier (lower) layers
//    ///  • Ensures player can always reach matching passengers without
//    ///    overflowing waiting slots
//    /// </summary>
//    private static List<PassengerColor> PlaceColors(
//        int rows, int cols,
//        int[,] layerMap,
//        Dictionary<PassengerColor, int> colorCounts,
//        List<PassengerColor> busOrder,
//        Difficulty difficulty)
//    {
//        int total = rows * cols;
//        PassengerColor[] grid = new PassengerColor[total];
//        bool[] assigned = new bool[total];

//        // Build cell indices sorted by layer (ascending = closer to exit first)
//        List<int> cellsByLayer = Enumerable.Range(0, total)
//            .OrderBy(i => layerMap[i / cols, i % cols])
//            .ToList();

//        // Remaining count per color
//        Dictionary<PassengerColor, int> remaining =
//            new Dictionary<PassengerColor, int>(colorCounts);

//        // How many cells per layer
//        int maxLayer = MaxLayer(rows);
//        int cellsPerLayer = cols; // each layer = one column of cells

//        // ── assign "window" per bus ──────────────────────────────────────────
//        // For each bus in order, its matching passengers should sit in
//        // cells reachable before waiting overflows.

//        // We track which layer range each bus's passengers should occupy.
//        // busWindow[i] = (minLayer, maxLayer) for bus i's color passengers
//        // placed in this "slot"

//        List<PassengerColor> distinctBusSequence = GetDistinctSequence(busOrder);

//        // Layer budget: earlier buses get lower layers
//        // We split total layers evenly among distinct colors in bus order
//        int layersPerBusSlot = Mathf.Max(1,
//            Mathf.FloorToInt((float)(maxLayer + 1) / distinctBusSequence.Count));

//        // colorLayerTarget[color] = preferred max layer for this color's passengers
//        Dictionary<PassengerColor, int> colorLayerTarget = new();
//        for (int i = 0; i < distinctBusSequence.Count; i++)
//        {
//            PassengerColor c = distinctBusSequence[i];
//            if (!colorLayerTarget.ContainsKey(c))
//                colorLayerTarget[c] = Mathf.Min(maxLayer,
//                    (i + 1) * layersPerBusSlot - 1);
//        }

//        // ── place colors cell by cell ────────────────────────────────────────
//        // Sort available colors each step by remaining count desc + layer preference
//        List<int> shuffledCells = new List<int>(cellsByLayer);

//        // Apply difficulty scatter: Hard = more shuffle among nearby layers
//        if (difficulty != Difficulty.Easy)
//            ShuffleWithinLayers(shuffledCells, layerMap, cols, difficulty);

//        foreach (int cellIdx in shuffledCells)
//        {
//            int cx = cellIdx / cols;
//            int cz = cellIdx % cols;
//            int layer = layerMap[cx, cz];

//            // Pick best color for this cell
//            PassengerColor chosen = PickColorForCell(layer, maxLayer,
//                remaining, colorLayerTarget, difficulty);

//            if (chosen == (PassengerColor)(-1))
//            {
//                Debug.LogWarning("[LevelGenerator] Could not pick color for cell.");
//                return null;
//            }

//            grid[cellIdx] = chosen;
//            assigned[cellIdx] = true;
//            remaining[chosen]--;
//            if (remaining[chosen] <= 0) remaining.Remove(chosen);
//        }

//        if (remaining.Values.Any(v => v > 0))
//        {
//            Debug.LogWarning("[LevelGenerator] Color placement incomplete.");
//            return null;
//        }

//        return new List<PassengerColor>(grid);
//    }

//    private static PassengerColor PickColorForCell(
//        int layer, int maxLayer,
//        Dictionary<PassengerColor, int> remaining,
//        Dictionary<PassengerColor, int> colorLayerTarget,
//        Difficulty difficulty)
//    {
//        if (remaining.Count == 0) return (PassengerColor)(-1);

//        // Score each remaining color
//        // Lower score = better fit for this cell
//        PassengerColor best = (PassengerColor)(-1);
//        float bestScore = float.MaxValue;

//        foreach (var kvp in remaining)
//        {
//            PassengerColor c = kvp.Key;
//            int rem = kvp.Value;

//            // How far is this cell from the color's preferred max layer?
//            int target = colorLayerTarget.ContainsKey(c) ? colorLayerTarget[c] : maxLayer;
//            float dist = Mathf.Abs(layer - target);

//            // Add noise based on difficulty to scatter placements
//            float noise = difficulty switch
//            {
//                Difficulty.Easy => Random.Range(0f, 0.5f),
//                Difficulty.Medium => Random.Range(0f, 1.5f),
//                Difficulty.Hard => Random.Range(0f, 3f),
//                _ => 0f
//            };

//            // Prefer colors with more remaining (fill evenly)
//            float score = dist + noise - rem * 0.1f;

//            if (score < bestScore)
//            {
//                bestScore = score;
//                best = c;
//            }
//        }

//        return best;
//    }

//    /// <summary>
//    /// Shuffle cells within the same layer to add intra-layer randomness.
//    /// Hard mode shuffles across 2-layer windows too.
//    /// </summary>
//    private static void ShuffleWithinLayers(List<int> cells, int[,] layerMap,
//                                             int cols, Difficulty difficulty)
//    {
//        int windowSize = difficulty == Difficulty.Hard ? 2 : 1;

//        int i = 0;
//        while (i < cells.Count)
//        {
//            int baseLayer = layerMap[cells[i] / cols, cells[i] % cols];
//            int j = i;

//            while (j < cells.Count &&
//                   layerMap[cells[j] / cols, cells[j] % cols] <= baseLayer + windowSize)
//                j++;

//            // shuffle cells[i..j-1]
//            for (int k = i; k < j - 1; k++)
//            {
//                int rand = Random.Range(k, j);
//                (cells[k], cells[rand]) = (cells[rand], cells[k]);
//            }

//            i = j;
//        }
//    }

//    // ─── step 4 : direction assignment ───────────────────────────────────────

//    /// <summary>
//    /// Assign each cell a direction that:
//    ///  1. Points toward exit (right edge) following the layer chain
//    ///  2. Never creates a head-on deadlock (two passengers blocking each other)
//    ///
//    /// Strategy:
//    ///  • Layer 0 (rightmost col) → Direction.Right  (direct exit)
//    ///  • Other layers            → Direction.Right   (move toward lower layer)
//    ///
//    /// This guarantees no two passengers in the same row face each other
//    /// because ALL horizontal passengers face Right.
//    /// Vertical variation is introduced on higher layers for challenge
//    /// without creating deadlocks (vertical movement is perpendicular).
//    /// </summary>
//    private static List<Direction> AssignDirections(int rows, int cols,
//                                                    int[,] layerMap,
//                                                    Difficulty difficulty)
//    {
//        Direction[] dirs = new Direction[rows * cols];

//        int maxLayer = MaxLayer(rows);

//        for (int x = 0; x < rows; x++)
//        {
//            for (int z = 0; z < cols; z++)
//            {
//                int idx = x * cols + z;
//                int layer = layerMap[x, z];

//                dirs[idx] = ChooseDirection(x, z, layer, maxLayer,
//                                            rows, cols, difficulty);
//            }
//        }

//        return new List<Direction>(dirs);
//    }

//    private static Direction ChooseDirection(int x, int z, int layer, int maxLayer,
//                                              int rows, int cols, Difficulty difficulty)
//    {
//        // Layer 0 = right edge → always Right (direct exit)
//        if (layer == 0)
//            return Direction.Right;

//        // Easy: everyone goes Right (simplest, no deadlock ever)
//        if (difficulty == Difficulty.Easy)
//            return Direction.Right;

//        // Medium & Hard: introduce vertical directions on deeper layers
//        // Rule: vertical directions only on cells NOT in the same row as
//        //       another same-direction passenger directly ahead → safe

//        // We use a zone-based system:
//        //  Top third    → Forward (z increasing)
//        //  Bottom third → Backward (z decreasing)
//        //  Middle       → Right
//        // Then deeper layers (higher layer index) get more vertical

//        float layerRatio = (float)layer / maxLayer;  // 0..1
//        float zRatio = (float)z / Mathf.Max(1, cols - 1); // 0..1

//        // Deeper layers → more vertical chance
//        float verticalChance = difficulty == Difficulty.Hard
//            ? layerRatio * 0.6f
//            : layerRatio * 0.35f;

//        if (Random.value < verticalChance)
//        {
//            // top half of grid → Forward, bottom half → Backward
//            // This ensures vertical passengers never face each other
//            // (they all move same direction in their half)
//            if (zRatio >= 0.5f)
//                return Direction.Forward;
//            else
//                return Direction.Backward;
//        }

//        return Direction.Right;
//    }

//    // ─── step 5 : waiting safety validation ──────────────────────────────────

//    /// <summary>
//    /// Simulate an OPTIMAL play sequence and verify waiting never hits the limit.
//    /// Optimal = always tap a passenger whose color matches current bus first.
//    ///
//    /// This is a lightweight simulation (not full BFS) but sufficient to
//    /// catch obvious overflow scenarios.
//    /// </summary>
//    private static bool ValidateWaitingSafety(
//        int rows, int cols,
//        List<PassengerColor> colors,
//        List<Direction> dirs,
//        int[,] layerMap,
//        List<PassengerColor> busOrder)
//    {
//        // Build movability map: cell is "movable" if its path to border is clear
//        // In our layer system, layer-0 cells are always movable initially.
//        // A cell becomes movable when all cells in front of it (same row, lower layer)
//        // are cleared.

//        bool[] cleared = new bool[rows * cols];
//        int waiting = 0;
//        int busIdx = 0;
//        int boardedThisBus = 0;

//        // Track which cells are currently movable
//        HashSet<int> movable = GetInitialMovableCells(rows, cols, layerMap, dirs, colors);

//        int safeLimit = WAITING_SLOTS - WAITING_BUFFER;
//        int maxIterations = rows * cols * 4; // prevent infinite loop
//        int iterations = 0;

//        while (busIdx < busOrder.Count && iterations++ < maxIterations)
//        {
//            PassengerColor busColor = busOrder[busIdx];

//            // Try to find a movable passenger matching bus color
//            int matchIdx = FindMovableMatch(movable, colors, cleared, busColor);

//            if (matchIdx >= 0)
//            {
//                // Board directly
//                cleared[matchIdx] = true;
//                movable.Remove(matchIdx);
//                boardedThisBus++;
//                UpdateMovable(matchIdx, rows, cols, layerMap, dirs,
//                              cleared, movable, colors);

//                // Also drain waiting if any match
//                // (simulate AutoBoardWaitingPassengers)
//                // waiting passengers of matching color board for free
//                // we just decrement waiting count for matching ones
//                // (simplified: assume up to BUS_CAPACITY waiting can board)
//            }
//            else
//            {
//                // No matching movable — tap any movable → goes to waiting
//                int anyIdx = FindAnyMovable(movable, cleared);
//                if (anyIdx < 0)
//                {
//                    // No movable cells at all — deadlock detected
//                    Debug.LogWarning("[LevelGenerator] Validation: no movable cells.");
//                    return false;
//                }

//                cleared[anyIdx] = true;
//                movable.Remove(anyIdx);
//                waiting++;
//                UpdateMovable(anyIdx, rows, cols, layerMap, dirs,
//                              cleared, movable, colors);

//                if (waiting >= safeLimit)
//                {
//                    Debug.LogWarning($"[LevelGenerator] Validation failed: " +
//                                     $"waiting={waiting} >= safeLimit={safeLimit}");
//                    return false;
//                }
//            }

//            // Bus full?
//            if (boardedThisBus >= BUS_CAPACITY)
//            {
//                busIdx++;
//                boardedThisBus = 0;

//                // New bus: waiting passengers of matching color auto-board
//                if (busIdx < busOrder.Count)
//                {
//                    PassengerColor nextColor = busOrder[busIdx];
//                    // drain waiting of nextColor (up to capacity)
//                    int drain = Mathf.Min(waiting, BUS_CAPACITY);
//                    waiting = Mathf.Max(0, waiting - drain);
//                }
//            }
//        }

//        return true;
//    }

//    private static HashSet<int> GetInitialMovableCells(
//        int rows, int cols, int[,] layerMap,
//        List<Direction> dirs, List<PassengerColor> colors)
//    {
//        HashSet<int> movable = new();

//        for (int x = 0; x < rows; x++)
//        {
//            for (int z = 0; z < cols; z++)
//            {
//                int idx = x * cols + z;
//                if (IsCellMovable(x, z, idx, rows, cols, layerMap,
//                                  new bool[rows * cols], dirs))
//                    movable.Add(idx);
//            }
//        }

//        return movable;
//    }

//    private static bool IsCellMovable(int x, int z, int idx,
//                                       int rows, int cols, int[,] layerMap,
//                                       bool[] cleared, List<Direction> dirs)
//    {
//        Direction dir = dirs[idx];

//        // Check if path to border is clear
//        int cx = x, cz = z;
//        while (true)
//        {
//            switch (dir)
//            {
//                case Direction.Right: cx++; break;
//                case Direction.Left: cx--; break;
//                case Direction.Forward: cz++; break;
//                case Direction.Backward: cz--; break;
//            }

//            // Reached border → path clear
//            if (cx < 0 || cx >= rows || cz < 0 || cz >= cols)
//                return true;

//            int nextIdx = cx * cols + cz;
//            if (!cleared[nextIdx])
//                return false; // blocked
//        }
//    }

//    private static void UpdateMovable(int clearedIdx,
//                                       int rows, int cols,
//                                       int[,] layerMap,
//                                       List<Direction> dirs,
//                                       bool[] cleared,
//                                       HashSet<int> movable,
//                                       List<PassengerColor> colors)
//    {
//        // After clearing a cell, re-check all non-cleared cells
//        // (only cells that could be unblocked by this clearing matter,
//        //  but for correctness we re-scan all)
//        for (int x = 0; x < rows; x++)
//        {
//            for (int z = 0; z < cols; z++)
//            {
//                int idx = x * cols + z;
//                if (cleared[idx]) continue;

//                if (IsCellMovable(x, z, idx, rows, cols, layerMap, cleared, dirs))
//                    movable.Add(idx);
//                else
//                    movable.Remove(idx);
//            }
//        }
//    }

//    private static int FindMovableMatch(HashSet<int> movable,
//                                         List<PassengerColor> colors,
//                                         bool[] cleared,
//                                         PassengerColor target)
//    {
//        foreach (int idx in movable)
//        {
//            if (!cleared[idx] && colors[idx] == target)
//                return idx;
//        }
//        return -1;
//    }

//    private static int FindAnyMovable(HashSet<int> movable, bool[] cleared)
//    {
//        foreach (int idx in movable)
//        {
//            if (!cleared[idx]) return idx;
//        }
//        return -1;
//    }

//    // ─── helpers ──────────────────────────────────────────────────────────────

//    private static List<PassengerColor> GetShuffledColors(int count)
//    {
//        List<PassengerColor> all = new()
//        {
//            PassengerColor.Red,
//            PassengerColor.Blue,
//            PassengerColor.Green,
//            PassengerColor.Yellow,
//            PassengerColor.Orange,
//            PassengerColor.Purple,
//            PassengerColor.Pink,
//            PassengerColor.Cyan
//        };

//        Shuffle(all);
//        return all.GetRange(0, Mathf.Min(count, all.Count));
//    }

//    private static List<PassengerColor> GetDistinctSequence(List<PassengerColor> busOrder)
//    {
//        List<PassengerColor> seen = new();
//        HashSet<PassengerColor> hs = new();

//        foreach (var c in busOrder)
//            if (hs.Add(c)) seen.Add(c);

//        return seen;
//    }

//    private static int RoundUpToMultiple(int value, int multiple)
//    {
//        return Mathf.CeilToInt((float)value / multiple) * multiple;
//    }

//    private static void Shuffle<T>(List<T> list)
//    {
//        for (int i = list.Count - 1; i > 0; i--)
//        {
//            int j = Random.Range(0, i + 1);
//            (list[i], list[j]) = (list[j], list[i]);
//        }
//    }
//}






//using System.Collections.Generic;
//using UnityEngine;
//using static GameEnums;

//public static class LevelGenerator
//{
//    private const int MAX_ATTEMPTS = 200;

//    public static void Generate(LevelConfig config)
//    {
//        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
//        {
//            if (TryGenerate(config))
//                return;
//        }

//        Debug.LogError("Failed to generate level after max attempts.");
//    }

//    // =========================
//    // MAIN GENERATION
//    // =========================

//    private static bool TryGenerate(LevelConfig config)
//    {
//        int rows = config.gridRows;
//        int cols = config.gridCols;

//        int total = rows * cols;
//        if (total % 3 != 0)
//            return false;

//        int groupCount = total / 3;

//        // 1️⃣ Generate random bus order
//        List<PassengerColor> busOrder = GenerateBusOrder(groupCount);

//        // 2️⃣ Construct grid reverse
//        PassengerColor[,] colorGrid;
//        Direction[,] arrowGrid;

//        if (!ConstructGrid(rows, cols, busOrder, out colorGrid, out arrowGrid))
//            return false;

//        // 3️⃣ Apply to config
//        ApplyToConfig(config, rows, cols, busOrder, colorGrid, arrowGrid);

//        // 4️⃣ Final safety validation using your DFS solver
//        var result = LevelSolver.SolveLevel(config);
//        return result.isSolvable;
//    }

//    // =========================
//    // BUS ORDER
//    // =========================

//    private static List<PassengerColor> GenerateBusOrder(int groupCount)
//    {
//        List<PassengerColor> order = new List<PassengerColor>();
//        var colors = System.Enum.GetValues(typeof(PassengerColor));

//        for (int i = 0; i < groupCount; i++)
//        {
//            order.Add((PassengerColor)colors.GetValue(
//                Random.Range(0, colors.Length)));
//        }

//        Shuffle(order);
//        return order;
//    }

//    // =========================
//    // CONSTRUCT GRID (REVERSE)
//    // =========================

//    private static bool ConstructGrid(
//        int rows,
//        int cols,
//        List<PassengerColor> busOrder,
//        out PassengerColor[,] colorGrid,
//        out Direction[,] arrowGrid)
//    {
//        colorGrid = new PassengerColor[rows, cols];
//        arrowGrid = new Direction[rows, cols];

//        List<Vector2Int> spiral = GenerateSpiral(rows, cols);

//        if (spiral.Count != rows * cols)
//            return false;

//        int index = 0;

//        // Reverse fill
//        for (int g = busOrder.Count - 1; g >= 0; g--)
//        {
//            PassengerColor color = busOrder[g];

//            for (int i = 0; i < 3; i++)
//            {
//                if (index >= spiral.Count)
//                    return false;

//                Vector2Int pos = spiral[index++];
//                colorGrid[pos.x, pos.y] = color;
//            }
//        }

//        // Assign arrows with safe outward preference
//        if (!AssignArrows(rows, cols, colorGrid, arrowGrid))
//            return false;

//        return true;
//    }

//    // =========================
//    // SPIRAL LAYERING
//    // =========================

//    private static List<Vector2Int> GenerateSpiral(int rows, int cols)
//    {
//        List<Vector2Int> result = new List<Vector2Int>();

//        int top = 0, bottom = rows - 1;
//        int left = 0, right = cols - 1;

//        while (top <= bottom && left <= right)
//        {
//            for (int i = left; i <= right; i++)
//                result.Add(new Vector2Int(top, i));
//            top++;

//            for (int i = top; i <= bottom; i++)
//                result.Add(new Vector2Int(i, right));
//            right--;

//            if (top <= bottom)
//            {
//                for (int i = right; i >= left; i--)
//                    result.Add(new Vector2Int(bottom, i));
//                bottom--;
//            }

//            if (left <= right)
//            {
//                for (int i = bottom; i >= top; i--)
//                    result.Add(new Vector2Int(i, left));
//                left++;
//            }
//        }

//        return result;
//    }

//    // =========================
//    // ARROW ASSIGNMENT
//    // =========================

//    private static bool AssignArrows(
//        int rows,
//        int cols,
//        PassengerColor[,] colorGrid,
//        Direction[,] arrowGrid)
//    {
//        for (int r = 0; r < rows; r++)
//        {
//            for (int c = 0; c < cols; c++)
//            {
//                // Prefer outward directions first
//                List<Direction> directions = GetDirectionalPriority(r, c, rows, cols);

//                bool assigned = false;

//                foreach (var dir in directions)
//                {
//                    arrowGrid[r, c] = dir;

//                    if (PathLeadsOutside(r, c, dir, rows, cols))
//                    {
//                        assigned = true;
//                        break;
//                    }
//                }

//                if (!assigned)
//                    return false;
//            }
//        }

//        return true;
//    }

//    private static List<Direction> GetDirectionalPriority(int r, int c, int rows, int cols)
//    {
//        List<Direction> list = new List<Direction>();

//        // outward preference
//        if (r == 0) list.Add(Direction.Forward);
//        if (r == rows - 1) list.Add(Direction.Backward);
//        if (c == 0) list.Add(Direction.Left);
//        if (c == cols - 1) list.Add(Direction.Right);

//        // remaining directions
//        Direction[] all =
//        {
//            Direction.Left,
//            Direction.Right,
//            Direction.Forward,
//            Direction.Backward
//        };

//        foreach (var d in all)
//            if (!list.Contains(d))
//                list.Add(d);

//        return list;
//    }

//    private static bool PathLeadsOutside(int x, int y, Direction dir, int rows, int cols)
//    {
//        Vector2Int d = ConvertDirection(dir);

//        int nx = x + d.x;
//        int ny = y + d.y;

//        while (nx >= 0 && ny >= 0 && nx < rows && ny < cols)
//        {
//            nx += d.x;
//            ny += d.y;
//        }

//        return true;
//    }

//    // =========================
//    // APPLY TO CONFIG
//    // =========================

//    private static void ApplyToConfig(
//        LevelConfig config,
//        int rows,
//        int cols,
//        List<PassengerColor> busOrder,
//        PassengerColor[,] colorGrid,
//        Direction[,] arrowGrid)
//    {
//        config.passengerColors.Clear();
//        config.passengerDirections.Clear();
//        config.busOrder.Clear();

//        for (int r = 0; r < rows; r++)
//        {
//            for (int c = 0; c < cols; c++)
//            {
//                config.passengerColors.Add(colorGrid[r, c]);
//                config.passengerDirections.Add(arrowGrid[r, c]);
//            }
//        }

//        foreach (var color in busOrder)
//            config.busOrder.Add(color);

//        config.carCapacity = 3;
//        config.difficulty = Difficulty.Hard;
//    }

//    // =========================
//    // UTILITIES
//    // =========================

//    private static void Shuffle<T>(List<T> list)
//    {
//        for (int i = 0; i < list.Count; i++)
//        {
//            int rand = Random.Range(i, list.Count);
//            T temp = list[i];
//            list[i] = list[rand];
//            list[rand] = temp;
//        }
//    }

//    private static Vector2Int ConvertDirection(Direction dir)
//    {
//        switch (dir)
//        {
//            case Direction.Right: return new Vector2Int(0, 1);
//            case Direction.Left: return new Vector2Int(0, -1);
//            case Direction.Forward: return new Vector2Int(-1, 0);
//            case Direction.Backward: return new Vector2Int(1, 0);
//        }
//        return Vector2Int.zero;
//    }
//}



