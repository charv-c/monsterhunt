using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float MaxStamina = 100f;
    public float RegenRate = 15f;          // 每秒恢复量
    public float SprintCost = 20f;         // 每秒冲刺消耗
    public float JumpCost = 15f;           // 单次跳跃消耗
    public float RegenDelay = 1.5f;        // 停止消耗后多久开始恢复

    [Header("State Machine Sync")]
    public Animator PlayerAnimator;

    public float CurrentStamina { get; private set; }
    private float _delayTimer;

    void Start()
    {
        CurrentStamina = MaxStamina;
        if (PlayerAnimator == null) PlayerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleRegeneration();
        SyncAnimator();
    }

    private void HandleRegeneration()
    {
        if (_delayTimer > 0)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }

        if (CurrentStamina < MaxStamina)
        {
            CurrentStamina += RegenRate * Time.deltaTime;
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        }
    }

    // 尝试消耗耐力（用于瞬时动作如跳跃）
    public bool TryConsume(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            _delayTimer = RegenDelay;
            return true;
        }
        return false;
    }

    // 持续消耗耐力（用于冲刺）
    public void ConsumeContinuous(float amountPerSecond)
    {
        CurrentStamina -= amountPerSecond * Time.deltaTime;
        CurrentStamina = Mathf.Max(0, CurrentStamina);
        _delayTimer = RegenDelay;
    }

    private void SyncAnimator()
    {
        if (PlayerAnimator == null) return;

        // 暴露给状态机的变量
        PlayerAnimator.SetFloat("StaminaNormalized", CurrentStamina / MaxStamina);
        PlayerAnimator.SetBool("HasStamina", CurrentStamina > 0.1f);
    }
}