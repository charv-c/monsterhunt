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

    [Header("跳跃设置")]
    public float jumpForce = 8f;

    [Header("摄像机引用")]
    public CinemachineFreeLook freeLookCamera;

    [Header("动画")]
    public Animator animator;
    public Rigidbody rb;

    [Header("冲刺设置")]
    public float dashDistance = 6f;
    public float dashDuration = 0.2f;
    public float dashCost = 30f;

    // --- 内部状态 ---
    private Vector3 currentMoveDirection;
    private bool jumpRequested;
    private StaminaController _stamina;
    private HealthController _health;

    // 冲刺状态（供状态机使用）
    [HideInInspector] public bool isDashing;
    [HideInInspector] public Vector3 dashDirection;
    [HideInInspector] public float dashSpeed;

    // --- 状态机 ---
    public PlayerStateMachine stateMachine;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerLightAttackState lightAttackState;
    public PlayerHeavyAttackState heavyAttackState;
    public PlayerGuardState guardState;
    public PlayerDashState dashState;
    public PlayerJumpState jumpState;

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
        dashState = new PlayerDashState(this);
        jumpState = new PlayerJumpState(this);

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
    }

    void Update()
    {
        // 1. 输入检测（优先级：攻击 > 格挡 > 冲刺 > 跳跃）
        if (Input.GetMouseButtonDown(0))
        {
            stateMachine.TransitionTo(lightAttackState);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            stateMachine.TransitionTo(heavyAttackState);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            stateMachine.TransitionTo(guardState);
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            // 只有在非冲刺状态下才允许触发冲刺
            if (TryStartDash())
                stateMachine.TransitionTo(dashState);
        }
        else if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            jumpRequested = true;
            stateMachine.TransitionTo(jumpState);
        }

        // 2. 鼠标控制旋转（全局可用，不受状态限制）
        HandleRotation();

        // 3. 状态机逻辑更新
        stateMachine.CurrentState.LogicUpdate();

        // 4. 测试代码：按 K 键扣血
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_health != null) _health.TakeDamage(30f);
        }
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

    // --- 跳跃逻辑 ---
    public void HandleJump()
    {
        if (jumpRequested && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    public void ResetJumpRequest() => jumpRequested = false;

    // --- 冲刺逻辑 ---
    public bool TryStartDash()
    {
        if (isDashing) return false;

        bool staminaOk = _stamina == null || _stamina.TryConsume(dashCost);
        if (!staminaOk) return false;

        StartCoroutine(DashCoroutine());
        return true;
    }

    private IEnumerator DashCoroutine()
    {
        dashDirection = (currentMoveDirection.sqrMagnitude > 0.01f)
            ? currentMoveDirection
            : transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();

        dashSpeed = dashDistance / Mathf.Max(0.0001f, dashDuration);
        isDashing = true;

        if (_health != null)
            _health.SetInvincible(true);

        if (animator != null)
            animator.SetTrigger("Dash");

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        if (_health != null)
            _health.SetInvincible(false);

        // 冲刺结束后自动回到Idle或Move状态
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
        animator.SetBool("IsDashing", isDashing);
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