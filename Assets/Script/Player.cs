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

    [Header("朝向敌人平滑设置")]
    [Tooltip("按下中键时朝向敌人旋转时长（秒）")]
    public float faceTargetDuration = 0.1f;

    // --- 内部状态 ---
    [HideInInspector] public bool isDodging = false;
    private float lastDodgeTime = -1f;
    [HideInInspector] public Vector3 dodgeDirection;
    [HideInInspector] public float dodgeSpeed;

    private Vector3 currentMoveDirection;
    private bool jumpRequested;
    private StaminaController _stamina;
    private HealthController _health;

    // 平滑朝向控制
    private bool isFacingSmooth = false;      // 正在平滑朝向目标期间为 true（用于忽略鼠标水平输入）
    private Coroutine faceCoroutine = null;

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
            if (stateMachine.CurrentState == guardState)
                stateMachine.TransitionTo(idleState);
            else
                stateMachine.TransitionTo(guardState);
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

        // 中键按下：面向敌人并同步摄像机
        if (Input.GetMouseButtonDown(2))
        {
            TryFaceEnemyUnderCursorSmooth();
        }

        // 2. 旋转逻辑（鼠标）
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
            SyncCameraYawToPlayer();
        }
    }

    // --- 在 Start 时或需要时刷新相机，使其朝向玩家朝向 ---
    private void RefreshCameraRotation()
    {
        SyncCameraYawToPlayer();
        // 强制刷新位置/旋转（容错调用，若方法存在）
        if (freeLookCamera != null)
        {
            try
            {
                freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation);
            }
            catch { }
        }
    }

    // 将 Cinemachine FreeLook 的横向轴（m_XAxis）与玩家 Y 角度同步
    private void SyncCameraYawToPlayer()
    {
        if (freeLookCamera == null) return;
        try
        {
            freeLookCamera.m_XAxis.Value = transform.eulerAngles.y;
        }
        catch
        {
            // 若 Cinemachine 版本 API 不同则忽略失败
        }
    }

    // --- 鼠标中键：平滑面向目标（优先光标下的敌人，否则最近敌人） ---
    private void TryFaceEnemyUnderCursorSmooth()
    {
        // 首先做射线检测
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Transform enemy = null;

        if (Physics.Raycast(ray, out hit, 200f))
        {
            enemy = ResolveEnemyTransform(hit.transform);
        }

        // 如果射线没有命中敌人，尝试找最近的带 EnemyAI 的对象
        if (enemy == null)
        {
            EnemyAI[] ais = GameObject.FindObjectsOfType<EnemyAI>();
            float bestDist = float.MaxValue;
            foreach (var a in ais)
            {
                float d = Vector3.SqrMagnitude(a.transform.position - transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    enemy = a.transform;
                }
            }
        }

        if (enemy != null)
        {
            StartFaceTargetSmooth(enemy.position, faceTargetDuration);
        }
    }

    private Transform ResolveEnemyTransform(Transform t)
    {
        if (t == null) return null;
        if (t.CompareTag("Enemy")) return t;
        var ai = t.GetComponentInParent<EnemyAI>();
        if (ai != null) return ai.transform;
        var bt = t.GetComponentInParent<BehaviorTree>();
        if (bt != null) return bt.transform;
        var er = t.GetComponentInParent<EnemyRangedAttack>();
        if (er != null) return er.transform;
        return null;
    }

    private void StartFaceTargetSmooth(Vector3 worldPosition, float duration)
    {
        if (faceCoroutine != null)
        {
            StopCoroutine(faceCoroutine);
            faceCoroutine = null;
            isFacingSmooth = false;
        }
        faceCoroutine = StartCoroutine(FaceTargetSmoothCoroutine(worldPosition, duration));
    }

    private IEnumerator FaceTargetSmoothCoroutine(Vector3 worldPosition, float duration)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            yield break;

        float startYaw = transform.eulerAngles.y;
        float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y;
        // 使用最短角度差
        float delta = Mathf.DeltaAngle(startYaw, targetYaw);

        if (duration <= 0f)
        {
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            SyncCameraYawToPlayer();
            yield break;
        }

        isFacingSmooth = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float yaw = startYaw + delta * t;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // 同步摄像机横轴
            if (freeLookCamera != null)
            {
                try
                {
                    freeLookCamera.m_XAxis.Value = yaw;
                }
                catch { }
            }

            yield return null;
        }

        // Ensure final exact rotation
        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        SyncCameraYawToPlayer();

        // 强制刷新摄像机位置/旋转（celan final)
        if (freeLookCamera != null)
        {
            try { freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation); } catch { }
        }

        isFacingSmooth = false;
        faceCoroutine = null;
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