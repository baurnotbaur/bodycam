using UnityEngine;

/// <summary>
/// Физический контроллер перемещения персонажа с механикой нагруженного шага оперативника.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class BodycamPlayerController : MonoBehaviour
{
    [Header("Параметры движения")]
    [Tooltip("Базовая скорость ходьбы (м/с)")]
    [SerializeField] private float walkSpeed = 2.4f;

    [Tooltip("Скорость тактического бега (м/с)")]
    [SerializeField] private float runSpeed = 5.2f;

    [Tooltip("Скорость в приседе (м/с)")]
    [SerializeField] private float crouchSpeed = 1.3f;

    [Tooltip("Скорость набора и сброса инерции перемещения")]
    [SerializeField] private float acceleration = 12f;

    [Header("Гравитация и заземление")]
    [Tooltip("Сила гравитации")]
    [SerializeField] private float gravity = -18.0f;

    [Tooltip("Сила прижима к земле (ground snap)")]
    [SerializeField] private float groundStickForce = -2.0f;

    [Header("Позиционирование Bodycam")]
    [Tooltip("Transform точки крепления нагрудной камеры")]
    [SerializeField] private Transform chestMountPivot;

    [Tooltip("Высота камеры от земли в полный рост (уровень ключицы ~ 1.42м)")]
    [SerializeField] private float standChestHeight = 1.42f;

    [Tooltip("Высота камеры от земли в приседе (~ 0.85м)")]
    [SerializeField] private float crouchChestHeight = 0.85f;

    [Tooltip("Скорость смены стойки (присед/вставание)")]
    [SerializeField] private float stanceTransitionSpeed = 10f;

    private CharacterController characterController;
    private PlayerInputHandler inputHandler;

    private Vector3 currentVelocity;
    private float targetSpeed;
    private float verticalVelocity;
    private float targetChestHeight;
    private float currentChestHeight;

    public Vector3 WorldVelocity => characterController != null ? characterController.velocity : Vector3.zero;
    public float CurrentSpeedRatio => characterController != null && runSpeed > 0f ? characterController.velocity.magnitude / runSpeed : 0f;
    public bool IsGrounded => characterController != null && characterController.isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();

        targetChestHeight = standChestHeight;
        currentChestHeight = standChestHeight;

        if (chestMountPivot != null)
        {
            chestMountPivot.localPosition = new Vector3(0f, currentChestHeight, 0f);
        }
    }

    private void Update()
    {
        HandleStance();
        HandleMovement();
    }

    private void HandleStance()
    {
        targetChestHeight = inputHandler.IsCrouching ? crouchChestHeight : standChestHeight;
        
        float blend = 1f - Mathf.Exp(-stanceTransitionSpeed * Time.deltaTime);
        currentChestHeight = Mathf.Lerp(currentChestHeight, targetChestHeight, blend);

        if (chestMountPivot != null)
        {
            Vector3 pos = chestMountPivot.localPosition;
            pos.y = currentChestHeight;
            chestMountPivot.localPosition = pos;
        }
    }

    private void HandleMovement()
    {
        Vector2 input = inputHandler.MoveInput;
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (inputHandler.IsCrouching) targetSpeed = crouchSpeed;
        else if (inputHandler.IsRunning && isMoving) targetSpeed = runSpeed;
        else targetSpeed = walkSpeed;

        if (!isMoving) targetSpeed = 0f;

        Vector3 targetDirection = transform.right * input.x + transform.forward * input.y;
        Vector3 targetHorizontalVelocity = targetDirection * targetSpeed;

        float accelRate = 1f - Mathf.Exp(-acceleration * Time.deltaTime);
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetHorizontalVelocity.x, accelRate);
        currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetHorizontalVelocity.z, accelRate);

        if (characterController.isGrounded)
        {
            verticalVelocity = groundStickForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        currentVelocity.y = verticalVelocity;
        characterController.Move(currentVelocity * Time.deltaTime);
    }

    public void RotateBodyYaw(float angleDelta)
    {
        transform.Rotate(Vector3.up, angleDelta, Space.World);
    }

    public void SetChestMountPivot(Transform pivot)
    {
        chestMountPivot = pivot;
    }
}
