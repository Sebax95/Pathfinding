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

    public override void Enter() { }

    public override void Execute() => _owner.UpdateGridPosition();

    public override void FixedExecute() { }
    
    public override void Exit()
    {
        _currentTween?.Kill();
        _owner.StopAllCoroutines();
    }
}