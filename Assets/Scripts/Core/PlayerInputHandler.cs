using UnityEngine;

/// <summary>
/// Централизованный обработчик ввода игрока.
/// Поддерживает сглаживание дельты мыши для устранения аппаратных микрорывков.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Настройки чувствительности")]
    [Tooltip("Горизонтальная чувствительность мыши")]
    [SerializeField] [Range(0.1f, 10f)] private float mouseSensitivityX = 1.8f;

    [Tooltip("Вертикальная чувствительность мыши")]
    [SerializeField] [Range(0.1f, 10f)] private float mouseSensitivityY = 1.8f;

    [Tooltip("Сглаживание сырого ввода мыши (предотвращает ступенчатые рывки сенсора)")]
    [SerializeField] private bool smoothMouseInput = true;

    [Tooltip("Коэффициент сглаживания мыши")]
    [SerializeField] [Range(1f, 50f)] private float mouseSmoothingFactor = 25f;

    private Vector2 rawLookDelta;
    private Vector2 smoothedLookDelta;
    private Vector2 moveInput;
    private bool isRunning;
    private bool isCrouching;
    private bool isFiring;
    private bool isAiming;

    public Vector2 LookDelta => smoothMouseInput ? smoothedLookDelta : rawLookDelta;
    public Vector2 MoveInput => moveInput;
    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;
    public bool IsFiring => isFiring;
    public bool IsAiming => isAiming;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        GatherMovementInput();
        GatherLookInput();
        GatherCombatInput();
    }

    private void GatherMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;

        isRunning = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);
    }

    private void GatherLookInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivityY;
        rawLookDelta = new Vector2(mouseX, mouseY);

        if (smoothMouseInput)
        {
            float blend = 1f - Mathf.Exp(-mouseSmoothingFactor * Time.deltaTime);
            smoothedLookDelta = Vector2.Lerp(smoothedLookDelta, rawLookDelta, blend);
        }
    }

    private void GatherCombatInput()
    {
        isFiring = Input.GetMouseButton(0); // ЛКМ
        isAiming = Input.GetMouseButton(1); // ПКМ
    }
}
