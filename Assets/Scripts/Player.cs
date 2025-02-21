using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

public class Player: BaseMonoBehaviour
{
    //private List<Node> _destination = new();
    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            MovePlayer(GridManager.Instance.Path, 3);
        }
    }

    private void MovePlayer(List<Node> path, float baseSpeed)
    {
        if (path == null || path.Count < 2) 
            return;
        
        var sequence = DOTween.Sequence();
        float fixedY = transform.position.y;

        foreach (var node in path)
        {
            Vector3 targetPosition = new Vector3(node.WorldPosition.x, fixedY, node.WorldPosition.z); 
            
            float distance = Vector3.Distance(transform.position, targetPosition);
            float duration = Mathf.Clamp(distance / baseSpeed, 0.2f, 0.3f); 
        
            sequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.Linear));
        }

        
        sequence.OnComplete(() => Debug.Log("Jugador llegó al destino."));
        //_destination.Clear();
    }
}