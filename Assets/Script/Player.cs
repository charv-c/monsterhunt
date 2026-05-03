using UnityEngine;
using Cinemachine;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;

    [Header("旋转设置")]
    [Tooltip("鼠标X轴灵敏度")]
    public float mouseSensitivityX = 2f;

    [Header("摄像机引用")]
    public CinemachineFreeLook freeLookCamera;

    [Header("动画")]
    public Animator animator;
    public Rigidbody rb;

    [Header("闪避设置 (Space键)")]
    public float dodgeDistance = 6f;     // 闪避距离
    public float dodgeDuration = 0.2f;   // 闪避持续时间
    public float dodgeCooldown = 0.8f;   // 闪避冷却时间
    public float dodgeCost = 20f;        // 闪避消耗耐力

    // --- 内部状态 ---
    [HideInInspector] public bool isDodging = false;
    private float lastDodgeTime = -1f;
    [HideInInspector] public Vector3 dodgeDirection;
    [HideInInspector] public float dodgeSpeed;

    private Vector3 currentMoveDirection;
    private bool jumpRequested;
    private StaminaController _stamina;
    private HealthController _health;
    // --- 状态机 ---
    public PlayerStateMachine stateMachine;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerLightAttackState lightAttackState;
    public PlayerHeavyAttackState heavyAttackState;
    public PlayerGuardState guardState;
    public PlayerDodgeState dodgeState;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        _health = GetComponent<HealthController>();
        _stamina = GetComponent<StaminaController>();

        // 初始化状态机
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        lightAttackState = new PlayerLightAttackState(this);
        heavyAttackState = new PlayerHeavyAttackState(this);
        guardState = new PlayerGuardState(this);
        dodgeState = new PlayerDodgeState(this);
        stateMachine.Initialize(idleState);

        // 自动查找摄像机
        if (freeLookCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            freeLookCamera = FindAnyObjectByType<CinemachineFreeLook>();
#else
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
#endif
        }

        // 启动时刷新一次相机旋转，使相机面向玩家当前朝向
        RefreshCameraRotation();
    }

    void Update()
    {
        // 1. 输入检测
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("<color=yellow>【战斗】触发：轻攻击</color>");
            stateMachine.TransitionTo(lightAttackState);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("<color=red>【战斗】触发：重攻击</color>");
            stateMachine.TransitionTo(heavyAttackState); // 这里帮你补了分号
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // 如果当前已经是防御状态，就切回闲置；否则进入防御
            if (stateMachine.CurrentState == guardState)
            {
                stateMachine.TransitionTo(idleState);
            }
            else
            {
                stateMachine.TransitionTo(guardState);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space) && !isDodging && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            stateMachine.TransitionTo(dodgeState);
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift) && !isDodging)
        {
            // placeholder for dash if you later add it
        }
        else if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            jumpRequested = true;
            stateMachine.TransitionTo(idleState); // 如果没有 jumpState，回到 idle（若有 jumpState，可改为 jumpState）
        }

        // 2. 旋转逻辑
        HandleRotation();

        // 3. 状态机逻辑更新
        stateMachine.CurrentState.LogicUpdate();

        // 4. 测试代码：按 K 键扣血
        if (Input.GetKeyDown(KeyCode.K) && _health != null)
        {
            _health.TakeDamage(30f);
        }

        // 中键：兼容性处理 — 如果需要在 Player 层直接处理中键，可以在这里响应（可选）
        // 实际旋转以接收广播为主（PositionBroadcaster.SendMessage -> OnReceivePositionBroadcast）
    } 

    void FixedUpdate()
    {
        if (stateMachine.CurrentState != null)
            stateMachine.CurrentState.PhysicsUpdate();
    }

    // --- 旋转控制 ---
    public void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        if (Mathf.Abs(mouseX) > 0.001f)
        {
            transform.Rotate(0f, mouseX, 0f);
            // 同步 Cinemachine FreeLook 的横轴，使摄像机跟随玩家朝向
            if (freeLookCamera != null)
            {
                try
                {
                    freeLookCamera.m_XAxis.Value = transform.eulerAngles.y;
                }
                catch
                {
                    // 兼容性容错：部分 Cinemachine 版本可能访问方式不同
                }
            }
        }
    }

    /// <summary>
    /// 刷新相机的旋转，使其面向玩家当前水平朝向（Start 时调用）
    /// </summary>
    private void RefreshCameraRotation()
    {
        if (freeLookCamera == null) return;

        try
        {
            // 将 FreeLook 横轴设置为玩家 Y 角度
            freeLookCamera.m_XAxis.Value = transform.eulerAngles.y;
            // 强制摄像机立即应用当前位置/旋转
            freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation);
        }
        catch
        {
            // 若 Cinemachine API 不兼容，安全忽略
        }
    }

    /// <summary>
    /// 接收 PositionBroadcaster 的广播（SendMessage 调用）
    /// 玩家水平朝向 worldPosition 的方向，摄像机同步移动到该朝向
    /// </summary>
    public void OnReceivePositionBroadcast(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        // 立即朝向目标水平方向（可改为平滑插值）
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f);

        // 同步 Cinemachine FreeLook 的横向轴，让相机看向玩家朝向
        if (freeLookCamera != null)
        {
            try
            {
                freeLookCamera.m_XAxis.Value = transform.eulerAngles.y;
                freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation);
            }
            catch
            {
                // 容错：忽略不支持的 API 调用
            }
        }
    }

    // --- 移动处理（供状态机调用）---
    public Vector3 HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        bool canSprint = Input.GetKey(KeyCode.LeftShift) && _stamina != null && _stamina.CurrentStamina > 0;
        float currentSpeed = moveSpeed * (canSprint ? sprintMultiplier : 1f);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            if (canSprint) _stamina.ConsumeContinuous(_stamina.SprintCost);
            currentMoveDirection = moveDirection;
            return moveDirection * currentSpeed;
        }
        else
        {
            currentMoveDirection = Vector3.zero;
            return Vector3.zero;
        }
    }
    // --- 冲刺/闪避逻辑 ---
    public bool TryStartDodge()
    {
        if (isDodging) return false;

        bool staminaOk = _stamina == null || _stamina.TryConsume(dodgeCost);
        if (!staminaOk) return false;

        StartCoroutine(DodgeCoroutine());
        return true;
    }

    private IEnumerator DodgeCoroutine()
    {
        dodgeDirection = (currentMoveDirection.sqrMagnitude > 0.01f)
            ? currentMoveDirection
            : transform.forward; // 原地闪避默认向前（也可以改 -transform.forward 向后）

        dodgeDirection.y = 0f;
        dodgeDirection.Normalize();

        dodgeSpeed = dodgeDistance / Mathf.Max(0.0001f, dodgeDuration);
        isDodging = true;
        lastDodgeTime = Time.time;

        if (_health != null) _health.SetInvincible(true);

        // 注意：这里把触发器名字改成了 "Dodge"，请确保你的 Animator 里参数名也改成了 Dodge
        if (animator != null) animator.SetTrigger("Dodge");

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            // 通过修改 rb.velocity 实现物理位移
            rb.velocity = new Vector3(dodgeDirection.x * dodgeSpeed, rb.velocity.y, dodgeDirection.z * dodgeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
        if (_health != null) _health.SetInvincible(false);

        // 闪避结束后自动回到Idle或Move状态
        if (IsMoving())
            stateMachine.TransitionTo(moveState);
        else
            stateMachine.TransitionTo(idleState);
    }

    // --- 动画更新 ---
    public void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 localMove = transform.InverseTransformDirection(currentMoveDirection);
        animator.SetFloat("MoveX", localMove.x);
        animator.SetFloat("MoveZ", localMove.z);
        animator.SetFloat("Speed", currentMoveDirection.magnitude);
        animator.SetBool("IsMoving", currentMoveDirection.sqrMagnitude > 0.01f);
        animator.SetBool("IsDodging", isDodging);
    }

    // --- 工具方法 ---
    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.15f);
    }

    public float GetCurrentSpeed() => new Vector2(rb.velocity.x, rb.velocity.z).magnitude;

    public bool IsMoving()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return new Vector2(h, v).sqrMagnitude > 0.01f;
    }

    public bool IsGroundedState() => IsGrounded();
}