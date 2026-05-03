// PlayerLightAttackState.cs
using UnityEngine;
public class PlayerLightAttackState : IState
{
    private Player p;
    private float timer;
    public PlayerLightAttackState(Player player) => p = player;
    public void Enter()
    {
        timer = 0.5f; // 轻功时长
        p.animator.CrossFade("LightAttack", 0.1f);
        p.rb.velocity = Vector3.zero; // 攻击时通常锁定身位
    }
    public void LogicUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0) p.stateMachine.TransitionTo(p.idleState);
    }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

// PlayerHeavyAttackState.cs
public class PlayerHeavyAttackState : IState
{
    private Player p;
    private float timer;
    public PlayerHeavyAttackState(Player player) => p = player;
    public void Enter()
    {
        timer = 1.0f; // 重功动作更慢
        p.animator.CrossFade("HeavyAttack", 0.15f);
        p.rb.velocity = Vector3.zero;
        // 这里可以加一行：if(p._stamina != null) p._stamina.Consume(20f);
    }
    public void LogicUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0) p.stateMachine.TransitionTo(p.idleState);
    }
    public void PhysicsUpdate() { }
    public void Exit() { }
}