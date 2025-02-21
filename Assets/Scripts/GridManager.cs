using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//mi antigua Grid para mi final de ia1
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public LayerMask unwalkableMask;
    public Vector2 sizeGrid;
    public float nodeRaidus;
    public Node[,] grid;
    public bool gridWhite;

    int gridSizeX, gridSizeY;
    float nodeDiameter;
    public float wallPenaltyRadius = 5;
    public List<Node> Path { get; set; }

    
    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(this);
        nodeDiameter = nodeRaidus * 2;
        gridSizeX = Mathf.RoundToInt(sizeGrid.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(sizeGrid.y / nodeDiameter);
        GenerateGrid();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
            ResetGrid();
    }

    private void ResetGrid()
    {
        grid = null;
        GenerateGrid();
    }
    
    private void GenerateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * sizeGrid.x / 2 - Vector3.forward * sizeGrid.y / 2;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRaidus) + Vector3.forward * (y * nodeDiameter + nodeRaidus);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRaidus, unwalkableMask));
                float movementPenalty = 0;
                if(walkable)
                    if (Physics.CheckSphere(worldPoint, nodeRaidus * wallPenaltyRadius, unwalkableMask)) 
                        movementPenalty = 1;
                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }
    }
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                int checkX = node.GridX + x;
                int checkY = node.GridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) 
                    neighbours.Add(grid[checkX, checkY]);
            }
        }
        return neighbours;
    }

    public Node GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + sizeGrid.x / 2) / sizeGrid.x;
        float percentY = (worldPosition.z + sizeGrid.y / 2) / sizeGrid.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }



    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(sizeGrid.x, .5f, sizeGrid.y));
        if (grid == null || !gridWhite)
            return;
        foreach (var item in grid)
        {
            Gizmos.color =  item.Weight > 0 ? Color.gray : Color.white;
            Gizmos.color = !item.IsWalkable ? Color.red : Gizmos.color;
            if (Path != null) 
                Gizmos.color = Path.Contains(item) ? Color.green : Gizmos.color;
            Gizmos.DrawCube(item.WorldPosition, Vector3.one * (nodeDiameter));
        }
    }
}
