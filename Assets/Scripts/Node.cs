using UnityEngine;

public class Node
{
    public Vector3 WorldPosition { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public float GCost { get; set; }
    public float HCost { get; set; }
    public float Weight { get; set; }
    
    public float FCost => GCost + HCost;
    public bool IsWalkable { get; set; }

    public Node ParentNode { get; set; }

    public Node(bool isWalkable, Vector3 worldPosition, int gridX, int gridY, float weight = 0)
    {
        IsWalkable = isWalkable;
        WorldPosition = worldPosition;
        GridX = gridX;
        GridY = gridY;
        Weight = weight;
    }

    public float GetDistance(Node other)
    {
        return Vector3.Distance(WorldPosition, other.WorldPosition);
    }

    public bool HasLineOfSight(Node targetNode) => Physics.Raycast(WorldPosition,
        (targetNode.WorldPosition - WorldPosition).normalized,
        Vector3.Distance(WorldPosition, targetNode.WorldPosition));
}