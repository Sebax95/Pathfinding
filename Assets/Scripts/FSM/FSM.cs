using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FSM<T>
{
    private T _owner;
    private Dictionary<Enum, State<T>> _states;
    private State<T> _currentState;

    public FSM(T owner)
    {
        _owner = owner;
        _states = new();
    }

    public void AddState(Enum stateName, State<T> state) => _states.Add(stateName, state);
    

    public void SetState(Enum stateName)
    {
        if (_currentState != null)
            _currentState.Exit();
        _currentState = _states[stateName];
        _currentState.Enter();
    }

    public Enum GetState()
    {
        Enum aux = default;
        foreach (var item in _states.Where(item => item.Value == _currentState))
            aux = item.Key;
        
        return aux;
    }

    public void Update() => _currentState.Execute();
    public void FixUpdate() => _currentState.FixedExecute();
    
}