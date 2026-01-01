using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Gravity")]
    public float gravity = -25f;
    public float groundedStickForce = -6f; // сильнее прижимаем к земле

    [Header("References")]
    public Transform cameraTransform;  // Main Camera (или CameraRig)
    public Transform model;            // Визуал персонажа (меши/armature), НЕ корень!
    public Animator animator;          // Animator на модели

    [Header("Rotation")]
    public float rotationSharpness = 12f; // больше = быстрее поворот

    [Header("Animation")]
    [Tooltip("Порог, чтобы не дёргать анимацию при микровводе")]
    public float inputDeadZone = 0.01f;

    private CharacterController controller;
    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Подстраховка, чтобы не словить null в рантайме
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 1) Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2) Направление относительно камеры (по плоскости XZ)
        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 moveDir = (camForward * v + camRight * h);
        float inputMagnitude = Mathf.Clamp01(moveDir.magnitude);

        if (inputMagnitude > inputDeadZone)
            moveDir.Normalize();
        else
            moveDir = Vector3.zero;

        // 3) Walk/Run
        bool run = Input.GetKey(KeyCode.LeftShift) && v > 0.01f;
        float speed = run ? runSpeed : walkSpeed;

        // 4) Поворот ТОЛЬКО визуала (model), чтобы камера-child не попадала в "петлю"
        if (model != null && inputMagnitude > inputDeadZone)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            model.rotation = Quaternion.Slerp(model.rotation, targetRot, rotationSharpness * Time.deltaTime);
        }

        // 5) Гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;

        verticalVelocity += gravity * Time.deltaTime;

        // 6) Движение
        Vector3 velocity = moveDir * (speed * inputMagnitude);
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // 7) Анимация (без задержки реакции)
        // 0 = idle, ~0.5 = walk, 1 = run
        if (animator != null)
        {
            float animSpeed = inputMagnitude * (run ? 1f : 0.5f);
            animator.SetFloat("Speed", animSpeed); // мгновенный отклик (без damp)
        }
    }
}
