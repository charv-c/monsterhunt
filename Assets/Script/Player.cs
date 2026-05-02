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

    private Vector3 currentMoveDirection;
    private Rigidbody rb;
    private bool jumpRequested;
    private StaminaController _stamina;
    private HealthController _health;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (animator == null)
            animator = GetComponent<Animator>();
        // 新增：获取耐力控制器组件
        _health = GetComponent<HealthController>(); // 新增：获取血量组件
        _stamina = GetComponent<StaminaController>();
        if (freeLookCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            freeLookCamera = FindAnyObjectByType<CinemachineFreeLook>();
#else
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
#endif
        }

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 鼠标控制玩家旋转
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        if (Mathf.Abs(mouseX) > 0.001f)
        {
            transform.Rotate(0f, mouseX, 0f);
        }

        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }
    // 新增：测试代码，按 K 键扣 30 点血
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_health != null) _health.TakeDamage(30f);
        }
void FixedUpdate()
    {
        // 处理移动（基于玩家自身朝向）
        Vector3 horizontalVelocity = HandleMovement();

        // 保持当前垂直速度，只覆盖水平速度
        Vector3 newVelocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
        rb.velocity = newVelocity;

        // 跳跃：施加瞬时力
        if (jumpRequested && IsGrounded())
        {
            // 只有在 StaminaController 允许消耗时才执行跳跃
            if (_stamina != null && _stamina.TryConsume(_stamina.JumpCost))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            jumpRequested = false;
        }

        UpdateAnimator();
    }

    Vector3 HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 基于玩家自身朝向计算移动方向
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        // 判断逻辑：按下 Shift 且耐力大于 0
        bool canSprint = Input.GetKey(KeyCode.LeftShift) && _stamina != null && _stamina.CurrentStamina > 0;
        float currentSpeed = moveSpeed * (canSprint ? sprintMultiplier : 1f);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // 如果正在冲刺，持续扣除耐力
            if (canSprint)
            {
                _stamina.ConsumeContinuous(_stamina.SprintCost);
            }

            currentMoveDirection = moveDirection;
            return moveDirection * currentSpeed;
        }
        else
        {
            // 路径 2：必须处理没有移动输入的情况
            currentMoveDirection = Vector3.zero;
            return Vector3.zero;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.15f);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 localMove = transform.InverseTransformDirection(currentMoveDirection);
        animator.SetFloat("MoveX", localMove.x);
        animator.SetFloat("MoveZ", localMove.z);
        animator.SetFloat("Speed", currentMoveDirection.magnitude);
        animator.SetBool("IsMoving", currentMoveDirection.sqrMagnitude > 0.01f);
    }

    public float GetCurrentSpeed() => new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
    public bool IsMoving() => currentMoveDirection.sqrMagnitude > 0.01f;
    public bool IsGroundedState() => IsGrounded();
}