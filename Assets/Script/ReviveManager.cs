using UnityEngine;

public class ReviveManager : MonoBehaviour
{
    [Header("UI 设置")]
    public GameObject DeathUIPanel; // 拖入你的死亡界面 Panel
    [Header("目标玩家")]
    public GameObject TargetPlayer; // 【加这行】明确告诉它主角是谁
    private Vector3 _spawnPoint;
    private Quaternion _spawnRotation;

    private HealthController _health;
    private StaminaController _stamina;
    private Player _player;
    private DeathHandler _deathHandler;
    private Animator _animator;

    void Start()
    {
        // 记录游戏刚开始时的初始位置和旋转
        _spawnPoint = transform.position;
        _spawnRotation = transform.rotation;

        // 获取引用
        _health = TargetPlayer.GetComponent<HealthController>();
        _stamina = TargetPlayer.GetComponent<StaminaController>();
        _player = TargetPlayer.GetComponent<Player>();
        _deathHandler = TargetPlayer.GetComponent<DeathHandler>();
        _animator = TargetPlayer.GetComponent<Animator>();
    }

    // 这个方法绑定到 UI 按钮的 OnClick 事件上
    public void PerformRevive()
    {
        if (TargetPlayer == null) return;

        // 1. 坐标重置
        TargetPlayer.transform.position = _spawnPoint; // 【修改】改为 TargetPlayer
        TargetPlayer.transform.rotation = _spawnRotation;

        // 2. 数值重置
        if (_health != null) _health.ResetHealth();

        // 3. 脚本与逻辑重置
        if (_player != null) _player.enabled = true;
        if (_deathHandler != null) _deathHandler.Revive();

        // 4. 动画状态重置
        if (_animator != null)
        {
            _animator.Play("Locomotion", 0, 0f); // 【修改】名字改为 Locomotion
            _animator.ResetTrigger("Die");       // 【新增】清除死亡触发器残留
        }

        // 5. 隐藏死亡 UI
        if (DeathUIPanel != null) DeathUIPanel.SetActive(false);

        Debug.Log("【系统】玩家已回到初始位置并复活");
    }
}