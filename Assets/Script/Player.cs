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

    [Header("索敌与转向")]
    public float targetTurnDuration = 0.1f; // 中键索敌后旋转所用时间
    public float maxTargetSearchDistance = 50f; // 搜索敌人最大距离
    private Coroutine _turnCoroutine;

    // --- 内部状态 ---
    [HideInInspector] public bool isDodging = false;
    private float lastDodgeTime = -1f;
    [HideInInspector] public Vector3 dodgeDirection;
    [HideInInspector] public float dodgeSpeed;

    private Vector3 currentMoveDirection;
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
    }

    void Update()
    {
        // 中键索敌：找到最近敌人并在 targetTurnDuration 内朝向敌人，摄像机同步旋转
        if (Input.GetMouseButtonDown(2))
        {
            Transform enemy = FindNearestEnemy(maxTargetSearchDistance);
            if (enemy != null)
            {
                if (_turnCoroutine != null) StopCoroutine(_turnCoroutine);
                _turnCoroutine = StartCoroutine(TurnToEnemyCoroutine(enemy, targetTurnDuration));
            }
            else
            {
                Debug.Log("<color=cyan>未找到合适的敌人目标</color>");
            }
        }

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

        // 2. 旋转逻辑
        HandleRotation();

        // 3. 状态机逻辑更新
        stateMachine.CurrentState.LogicUpdate();

        // 4. 测试代码：按 K 键扣血
        if (Input.GetKeyDown(KeyCode.K) && _health != null)
        {
            _health.TakeDamage(30f);
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

    // --- 中键索敌并平滑转向协程 ---
    private IEnumerator TurnToEnemyCoroutine(Transform enemy, float duration)
    {
        if (enemy == null || duration <= 0f)
        {
            yield break;
        }

        // 目标方向（仅水平）
        Vector3 dir = enemy.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) yield break;

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // 摄像机轴角度：尝试使用 freeLookCamera 的 m_XAxis（若存在）
        bool hasFreeLook = freeLookCamera != null;
        float startCamYaw = 0f;
        float targetCamYaw = 0f;
        if (hasFreeLook)
        {
            // 当前 yaw（度）
            startCamYaw = freeLookCamera.m_XAxis.Value;
            // 目标 yaw 取目标朝向的 y 欧拉角
            targetCamYaw = targetRot.eulerAngles.y;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            // 使用球形插值平滑转向
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (hasFreeLook)
            {
                // 插值摄像机 yaw，使用 LerpAngle 保证角度环绕正确
                freeLookCamera.m_XAxis.Value = Mathf.LerpAngle(startCamYaw, targetCamYaw, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保结尾对齐
        transform.rotation = targetRot;
        if (hasFreeLook)
            freeLookCamera.m_XAxis.Value = targetCamYaw;

        _turnCoroutine = null;
    }

    // 在场景中查找最近的敌人（优先使用 Tag "Enemy"，找不到则查找带有 EnemyAI 组件的对象）
    private Transform FindNearestEnemy(float maxDistance)
    {
        Transform best = null;
        float bestSqr = maxDistance * maxDistance;

        // 1. 先按 Tag 查找
        GameObject[] byTag = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < byTag.Length; i++)
        {
            Transform t = byTag[i].transform;
            float sqr = (t.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        if (best != null) return best;

        // 2. 回退：按 EnemyAI 组件查找
        EnemyAI[] ais = FindObjectsOfType<EnemyAI>();
        for (int i = 0; i < ais.Length; i++)
        {
            Transform t = ais[i].transform;
            float sqr = (t.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        return best;
    }

    // --- 移动处理（供状态机调用）---
    // 修改：以 freeLookCamera 的 Y 轴朝向作为前进基准（若 freeLookCamera 为空则回退到玩家朝向）
    public Vector3 HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 使用摄像机的朝向作为移动基准（仅考虑 Y 轴旋转）
        Transform referenceTransform = freeLookCamera != null ? freeLookCamera.transform : transform;

        Vector3 forward = referenceTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = referenceTransform.right;
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
    // --- 冲刺逻辑 ---
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