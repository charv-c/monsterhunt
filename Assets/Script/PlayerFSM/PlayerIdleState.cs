using UnityEngine;
public class PlayerIdleState : IState
{
    private Player p;
    public PlayerIdleState(Player player) => p = player;
    public void Enter() => p.rb.velocity = new Vector3(0, p.rb.velocity.y, 0); // 停下水平移动
    public void LogicUpdate() { if (p.IsMoving()) p.stateMachine.TransitionTo(p.moveState); }
    public void PhysicsUpdate() => p.UpdateAnimator();
    public void Exit() { }
}