using UnityEngine;

public class DeathHandler : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject DeathUIPanel; // 新增：死亡时要弹出的 UI

    private HealthController _health;
    private Player _player;
    private Animator _animator;
    private Rigidbody _rb;

    public bool IsDead { get; private set; } = false;

    void Start()
    {
        _health = GetComponent<HealthController>();
        _player = GetComponent<Player>();
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!IsDead && _health != null && _health.CurrentHealth <= 0)
        {
            ExecuteDeath();
        }
    }

    private void ExecuteDeath()
    {
        IsDead = true;

        if (_player != null) _player.enabled = false;
        if (_rb != null) _rb.velocity = new Vector3(0, _rb.velocity.y, 0);

        if (_animator != null)
        {
            _animator.SetTrigger("Die");
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("IsMoving", false);
        }

        // 新增：激活死亡 UI 面板
        if (DeathUIPanel != null) DeathUIPanel.SetActive(true);

        Debug.Log("【系统】死亡逻辑已由 DeathHandler 接管：玩家已阵亡");
    }

    public void Revive()
    {
        IsDead = false;
        Debug.Log("<color=cyan>【DeathHandler】状态已重置：IsDead = false，允许再次死亡！</color>");
    }
}