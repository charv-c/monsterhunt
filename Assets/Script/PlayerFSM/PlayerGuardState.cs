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
        Debug.Log("<color=cyan>【战斗系统】退出格挡：防御已结束，耐力恢复正常</color>");
    }
}