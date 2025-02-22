using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class WalkState : State<Player>
{
    private Tween _currentTween;
    private List<Node> _currentPath;
    private Vector3 _lastWorldPosition;
    private bool _isUpdatingPath;

    public WalkState(Player owner, FSM<Player> fsm) : base(owner, fsm) { }

    public override void Enter()
    {
        _lastWorldPosition = _owner.transform.position;
        LoadInitialPath();
        _owner.StartCoroutine(PathUpdateLoop());
    }

    public override void Execute()
    {
        _owner.UpdateGridPosition();
    }

    public override void FixedExecute()
    {
        
    }

    private void LoadInitialPath()
    {
        _currentPath = new List<Node>(GridManager.Instance.Path);
        if (!ValidatePath()) return;
        
        StartMovement();
    }

    private IEnumerator PathUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            if (!_isUpdatingPath)
                UpdatePathSafely();
        }
    }

    private void UpdatePathSafely()
    {
        _isUpdatingPath = true;
        
        // Conservar el último nodo alcanzado
        int currentProgress = FindCurrentPathProgress();
        var newPath = GridManager.Instance.Path;
        
        if (newPath != null && newPath.Count > 0 && IsSameDestination(newPath))
        {
            // Fusionar caminos: mantener progreso anterior + nuevos nodos
            _currentPath = MergePaths(_currentPath, newPath, currentProgress);
        }
        
        _isUpdatingPath = false;
    }

    private int FindCurrentPathProgress()
    {
        float closestDistance = Mathf.Infinity;
        int closestIndex = 0;
        
        for (int i = 0; i < _currentPath.Count; i++)
        {
            float dist = Vector3.Distance(_lastWorldPosition, _currentPath[i].WorldPosition);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }
        return Mathf.Clamp(closestIndex - 1, 0, _currentPath.Count - 1);
    }

    private List<Node> MergePaths(List<Node> oldPath, List<Node> newPath, int progress)
    {
        List<Node> mergedPath = new List<Node>();
        
        // Mantener nodos ya recorridos
        for (int i = 0; i <= progress; i++)
            mergedPath.Add(oldPath[i]);
        
        // Añadir nuevos nodos no duplicados
        foreach (Node n in newPath)
            if (!mergedPath.Contains(n)) mergedPath.Add(n);
        
        return mergedPath;
    }

    private bool IsSameDestination(List<Node> newPath)
    {
        return _currentPath.Count > 0 && 
               newPath.Count > 0 && 
               _currentPath[_currentPath.Count - 1] == newPath[newPath.Count - 1];
    }

    private void StartMovement()
    {
        if (!ValidatePath()) return;
        MoveToNextNode(FindCurrentPathProgress());
    }

    private void MoveToNextNode(int startIndex)
    {
        if (startIndex >= _currentPath.Count)
        {
            _fsm.SetState(PlayerState.Idle);
            return;
        }
        //List<GameObject> nearbyObjects = SpatialGrid.Instance.GetNearbyObjects(_owner.transform.position, 2);
    
        /*foreach(GameObject obj in nearbyObjects)
        {
            if(obj.CompareTag("Enemy"))
            {
                Debug.Log($"Enemigo cercano detectado: {obj.name}");
            }
        }*/

        Node targetNode = _currentPath[startIndex];
        Vector3 targetPosition = new Vector3(
            targetNode.WorldPosition.x,
            _owner.transform.position.y,
            targetNode.WorldPosition.z
        );

        _currentTween = _owner.transform.DOMove(targetPosition, CalculateDuration(targetPosition))
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                _lastWorldPosition = targetPosition;
                MoveToNextNode(startIndex + 1);
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

    //... Execute y FixedExecute permanecen igual
}