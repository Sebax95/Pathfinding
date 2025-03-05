using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PatrolState: State<Player>
{
    private List<WaypointNode> _nodes;
    private int _currentNodeIndex;
    private bool _isReversing;
    private bool _reversePatrolling;
    public PatrolState(Player owner, FSM<Player> fsm) : base(owner, fsm) { }

    #region Patrol Methods

    

    private List<WaypointNode> GetAllNodes(WaypointNode initialNode)
    {
        List<WaypointNode> l = new() { initialNode };
        if (initialNode.nextNode != null)
            l.AddRange(GetAllNodes(initialNode.nextNode));
        return l;
    }

    private void MoveToNode()
    {
        var node = _nodes[_currentNodeIndex];
        _owner.SetTargetPosition(node.transform.position);
        _owner.MoveToNextNode(OnNodeReached);
    }
    
    private void OnNodeReached()
    {
        UpdateNodeIndex();
        MoveToNode();
    }

    private void UpdateNodeIndex()
    {
        if (_reversePatrolling)
        {
            _currentNodeIndex += _isReversing ? -1 : 1;

            if (_currentNodeIndex == 0 || _currentNodeIndex == _nodes.Count - 1)
                _isReversing = !_isReversing;
        }
        else
            _currentNodeIndex = (_currentNodeIndex + 1) % _nodes.Count;
    }
    #endregion


    #region State Methods

    public override void Enter()
    {
        _nodes = GetAllNodes(_owner.initialNode);
        _reversePatrolling = false;
        _currentNodeIndex = 0;
        _isReversing = false;
        MoveToNode();
    }

    public override void Execute()
    {
        _owner.UpdateGridPosition();
        if(_owner.CheckLineOfSight())
           _fsm.SetState(PlayerState.Chase);
    }

    public override void FixedExecute() { }

    public override void Exit()
    {
        _owner.StopMoving();
    }

    #endregion
    
}