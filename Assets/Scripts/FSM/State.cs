public abstract class State<T>
{
    public abstract void Enter();
    public abstract void Execute();
    public abstract void FixedExecute();
    public abstract void Exit();

    protected T _owner;
    protected FSM<T> _fsm;

    protected State(T owner, FSM<T> fsm)
    {
        _owner = owner;
        _fsm = fsm;
    }
}