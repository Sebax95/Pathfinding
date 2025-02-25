using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    public LayerMask UnwalkableMask;
    public Vector2 GridSize;
    public float NodeRadius;
    public Node[,] Grid;
    public bool DisplayGridInGizmos;
    public float WallPenaltyRadius = 5f;

    private int _gridSizeX, _gridSizeY;
    private float _nodeDiameter;
    
    private List<Node> _path;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        InitializeGrid();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetGrid();
    }

    private void InitializeGrid()
    {
        _nodeDiameter = NodeRadius * 2;
        CalculateGridSize();
        GenerateGrid();
    }

    private void CalculateGridSize()
    {
        _gridSizeX = Mathf.RoundToInt(GridSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(GridSize.y / _nodeDiameter);
    }

    private void ResetGrid()
    {
        Grid = null;
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        Grid = new Node[_gridSizeX, _gridSizeY];
        Vector3 bottomLeft = transform.position - Vector3.right * GridSize.x / 2 - Vector3.forward * GridSize.y / 2;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPosition = GetWorldPosition(bottomLeft, x, y);
                bool isWalkable = !Physics.CheckSphere(worldPosition, NodeRadius, UnwalkableMask);
                float movementPenalty = CalculateMovementPenalty(worldPosition, isWalkable);

                Grid[x, y] =  new(isWalkable, worldPosition, x, y, movementPenalty);
            }
        }
    }

    private Vector3 GetWorldPosition(Vector3 bottomLeft, int x, int y) =>
        bottomLeft + Vector3.right * (x * _nodeDiameter + NodeRadius) +
        Vector3.forward * (y * _nodeDiameter + NodeRadius);

    private float CalculateMovementPenalty(Vector3 worldPosition, bool isWalkable) =>
        isWalkable && Physics.CheckSphere(worldPosition, NodeRadius * WallPenaltyRadius, UnwalkableMask)
            ? 1f
            : 0f;
       

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0)
                    continue;

                int neighborX = node.GridX + offsetX;
                int neighborY = node.GridY + offsetY;

                if (IsInBounds(neighborX, neighborY))
                    neighbors.Add(Grid[neighborX, neighborY]);
            }
        }
        return neighbors;
    }

    public Node FindNearNode(Vector3 position)
    {
        var dist = Mathf.Infinity;
        Node nearNode = null;
        foreach (var item in Grid)
        {
            if ((!(Vector3.Distance(item.WorldPosition, position) < dist))) 
                continue;
            dist = Vector3.Distance(item.WorldPosition, position);
            nearNode = item;
        }
        return nearNode;
    }

    private bool IsInBounds(int x, int y) => x >= 0 && x < _gridSizeX && y >= 0 && y < _gridSizeY;

    public Node GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        float percentX = Mathf.Clamp01((worldPosition.x + GridSize.x / 2) / GridSize.x);
        float percentY = Mathf.Clamp01((worldPosition.z + GridSize.y / 2) / GridSize.y);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

        return Grid[x, y];
    }
    public void UpdatePaths(List<Node> path) => _path = path;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(GridSize.x, 0.5f, GridSize.y));

        if (Grid == null || !DisplayGridInGizmos)
            return;

        foreach (var node in Grid)
        {
            Gizmos.color = node.Weight > 0 ? Color.gray : Color.white;
            Gizmos.color = !node.IsWalkable ? Color.red : Gizmos.color;
            if (_path != null && _path.Contains(node))
                Gizmos.color = Color.green;

            Gizmos.DrawCube(node.WorldPosition, Vector3.one * _nodeDiameter);
        }
    }
}