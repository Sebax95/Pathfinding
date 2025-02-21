using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Pathfinding : MonoBehaviour
{
    static Pathfinding Instance;
    
    public GridManager gridManager;

    public bool useTheta;
    public Transform startPosition;
    public Transform targetPosition;

    public float maxExecutionTimePerFrame = 10f;

    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
        else
            Destroy(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UnityEngine.Debug.Log("Iniciando búsqueda de camino con time slicing...");
            StartCoroutine(FindPathWithTimeSlicing(startPosition.position, targetPosition.position));
        }
    }

    IEnumerator FindPathWithTimeSlicing(Vector3 startPos, Vector3 targetPos)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Node startNode = gridManager.GetNodeFromWorldPosition(startPos);
        Node targetNode = gridManager.GetNodeFromWorldPosition(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Stopwatch frameStopwatch = new Stopwatch();
            frameStopwatch.Start();

            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                stopwatch.Stop();
                Debug.Log($"Path encontrado en {stopwatch.ElapsedMilliseconds} ms.");
                RetracePath(startNode, targetNode);
                yield break;
            }

            foreach (Node neighbor in gridManager.GetNeighbours(currentNode))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;

                float newGCostToNeighbor = currentNode.GCost +
                                           Vector3.Distance(currentNode.WorldPosition, neighbor.WorldPosition) + neighbor.Weight;

                if (newGCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newGCostToNeighbor;
                    neighbor.HCost = Vector3.Distance(neighbor.WorldPosition, targetNode.WorldPosition);
                    neighbor.ParentNode = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
            frameStopwatch.Stop();
            if (frameStopwatch.ElapsedMilliseconds > maxExecutionTimePerFrame)
            {
                Debug.Log("Detengo ejecución para el siguiente frame...");
                yield return null;
            }
        }

        stopwatch.Stop();
        Debug.Log($"No se encontró un camino. Tiempo total transcurrido: {stopwatch.ElapsedMilliseconds} ms.");
    }

    /*private List<Node> Theta(List<Node> path)
    {
        for (int i = 1; i < path.Count - 1; i++)
        {
            if (!path[i - 1].HasLineOfSight(path[i + 1]))
                path.RemoveAt(i--);
        }

        return path;
    }*/
    
    private List<Node> Theta(List<Node> path)
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

        optimizedPath.Add(path[^1]); // Agregar el nodo final
        return optimizedPath;
    }


    void RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.ParentNode;
        }

        path.Reverse();

        gridManager.Path = useTheta ? Theta(path) : path;
        UnityEngine.Debug.Log("Camino trazado con éxito.");
    }
}