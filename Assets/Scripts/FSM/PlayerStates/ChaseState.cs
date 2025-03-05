using System.Collections;
using UnityEngine;
using System.Linq;

public class ChaseState : State<Player>
{
    public ChaseState(Player owner, FSM<Player> fsm) : base(owner, fsm) { }

    Coroutine _actualCoroutine;
    
    public override void Enter()
    {
        Debug.Log("Entrando en estado Chase");
        if (_owner.target == null) 
            return;
        _owner.SetTargetPosition(_owner.target.transform.position);
        _owner.MoveToNextNode();
    }

    public override void Execute()
    {
        _owner.UpdateGridPosition();
        
        if (!_owner.CheckLineOfSight())
        {
            if (_actualCoroutine == null)
                _actualCoroutine = _owner.StartCoroutine(WaitForSeeEnemy());
            return;
        }
        
        if (_actualCoroutine != null)
        {
            _owner.StopCoroutine(_actualCoroutine);
            _actualCoroutine = null;
        }
        
        if (_owner.target != null)
        {
            float distanceToTarget = Vector3.Distance(_owner.transform.position, _owner.target.transform.position);
            
            if (distanceToTarget < _owner.visionRadius / 2)
            {
                _owner.StopMoving();
                //_fsm.SetState(PlayerState.Shoot); 
                return;
            }
        }
        else
        {
            if (_actualCoroutine == null)
                _actualCoroutine = _owner.StartCoroutine(WaitForSeeEnemy());
        }
    }

    IEnumerator WaitForSeeEnemy()
    {
        yield return new WaitForSeconds(0.5f);
        _fsm.SetState(PlayerState.Patrol);
    }

    public override void FixedExecute() { }

    public override void Exit()
    {
        _owner.StopMoving();
    }
}