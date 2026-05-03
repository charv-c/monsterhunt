// IState.cs
public interface IState
{
    void Enter();         // 进入状态时执行一次
    void LogicUpdate();   // 放在 Update 中执行
    void PhysicsUpdate(); // 放在 FixedUpdate 中执行
    void Exit();          // 退出状态时执行一次
}

// PlayerStateMachine.cs
public class PlayerStateMachine
{
    public IState CurrentState { get; private set; }

    // 初始化状态
    public void Initialize(IState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // 切换状态
    public void TransitionTo(IState nextState)
    {
        CurrentState.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }
}