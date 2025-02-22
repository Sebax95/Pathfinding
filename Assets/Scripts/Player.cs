using System.Collections.Generic;
using UnityEngine;

public class Player : BaseMonoBehaviour
{
    private FSM<Player> _fsm;
    [SerializeField] private Vector3 _targetMarker;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private Camera _mainCamera;
    private Vector2Int _lastGridPosition;
    public List<Node> actualPath;
    protected override void Start()
    {
        base.Start();
        InitializeFSM();
        RegisterInGrid();
    }

    private void RegisterInGrid()
    {
        SpatialGrid.Instance.RegisterObject(gameObject);
        _lastGridPosition = SpatialGrid.Instance.WorldToGridPosition(transform.position);
    }
    public void UpdateGridPosition()
    {
        Vector2Int currentGridPos = SpatialGrid.Instance.WorldToGridPosition(transform.position);
        
        if(currentGridPos != _lastGridPosition)
        {
            SpatialGrid.Instance.UpdateObjectPosition(gameObject);
            _lastGridPosition = currentGridPos;
        }
    }

    private void InitializeFSM()
    {
        _fsm = new FSM<Player>(this);
        _fsm.AddState(PlayerState.Idle, new IdleState(this, _fsm));
        _fsm.AddState(PlayerState.Follow, new WalkState(this, _fsm));
        _fsm.SetState(PlayerState.Idle);
    }

    public override void OnUpdate()
    {
        _fsm.Update();
    }
   
    public bool GetMouseWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundMask))
        {
            position = hit.point;
            return true;
        }
        return false;
    }

    public void SetTargetPosition(Vector3 newPosition)
    { 
        SetTarget(newPosition);
        actualPath = Pathfinding.Instance.FindPath(transform.position, newPosition);
    }
    public Vector3 GetTarget() => _targetMarker;
    private void RequestPathToTarget()
    {
        if (_targetMarker == null)
            return;
        
        Pathfinding.Instance.FindPath(transform.position, _targetMarker);
        _fsm.SetState(PlayerState.Follow);
    }

    public void SetTarget(Vector3 newTarget) => _targetMarker = newTarget;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SpatialGrid.Instance.UnregisterObject(gameObject);

    }
}

public enum PlayerState
{
    Idle,
    Follow
}