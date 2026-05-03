using UnityEngine;
public class PlayerMoveState : IState
{
    private Player p;
    public PlayerMoveState(Player player) => p = player;
    public void Enter() { }
    public void LogicUpdate() { if (!p.IsMoving()) p.stateMachine.TransitionTo(p.idleState); }
    public void PhysicsUpdate()
    {
        Vector3 moveVel = p.HandleMovement();
        p.rb.velocity = new Vector3(moveVel.x, p.rb.velocity.y, moveVel.z);
        p.UpdateAnimator();
    }
    public void Exit() { }
}