using System.Collections.Generic;
using UnityEngine;
using static GameEnums;
public class GridGenerator : MonoBehaviour
{ 
    [Header("Grid Settings")]
    [SerializeField] private int rows = 8;
    [SerializeField] private int columns = 8;
    //[SerializeField] private float cellSpacing = 1f;

    [Header("Prefab")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject ExtraPrefab;

    [Header("Generate Grid")]
    [SerializeField] private bool generateOnStart = true;
    private GridCell[,] gridArray;

    [SerializeField] private GameObject WaitingSlots;
    [SerializeField] private float waitingAreaMargin = 2f;

    [SerializeField] float padding = 1f;  
    private float cellSpacing;
    [SerializeField] float gridHeightPercent = 0.6f;
     
    [SerializeField] private float minCellSize = 0.8f;
    [SerializeField] private float maxCellSize = 1.5f;

   [SerializeField]  private float percentage = 0.4f;

    [SerializeField] float cellSize = 1f; 
    [SerializeField] private float cellGap = 0.12f;
    [SerializeField] private float spacingBelowWaiting = 1f;
    [Header("Camera")]
    [SerializeField] private float CameraMargin = 1.5f;
    private float defaultOrthoSize;
    private Vector3 defaultCamPos;

    public int MaxX;
    public int MaxZ;
    private void Start()
    {
        //if (generateOnStart)
        //    GenerateGrid();
    }

    private void Awake()
    {
        Camera cam = Camera.main;
        defaultOrthoSize = cam.orthographicSize;
        defaultCamPos = cam.transform.position;
    }

    public void SetGridSize(int newRows, int newColumns)
    {
        rows = newRows;
        columns = newColumns; 
    }

    [ContextMenu("Generate Grid")]
    
    Vector2 GetCameraWorldSize()
    {
        Camera cam = Camera.main;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        return new Vector2(width, height);
    }
    public void GenerateGrid()
    {
        Debug.Log("Rows: " + rows + ", Columns: " + columns);
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        MaxX = rows-1;
        MaxZ = columns-1 ;
        int ExpandedRows = rows ;
        int ExpandedColumns = columns ;
        gridArray = new GridCell[ExpandedRows, ExpandedColumns];

        float step = cellSize + cellGap;

        float totalWidth = (ExpandedRows - 1) * step;
        float totalHeight = (ExpandedColumns - 1) * step;
        float waitingBottom = 0f;

        if (WaitingSlots != null)
        {
            Renderer r = WaitingSlots.GetComponentInChildren<Renderer>();

            if (r != null)
            {
                waitingBottom = r.bounds.min.z;
            }
            else
            {
                Debug.LogError("No Renderer found inside WaitingSlots");
                waitingBottom = 0f;
            }

        } 

        float gridTopLimit = waitingBottom - spacingBelowWaiting;
         
        float gridStartZ = gridTopLimit - totalHeight;


        //Vector3 origin = new Vector3(-totalWidth / 2f, 0f,-totalHeight / 2f );
        Vector3 origin = new Vector3(-totalWidth / 2f, 0f,gridStartZ);

        for (int x = 0; x < ExpandedRows; x++)
        {
            for (int z = 0; z < ExpandedColumns; z++)
            {
 
                bool isBorder =z == 0 ||x == 0 ||x == ExpandedRows - 1||z==ExpandedColumns-1;

                Vector3 pos = origin + new Vector3(x * step, 0, z * step);
                if (isBorder)
                {
                    GameObject extra = Instantiate(ExtraPrefab, pos, Quaternion.identity, transform);
                    extra.transform.localScale = new Vector3(cellSize, 0.04f, cellSize);
                    extra.name = $"Extra_{x}_{z}";
                    GridCell gc = extra.GetComponent<GridCell>();
                    gc.x = x;
                    gc.z = z;
                    gridArray[x, z] = gc; 
                }
                else
                {
                     

                    GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                    cell.transform.localScale = new Vector3(cellSize, 0.04f, cellSize);
                    cell.name = $"Cell_{x}_{z}";

                    GridCell gc = cell.GetComponent<GridCell>();
                    gc.x = x;
                    gc.z = z;
                    gridArray[x, z] = gc; 
                }
            }
        }

        CameraAutoZoom(totalWidth, totalHeight);
    }
    void CameraAutoZoom(float gridWidth, float gridHeight)
    {
        Camera cam = Camera.main;

        float requiredHalfHeight = gridHeight / 2f + CameraMargin;
        float requiredHalfWidth = gridWidth / 2f + CameraMargin;

        float requiredSize = Mathf.Max(
            requiredHalfHeight,
            requiredHalfWidth / cam.aspect
        );
         
        float finalSize = Mathf.Max(defaultOrthoSize, requiredSize);

        cam.orthographicSize = finalSize;
         
        float zoomDifference = finalSize - defaultOrthoSize;

        if (zoomDifference > 0)
        {
            cam.transform.position = defaultCamPos + new Vector3(0, 0, -zoomDifference * 0.5f);
        }
        else
        {
            cam.transform.position = defaultCamPos;
        }
    } 

    public void ClearGrid()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }


    public bool IsPathClear(GridCell startCell, Direction dir)
    {
        int x = startCell.x;
        int z = startCell.z;

        while (true)
        {
            switch (dir)
            {
                case Direction.Forward: z += 1; break;
                case Direction.Backward: z -= 1; break;
                case Direction.Left: x -= 1; break;
                case Direction.Right: x += 1; break;
            }

            GridCell nextCell = GetCell(x, z);
            if (nextCell == null)
                return true;
            if (nextCell.CurrentPassanger != null && !nextCell.CurrentPassanger.isMoving)
                return false;
            //if (nextCell.IsOccupied)
            //    return false;
        }
    }
    public GridCell GetCell(int x, int z)
    {
        if (x < 0 || x >= rows || z < 0 || z >= columns)
            return null;

        return gridArray[x, z];
    }

    public List<GridCell> GetAllCells()
    {
        List<GridCell> list = new();

        for (int x = 0; x < rows ; x++)
        {
            for (int z = 0; z < columns ; z++)
            {
                bool isBorder = z == 0 || x == 0 || x == rows-1||z==columns-1;
                if(isBorder)
                    continue;

                list.Add(gridArray[x, z]);
            }
        }

        return list;
    }


}

