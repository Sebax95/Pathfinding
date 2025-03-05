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

    #region Line Of Sight Variables

    public float visionRadius = 10f;
    public float fieldOfView = 90f;
    public LayerMask obstacleMask;
    public Transform target;
    public Vector3 lastPosition;
    #endregion

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

    public bool IsMoving
    {
        get => _currentTween != null && _currentTween.IsPlaying();
    }


    protected override void Start()
    {
        base.Start();
        _currentPath = new();
        InitializeFSM();
        RegisterInGrid();
    }
    private void InitializeFSM()
    {
        _fsm = new FSM<Player>(this);
        _fsm.AddState(PlayerState.Idle, new IdleState(this, _fsm));
        _fsm.AddState(PlayerState.Patrol, new PatrolState(this, _fsm));
        _fsm.AddState(PlayerState.Chase, new ChaseState(this, _fsm));
        _fsm.AddState(PlayerState.Shoot, new ShootState(this, _fsm));
        _fsm.SetState(PlayerState.Patrol);
    }

    #region SpatialGrid

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


    #endregion
    
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

    #region Line Of Sight

    public bool CheckLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        
        Vector3 directionToTarget = target.transform.position - origin;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        float distanceToTarget = Vector3.Distance(origin, target.transform.position);
        
        if (angleToTarget > fieldOfView || distanceToTarget > visionRadius)
            return false;
        if (Physics.Raycast(origin, directionToTarget.normalized, out RaycastHit hitInfo, distanceToTarget,
                obstacleMask))
        {
            Debug.DrawRay(origin, directionToTarget.normalized * hitInfo.distance, Color.red);
            return false;
        }
        
        Debug.DrawRay(origin, directionToTarget.normalized * distanceToTarget, Color.green);
        lastPosition = target.transform.position;
        return true;
    }

    #endregion

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
        
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        float duration = CalculateDuration(targetPosition);
        transform.DORotate(targetRotation.eulerAngles, duration)
            .SetEase(Ease.Linear);

        
        _currentTween = transform.DOMove(targetPosition, duration)
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
    
    public Vector3 GetTarget() => _targetMarker;

    private void SetTarget(Vector3 newTarget) => _targetMarker = newTarget;
    
    public void SetTargetPosition(Vector3 newPosition)
    { 
        SetTarget(newPosition);
        actualPath = Pathfinding.Instance.FindPath(transform.position, newPosition);
        _currentPath = new Queue<Node>(actualPath);
    }

    public void StopMoving()
    {
        _currentTween.Kill();
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f; 
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, visionRadius);
        Gizmos.color = Color.yellow;

        Vector3 rightLimit = Quaternion.AngleAxis(fieldOfView, transform.up) * transform.forward;
        Gizmos.DrawLine(origin, origin + (rightLimit * visionRadius));

        Vector3 leftLimit = Quaternion.AngleAxis(-fieldOfView, transform.up) * transform.forward;
        Gizmos.DrawLine(origin, origin + (leftLimit * visionRadius));

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(target.transform.position, origin);
        }
    }

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
    Patrol,
    Chase,
    Shoot
}