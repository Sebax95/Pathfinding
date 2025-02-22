using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class WalkState : State<Player>
{
    private Tween _currentTween;
    private Queue<Node> _currentPath;
    private bool _isUpdatingPath;

    public WalkState(Player owner, FSM<Player> fsm) : base(owner, fsm) { }

    public override void Enter()
    {
        LoadInitialPath();
    }

    public override void Execute() => _owner.UpdateGridPosition();

    public override void FixedExecute() { }

    private void LoadInitialPath()
    {
        _currentPath = new Queue<Node>(_owner.actualPath);
        if (!ValidatePath()) return;
        
        StartMovement();
    }
    private void StartMovement()
    {
        if (!ValidatePath()) return;
        MoveToNextNode();
    }

    private void MoveToNextNode()
    {
        if (_currentPath.Count <= 0)
        {
            _fsm.SetState(PlayerState.Idle);
            return;
        }

        Node targetNode = _currentPath.Dequeue();
        Vector3 targetPosition = new Vector3(
            targetNode.WorldPosition.x,
            _owner.transform.position.y,
            targetNode.WorldPosition.z
        );

        _currentTween = _owner.transform.DOMove(targetPosition, CalculateDuration(targetPosition))
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                MoveToNextNode();
                GridManager.Instance.UpdatePaths(_currentPath.ToList());
            });
    }

    private float CalculateDuration(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(_owner.transform.position, targetPosition);
        return Mathf.Clamp(distance / 3f, 0.15f, 0.5f);
    }

    private bool ValidatePath()
    {
        if (_currentPath == null || _currentPath.Count == 0)
        {
            Debug.Log("Camino no válido");
            _fsm.SetState(PlayerState.Idle);
            return false;
        }
        return true;
    }

    public override void Exit()
    {
        _currentTween?.Kill();
        _owner.StopAllCoroutines();
    }
}