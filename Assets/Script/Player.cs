using UnityEngine;
using Cinemachine;

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
    private Vector3 currentMoveDirection;
    private StaminaController _stamina;
    private HealthController _health;

    // 状态机相关
    public PlayerStateMachine stateMachine;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerLightAttackState lightAttackState; // 改为轻功
    public PlayerHeavyAttackState heavyAttackState; // 改为重功
    public PlayerGuardState guardState;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        _health = GetComponent<HealthController>();
        _stamina = GetComponent<StaminaController>();

        // 初始化状态机
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        lightAttackState = new PlayerLightAttackState(this);
        heavyAttackState = new PlayerHeavyAttackState(this);
        guardState = new PlayerGuardState(this);

        stateMachine.Initialize(idleState);

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
        // 1. 攻击输入检测 (高优先级)
        if (Input.GetMouseButtonDown(0)) // 左键：轻攻击
        {
            stateMachine.TransitionTo(lightAttackState);
        }
        else if (Input.GetMouseButtonDown(1)) // 右键：重攻击
        {
            stateMachine.TransitionTo(heavyAttackState);
        }
        // 格挡
        else if (Input.GetKeyDown(KeyCode.R)) 
        {
            stateMachine.TransitionTo(guardState);
        }

// 2. 状态机逻辑执行
        stateMachine.CurrentState.LogicUpdate();

// 3. 测试代码：按 K 键扣血
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

// --- 以下是供状态类调用的公共工具方法 ---

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

public void UpdateAnimator()
{
if (animator == null) return;

Vector3 localMove = transform.InverseTransformDirection(currentMoveDirection);
animator.SetFloat("MoveX", localMove.x);
animator.SetFloat("MoveZ", localMove.z);
animator.SetFloat("Speed", currentMoveDirection.magnitude);
animator.SetBool("IsMoving", currentMoveDirection.sqrMagnitude > 0.01f);
}

public bool IsGrounded()
{
return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.15f);
}

    public float GetCurrentSpeed() => new Vector2(rb.velocity.x, rb.velocity.z).magnitude;

    // 重点检查这里：IsMoving 必须有完整的大括号包裹
    public bool IsMoving()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return new Vector2(h, v).sqrMagnitude > 0.01f;
    }

}