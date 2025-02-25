using System;
using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public bool showGizmos = true;
    public WaypointNode nextNode;
    public WaypointNode prevNode;

    private void OnDrawGizmos()
    {
        if(!showGizmos) 
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f);
        if (nextNode != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nextNode.transform.position);
        }
    }
}
