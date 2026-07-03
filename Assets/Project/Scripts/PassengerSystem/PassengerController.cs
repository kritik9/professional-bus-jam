using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using static GameEnums;

public class PassengerController : MonoBehaviour
{ 
    [Header("Identity")]
    public PassengerColor color;
    public Direction arrowDirection;

    public Animator animator;
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float finalMoveSpeed = 6f;

    [Header("Arrow")]
    [SerializeField] public  GameObject arrowPrefab;
     

    public GridCell currentCell;

    public bool isInWaiting = false;

    public GameObject arrowInstance;

    private Transform arrowPoint;
    public bool isMoving = false;


    private void Awake()
    {
        arrowPoint = transform.GetChild(0);
    }


    public void MoveAlongPath(Action onReachedEdge)
    {
        if (isMoving)
            return;

        isMoving = true;
        SetArrowVisible(false);
        SetWalking(true);
        StartCoroutine(MoveCoroutine(onReachedEdge));
    }

    public void MoveToPosition(Vector3 targetPos, Action onArrived)
    {
        if (isMoving)
            return;

        isMoving = true;
        SetArrowVisible(false);
        SetWalking(true);
        StartCoroutine(MoveToPositionCoroutine(targetPos, onArrived));
    }
     

    private IEnumerator MoveCoroutine(Action onReachedEdge)
    {
        List<Vector2Int> path = CalculatePath(currentCell.x, currentCell.z);
        List<Vector3> waypoints = BuildWaypoints(path);

        if (waypoints.Count == 0)
        {
            isMoving = false;
            SetArrowVisible(false);
            SetWalking(false);
            onReachedEdge?.Invoke();
            yield break;
        }

        if (arrowInstance != null && waypoints.Count > 1)
        {
            UpdateArrowDirection(path[0], path[1]);
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 target = waypoints[i];

            if (i < waypoints.Count - 1 && arrowInstance != null)
            {
                UpdateArrowDirection(path[i], path[i + 1]);
            }

            // Rotate towards next waypoint
            Vector3 direction = target - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    moveSpeed * Time.deltaTime);

                yield return null;
            }

            transform.position = target;

            if (i == waypoints.Count - 1)
            {
                GridCell finalCell =
                    GameManager.Instance._grid.GetCell(path[i].x, path[i].y);

                if (finalCell != null)
                    SetCell(finalCell);
            }
        }

        isMoving = false;
        SetArrowVisible(false);
        SetWalking(false);
        onReachedEdge?.Invoke();
    }

    private IEnumerator MoveToPositionCoroutine(
        Vector3 targetPos,
        Action onArrived)
    {
        Vector3 startPos = transform.position;
        float journeyLength = Vector3.Distance(startPos, targetPos);
        float startTime = Time.time;
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            float distCovered = (Time.time - startTime) * finalMoveSpeed;
            float fraction = distCovered / journeyLength;

            transform.position = Vector3.Lerp(startPos, targetPos, fraction);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        onArrived?.Invoke();
    }
     
    private List<Vector3> BuildWaypoints(List<Vector2Int> path)
    {
        List<Vector3> waypoints = new();

        foreach (var pos in path)
        {
            GridCell cell =
                GameManager.Instance._grid.GetCell(pos.x, pos.y);

            if (cell == null ||
                (cell.IsOccupied && !cell.CurrentPassanger.isMoving))
                break;

            waypoints.Add(cell.transform.position + Vector3.up * 0f);
        }

        return waypoints;
    }

    private List<Vector2Int> CalculatePath(int startX, int startZ)
    {
        List<Vector2Int> path = new();

        int x = startX;
        int z = startZ;

        int maxX = GameManager.Instance._grid.MaxX;
        int maxZ = GameManager.Instance._grid.MaxZ;

        bool isOutwards = arrowDirection switch
        {
            Direction.Left => x > 0,
            Direction.Right => x < maxX,
            Direction.Forward => z < maxZ,
            Direction.Backward => z > 0,
            _ => false
        };

        if (isOutwards)
            BuildOutwardPath(path, ref x, ref z, maxX, maxZ);
        else
            BuildFallbackPath(path, ref x, ref z, maxX, maxZ);

        return path;
    }

    private void BuildOutwardPath(
        List<Vector2Int> path,
        ref int x,
        ref int z,
        int maxX,
        int maxZ)
    {
        switch (arrowDirection)
        {
            case Direction.Backward:
                while (z > 0) path.Add(new Vector2Int(x, --z));
                while (x < maxX) path.Add(new Vector2Int(++x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Left:
                while (x > 0) path.Add(new Vector2Int(--x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Right:
                while (x < maxX) path.Add(new Vector2Int(++x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Forward:
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;
        }
    }

    private void BuildFallbackPath(
        List<Vector2Int> path,
        ref int x,
        ref int z,
        int maxX,
        int maxZ)
    {
        switch (arrowDirection)
        {
            case Direction.Forward:
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Backward:
                while (z > 0) path.Add(new Vector2Int(x, --z));
                while (x < maxX) path.Add(new Vector2Int(++x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Left:
                while (x > 0) path.Add(new Vector2Int(--x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;

            case Direction.Right:
                while (x < maxX) path.Add(new Vector2Int(++x, z));
                while (z < maxZ) path.Add(new Vector2Int(x, ++z));
                break;
        }
    }
     
    public void InitializeArrow()
    {
        if (arrowPrefab == null || arrowPoint == null)
        {
            Debug.LogWarning("ArrowPrefab or ArrowPoint missing");
            return;
        }

        if (arrowInstance != null)
            Destroy(arrowInstance);

        arrowInstance = Instantiate(arrowPrefab, arrowPoint);
        arrowInstance.transform.localPosition = Vector3.zero;
        arrowInstance.transform.localRotation =
            Quaternion.Euler(90, GetArrowYRotation(), 0);
        arrowInstance.transform.localScale = new Vector3(40f, 40f, 40f);
    }

    private void UpdateArrowDirection(Vector2Int from, Vector2Int to)
    {
        if (arrowInstance == null)
            return;

        int dx = to.x - from.x;
        int dz = to.y - from.y;

        Direction newDir =
            dx > 0 ? Direction.Right :
            dx < 0 ? Direction.Left :
            dz > 0 ? Direction.Forward :
            dz < 0 ? Direction.Backward :
            arrowDirection;

        arrowInstance.transform.localRotation =
            Quaternion.Euler(90, GetArrowYRotation(newDir), 0);
    }

    private float GetArrowYRotation()
    {
        return GetArrowYRotation(arrowDirection);
    }

    private float GetArrowYRotation(Direction dir)
    {
        return dir switch
        {
            Direction.Forward => 90f,
            Direction.Backward => 270f,
            Direction.Left => 0f,
            Direction.Right => 180f,
            _ => 0f
        };
    } 
    public void SetCell(GridCell cell)
    {
        if (currentCell != null)
            currentCell.CurrentPassanger = null;

        currentCell = cell;
        cell.CurrentPassanger = this;

        transform.position = cell.transform.position + Vector3.up * 0f;
    }

    public void ApplyColor()
    {
        Color targetColor = color switch
        {
            PassengerColor.Red => new Color32(255, 70, 70, 255),    // Candy Red
            PassengerColor.Blue => new Color32(50, 155, 255, 255),   // Bright Blue
            PassengerColor.Green => new Color32(70, 220, 90, 255),    // Lime Green
            PassengerColor.Yellow => new Color32(255, 220, 40, 255),   // Candy Yellow
            PassengerColor.Orange => new Color32(255, 145, 35, 255),   // Orange Pop
            PassengerColor.Purple => new Color32(170, 80, 255, 255),   // Bright Purple
            PassengerColor.Pink => new Color32(255, 95, 180, 255),   // Bubblegum Pink
            PassengerColor.Cyan => new Color32(50, 225, 255, 255),   // Aqua
            PassengerColor.Gray => new Color32(145, 145, 160, 255),  // Neutral Gray
            PassengerColor.White => new Color32(255, 255, 255, 255),  // White
            _ => Color.white
        };

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = targetColor;
        }
    }
    private void SetArrowVisible(bool visible)
    {
        if (arrowInstance != null)
            arrowInstance.SetActive(visible);
    }
    private void SetWalking(bool value)
    {
        animator.SetBool("isWalking", value);
    }
    public void SetWaiting(bool value)
    {
        isMoving = false;
        isInWaiting = value;

        SetWalking(false);
    }
}