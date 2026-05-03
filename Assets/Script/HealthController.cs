using UnityEngine;

public class HealthController : MonoBehaviour
{
    [Header("HP 设置")]
    public float MaxHealth = 100f;
    public float RecoveryRate = 5f;        // 红血回复速度 (5点/秒)
    public float RecoveryDelay = 2.0f;     // 受伤后多久开始回血

    [Header("伤害比例")]
    [Tooltip("伤害中转化为红血的比例（如 30点伤害里 20点是红血，则比例为 0.66）")]
    public float RedHealthRatio = 0.666f;

    public float CurrentHealth { get; private set; } // 当前实血 (绿条)
    public float RedHealth { get; private set; }     // 当前红血 (可恢复部分)

    private float _delayTimer;

    // 新增：无敌状态（用于冲刺期间短暂免伤）
    private bool _isInvincible;
    public bool IsInvincible => _isInvincible;

    void Start()
    {
        CurrentHealth = MaxHealth;
        RedHealth = 0f;
    }

    void Update()
    {
        HandleRecovery();
    }

    private void HandleRecovery()
    {
        if (RedHealth <= 0) return;

        // 计时器逻辑
        if (_delayTimer > 0)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }

        // 开始回血：把红血转为实血
        float recoverThisFrame = RecoveryRate * Time.deltaTime;
        recoverThisFrame = Mathf.Min(recoverThisFrame, RedHealth); // 别回多了

        CurrentHealth += recoverThisFrame;
        RedHealth -= recoverThisFrame;
    }

    /// <summary>
    /// 设置/取消无敌（供 Player 在冲刺开始/结束时调用）
    /// </summary>
    /// <param name="invincible">是否进入无敌</param>
    public void SetInvincible(bool invincible)
    {
        _isInvincible = invincible;
    }

    public void TakeDamage(float totalDamage)
    {
        // 如果处于无敌状态，不受伤
        if (_isInvincible) return;

        // 1. 核心机制：再次受伤，之前的红血直接清零
        RedHealth = 0f;

        // 2. 计算切分：30伤害 -> 20红血，10直接消失
        float redPart = totalDamage * RedHealthRatio;

        // 3. 实血扣除全部
        CurrentHealth -= totalDamage;

        // 4. 设置新的红血量
        RedHealth = redPart;

        // 5. 重置回复延迟
        _delayTimer = RecoveryDelay;

  
    }
    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        RedHealth = 0f;
        SetInvincible(false); // 【加这行】防止死在闪避帧里导致永久无敌
    }
}