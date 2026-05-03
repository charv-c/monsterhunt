// PlayerLightAttackState.cs
using UnityEngine;
public class PlayerLightAttackState : IState
{
    private Player p;
    private float timer;
    public PlayerLightAttackState(Player player) => p = player;
    public void Enter()
    {
        // 在 Enter 的第一行添加
        Debug.Log("<color=yellow>【战斗系统】</color> 触发：轻攻击！消耗耐力。");
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

public class PlayerHeavyAttackState : IState
{
    private Player p;
    private float timer;
    public PlayerHeavyAttackState(Player player) => p = player;
    public void Enter()
    {
        // 在 Enter 的第一行添加
        Debug.Log("<color=red>【战斗系统】</color> 触发：重攻击！强力打击。");
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