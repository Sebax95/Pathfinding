using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class Player : BaseMonoBehaviour
{
    private FSM<Player> _fsm;
    
    [SerializeField] private Vector3 _targetMarker;
    
    [SerializeField] private LayerMask _groundMask;
    
    [SerializeField] private Camera _mainCamera;
    
    private Vector2Int _lastGridPosition;
    
    public List<Node> actualPath;

    #region Pathfinding Variables
    private Queue<Node> _currentPath;
    private bool _isUpdatingPath;
    #endregion

    #region Waypoint

    public WaypointNode initialNode;
    public WaypointNode finalNode;

    #endregion

    public float speed;
    private Tween _currentTween;

    
    protected override void Start()
    {
        base.Start();
        _currentPath = new ();
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

        if (currentGridPos == _lastGridPosition) 
            return;
        SpatialGrid.Instance.UpdateObjectPosition(gameObject);
        _lastGridPosition = currentGridPos;
    }

    private void InitializeFSM()
    {
        _fsm = new FSM<Player>(this);
        _fsm.AddState(PlayerState.Idle, new IdleState(this, _fsm));
        _fsm.AddState(PlayerState.Follow, new WalkState(this, _fsm));
        _fsm.AddState(PlayerState.Patrol, new PatrolState(this, _fsm));
        _fsm.SetState(PlayerState.Patrol);
    }

    public override void OnUpdate() => _fsm.Update();
    
    public bool GetMouseWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundMask)) 
            return false;
        position = hit.point;
        return true;
    }

    public void SetTargetPosition(Vector3 newPosition)
    { 
        Debug.Log("Llamando al pathfinding");
        SetTarget(newPosition);
        actualPath = Pathfinding.Instance.FindPath(transform.position, newPosition);
        _currentPath = new Queue<Node>(actualPath);
    }
    private void RequestPathToTarget()
    {
        if (_targetMarker == null)
            return;
        
        Pathfinding.Instance.FindPath(transform.position, _targetMarker);
        _fsm.SetState(PlayerState.Follow);
    }
    public Vector3 GetTarget() => _targetMarker;

    private void SetTarget(Vector3 newTarget) => _targetMarker = newTarget;

    #region PathFinding Movement

    public void MoveToNextNode(Action onComplete = null)
    {
        if (_currentPath.Count <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        Node targetNode = _currentPath.Dequeue();
        Vector3 targetPosition = new Vector3(
            targetNode.WorldPosition.x,
            transform.position.y,
            targetNode.WorldPosition.z
        );
        _currentTween = transform.DOMove(targetPosition, CalculateDuration(targetPosition))
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                MoveToNextNode(onComplete);
                GridManager.Instance.UpdatePaths(_currentPath.ToList());
            });
    }

    private float CalculateDuration(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        return Mathf.Clamp(distance / 3f, 0.15f, 0.5f);
    }

    private bool ValidatePath()
    {
        if (_currentPath == null || _currentPath.Count == 0)
        {
            Debug.Log("Camino no válido");
            return false;
        }
        return true;
    }

    #endregion
    protected override void OnDestroy()
    {
        base.OnDestroy();
        SpatialGrid.Instance.UnregisterObject(gameObject);

    }
}

public enum PlayerState
{
    Idle,
    Follow,
    Patrol
}