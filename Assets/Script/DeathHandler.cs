using UnityEngine;

public class DeathHandler : MonoBehaviour
{
    private HealthController _health;
    private Player _player;
    private Animator _animator;
    private Rigidbody _rb;
    private bool _isDead = false;

    void Start()
    {
        // 自动获取同一物体上的所有组件
        _health = GetComponent<HealthController>();
        _player = GetComponent<Player>();
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 核心监控：如果没死且血量归零
        if (!_isDead && _health != null && _health.CurrentHealth <= 0)
        {
            ExecuteDeath();
        }
    }

    private void ExecuteDeath()
    {
        _isDead = true;

        // 1. 禁用 Player 脚本，玩家立刻无法移动、跳跃、转视角
        if (_player != null) _player.enabled = false;

        // 2. 物理惯性清理，防止尸体滑行
        if (_rb != null) _rb.velocity = new Vector3(0, _rb.velocity.y, 0);

        // 3. 动画处理
        if (_animator != null)
        {
            _animator.SetTrigger("Die");
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("IsMoving", false);
        }

        Debug.Log("【系统】死亡逻辑已由 DeathHandler 接管：玩家已阵亡");
    }
}