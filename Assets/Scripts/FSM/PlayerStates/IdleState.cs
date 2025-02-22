using UnityEngine;

public class IdleState : State<Player>
{
    public IdleState(Player owner, FSM<Player> fsm) : base(owner, fsm) { }

    public override void Enter() => Debug.Log("Entrando en estado Idle");

    public override void Execute()
    {
        HandleMouseInput();
    }
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_owner.GetMouseWorldPosition(out Vector3 targetPosition))
            {
                _owner.SetTargetPosition(targetPosition);
                _fsm.SetState(PlayerState.Follow);
            }
        }
    }
    public override void FixedExecute() { }
    public override void Exit() => Debug.Log("Saliendo de estado Idle");
}