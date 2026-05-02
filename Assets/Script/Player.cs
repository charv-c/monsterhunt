using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class Player: MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;
    public float gravity = -9.81f;
    public float controllerCenterY = 1f;

    [Header("旋转设置")]
    public float rotationSpeed = 15f;
    public bool smoothRotation = true;

    [Header("摄像机引用")]
    public CinemachineFreeLook freeLookCamera;  // 可选，自动查找

    [Header("动画")]
    public Animator animator;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 currentMoveDirection;
    private Transform mainCameraTransform;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (freeLookCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            freeLookCamera = FindAnyObjectByType<CinemachineFreeLook>();
#else
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
#endif
        }

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        characterController.center = new Vector3(0, controllerCenterY, 0);
    }

    void Update()
    {
        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        HandleGrounding();
        HandleRotation();
        HandleMovement();
        ApplyGravity();

        characterController.Move(velocity * Time.deltaTime);
        UpdateAnimator();
    }

    // ========== 替代 GetCameraForward / GetCameraRight ==========

    Vector3 GetCameraForward()
    {
        if (mainCameraTransform == null) return Vector3.forward;
        Vector3 forward = mainCameraTransform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    Vector3 GetCameraRight()
    {
        if (mainCameraTransform == null) return Vector3.right;
        Vector3 right = mainCameraTransform.right;
        right.y = 0f;
        return right.normalized;
    }

    // ========== 核心逻辑 ==========

    void HandleRotation()
    {
        Vector3 cameraForward = GetCameraForward();
        if (cameraForward.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        if (smoothRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        else
            transform.rotation = targetRotation;
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 cameraForward = GetCameraForward();
        Vector3 cameraRight = GetCameraRight();

        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            currentMoveDirection = moveDirection;
            Vector3 horizontalVelocity = moveDirection * currentSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
            currentMoveDirection = Vector3.zero;
        }
    }

    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -0.5f;
        else
            velocity.y += gravity * Time.deltaTime;
    }

    void HandleGrounding()
    {
        isGrounded = characterController.isGrounded;
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

    public float GetCurrentSpeed() => new Vector2(velocity.x, velocity.z).magnitude;
    public bool IsMoving() => currentMoveDirection.sqrMagnitude > 0.01f;
}