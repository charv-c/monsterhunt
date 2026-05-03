using UnityEngine;
public class PlayerGuardState : IState
{
    private Player player;

    public PlayerGuardState(Player player) => this.player = player;

    public void Enter()
    {
        // 1. 播放格挡动画
        if (player.animator != null)
        {
            player.animator.SetBool("IsGuarding", true);
        }

        // 2. 物理修正：进入格挡时立刻停止水平位移，但保留垂直速度（重力）
        player.GetComponent<Rigidbody>().velocity = new Vector3(0, player.GetComponent<Rigidbody>().velocity.y, 0);

        Debug.Log("进入格挡态：R键防御中");
    }

    public void LogicUpdate()
    {
        // 3. 检测 R 键是否松开
        // 注意：Input.GetKey 是只要按着就返回 true，取反则表示松开了按键
        if (!Input.GetKey(KeyCode.R))
        {
            // 4. 智能切换：根据是否有位移输入决定回哪个状态
            if (player.IsMoving())
            {
                player.stateMachine.TransitionTo(player.moveState);
            }
            else
            {
                player.stateMachine.TransitionTo(player.idleState);
            }
        }
    }

    public void PhysicsUpdate()
    {
        // 格挡期间通常不需要处理移动物理
    }

    public void Exit()
    {
        // 5. 退出状态时必须关闭动画参数，否则角色会一直保持格挡动作
        if (player.animator != null)
        {
            player.animator.SetBool("IsGuarding", false);
        }
    }
}