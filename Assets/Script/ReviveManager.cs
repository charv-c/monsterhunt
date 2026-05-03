using UnityEngine;

public class ReviveManager : MonoBehaviour
{
    [Header("UI 设置")]
    public GameObject DeathUIPanel; // 拖入你的死亡界面 Panel

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
        _health = GetComponent<HealthController>();
        _stamina = GetComponent<StaminaController>();
        _player = GetComponent<Player>();
        _deathHandler = GetComponent<DeathHandler>();
        _animator = GetComponent<Animator>();
    }

    // 这个方法绑定到 UI 按钮的 OnClick 事件上
    public void PerformRevive()
    {
        // 1. 坐标重置
        transform.position = _spawnPoint;
        transform.rotation = _spawnRotation;

        // 2. 数值重置
        if (_health != null) _health.ResetHealth();
        // 如果 StaminaController 没有 Reset，可以直接暴力设置：
        // _stamina.TryConsume(-100); 

        // 3. 脚本与逻辑重置
        if (_player != null) _player.enabled = true;
        if (_deathHandler != null) _deathHandler.Revive();

        // 4. 动画状态重置
        if (_animator != null)
        {
            // 强制回到 Idle 状态，防止卡在死亡动画里
            _animator.Play("Movement", 0, 0f);
        }

        // 5. 隐藏死亡 UI
        if (DeathUIPanel != null) DeathUIPanel.SetActive(false);

        Debug.Log("【系统】玩家已回到初始位置并复活");
    }
}