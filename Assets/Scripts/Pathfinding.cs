using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance;

    private const string PATH_NOT_FOUND_MESSAGE = "No se encontró un camino. Tiempo total transcurrido: {0} ms.";
    private const string PATH_FOUND_MESSAGE = "Path encontrado en {0} ms.";
    
    public bool UseTheta;
    public Transform StartPosition;
    public Transform TargetPosition;
    public float MaxExecutionTimePerFrame = 10f;

    private List<Node> _path;
    
    private void Awake()
    {
        InitializeInstance();
    }
    private void InitializeInstance()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public List<Node> FindPath(Vector3 start, Vector3 end)
    {
        StartCoroutine(FindPathWithTimeSlicing(start, end));
        return _path;
    }

    private IEnumerator FindPathWithTimeSlicing(Vector3 start, Vector3 target)
    {
        Stopwatch totalStopwatch = StartStopwatch();
        Node startNode = GridManager.Instance.GetNodeFromWorldPosition(start);
        Node targetNode = GridManager.Instance.GetNodeFromWorldPosition(target);

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestCostNode(openSet);
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                totalStopwatch.Stop();
                Debug.LogFormat(PATH_FOUND_MESSAGE, totalStopwatch.ElapsedMilliseconds);
                RetracePath(startNode, targetNode);
                yield break;
            }

            ProcessNeighbors(currentNode, targetNode, openSet, closedSet);

            if (!ShouldPauseExecution())
            {
                Debug.Log("Detengo ejecución para el siguiente frame...");
                yield return null;
            }
        }

        totalStopwatch.Stop();
        Debug.LogFormat(PATH_NOT_FOUND_MESSAGE, totalStopwatch.ElapsedMilliseconds);
    }

    private Stopwatch StartStopwatch()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        return stopwatch;
    }

    private Node GetLowestCostNode(List<Node> nodeList)
    {
        Node lowestCostNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            Node node = nodeList[i];
            if (node.FCost < lowestCostNode.FCost || (node.FCost == lowestCostNode.FCost && node.HCost < lowestCostNode.HCost)) 
                lowestCostNode = node;
        }

        return lowestCostNode;
    }

    private void ProcessNeighbors(Node currentNode, Node targetNode, List<Node> openSet, HashSet<Node> closedSet)
    {
        foreach (Node neighbor in GridManager.Instance.GetNeighbors(currentNode))
        {
            if (!CanProcessNeighbor(neighbor, closedSet))
                continue;

            float newGCostToNeighbor = GetNewGCost(currentNode, neighbor);

            if (newGCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
            {
                UpdateNeighborCosts(neighbor, currentNode, newGCostToNeighbor, targetNode);
                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }
    }

    private bool CanProcessNeighbor(Node neighbor, HashSet<Node> closedSet) =>
        neighbor.IsWalkable && !closedSet.Contains(neighbor);

    private float GetNewGCost(Node currentNode, Node neighbor) =>
        currentNode.GCost +
        Vector3.Distance(currentNode.WorldPosition, neighbor.WorldPosition) +
        neighbor.Weight;

    private void UpdateNeighborCosts(Node neighbor, Node currentNode, float newGCost, Node targetNode)
    {
        neighbor.GCost = newGCost;
        neighbor.HCost = Vector3.Distance(neighbor.WorldPosition, targetNode.WorldPosition);
        neighbor.ParentNode = currentNode;
    }

    private bool ShouldPauseExecution() =>
        Stopwatch.GetTimestamp() / (float)Stopwatch.Frequency > MaxExecutionTimePerFrame / 1000f;

    private void RetracePath(Node startNode, Node targetNode)
    {
        List<Node> path = BuildPathFromNodes(startNode, targetNode);
        _path = UseTheta ? OptimizePathWithTheta(path) : path;
        Debug.Log("Camino trazado con éxito.");
    }

    private List<Node> BuildPathFromNodes(Node startNode, Node targetNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.ParentNode;
        }

        path.Reverse();
        return path;
    }

    private List<Node> OptimizePathWithTheta(List<Node> path)
    {
        if (path == null || path.Count < 3)
            return path;

        List<Node> optimizedPath = new List<Node> { path[0] };
        int currentIndex = 0;

        for (int i = 2; i < path.Count; i++)
        {
            if (path[currentIndex].HasLineOfSight(path[i]))
            {
                optimizedPath.Add(path[i - 1]);
                currentIndex = i - 1;
            }
        }

        optimizedPath.Add(path[^1]);
        return optimizedPath;
    }
}