using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{
    public static SpatialGrid Instance;
    
    public Vector3 GridOrigin = Vector3.zero;
    public Vector2Int GridDimensions = new(10, 10);
    public float CellSize = 5f;
    
    private Dictionary<Vector2Int, List<GameObject>> _grid = new();
    private Dictionary<GameObject, Vector2Int> _objectPositions = new();

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < GridDimensions.x; x++)
            for (int y = 0; y < GridDimensions.y; y++)
                _grid.Add(new Vector2Int(x, y), new List<GameObject>());
    }

    public void RegisterObject(GameObject obj)
    {
        Vector2Int gridPos = WorldToGridPosition(obj.transform.position);
        if (IsValidGridPosition(gridPos))
        {
            _grid[gridPos].Add(obj);
            _objectPositions[obj] = gridPos;
        }
    }

    public void UnregisterObject(GameObject obj)
    {
        if (_objectPositions.TryGetValue(obj, out Vector2Int gridPos))
        {
            _grid[gridPos].Remove(obj);
            _objectPositions.Remove(obj);
        }
    }

    public void UpdateObjectPosition(GameObject obj)
    {
        Vector2Int newGridPos = WorldToGridPosition(obj.transform.position);
        if (!_objectPositions.TryGetValue(obj, out Vector2Int currentGridPos) || currentGridPos != newGridPos)
        {
            if (IsValidGridPosition(currentGridPos))
                _grid[currentGridPos].Remove(obj);

            if (IsValidGridPosition(newGridPos))
            {
                _grid[newGridPos].Add(obj);
                _objectPositions[obj] = newGridPos;
            }
        }
    }

    public List<GameObject> GetNearbyObjects(Vector3 worldPosition, int radius = 1)
    {
        List<GameObject> nearbyObjects = new List<GameObject>();
        Vector2Int center = WorldToGridPosition(worldPosition);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);
                if (IsValidGridPosition(checkPos))
                {
                    nearbyObjects.AddRange(_grid[checkPos]);
                }
            }
        }
        return nearbyObjects;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - GridOrigin.x) / CellSize);
        int y = Mathf.FloorToInt((worldPosition.z - GridOrigin.z) / CellSize);
        return new Vector2Int(x, y);
    }

    private bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < GridDimensions.x &&
               gridPosition.y >= 0 && gridPosition.y < GridDimensions.y;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        for (int x = 0; x < GridDimensions.x; x++)
        {
            for (int y = 0; y < GridDimensions.y; y++)
            {
                Vector3 cellCenter = CalculateCellCenter(x, y);
                DrawCell(cellCenter, x, y);
                //DrawCellInfo(cellCenter, x, y);
            }
        }
    }

    private Vector3 CalculateCellCenter(int x, int y)
    {
        return new Vector3(
            GridOrigin.x + (x * CellSize) + (CellSize / 2),
            0,
            GridOrigin.z + (y * CellSize) + (CellSize / 2)
        );
    }

    private void DrawCell(Vector3 center, int x, int y)
    {
        // Color basado en la densidad de objetos
        int objectCount = _grid[new Vector2Int(x, y)].Count;
        Color cellColor = objectCount > 0 ? Color.red : Color.cyan;
        
        Gizmos.color = cellColor;
        Gizmos.DrawWireCube(center, new Vector3(CellSize * 0.9f, 0.1f, CellSize * 0.9f));
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(CellSize, 0.1f, CellSize));
    }

    private void DrawCellInfo(Vector3 center, int x, int y)
    {
#if UNITY_EDITOR
        string labelText = $"[{x},{y}]\nObjs: {_grid[new Vector2Int(x, y)].Count}";
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(center + Vector3.up * 0.2f, labelText);
#endif
    }
}
