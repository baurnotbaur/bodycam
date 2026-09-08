using UnityEngine;

/// <summary>
/// Контроллер тактического свободного прицеливания (Deadzone Free-Aim).
/// Оружие свободно отклоняется внутри мертвой зоны экрана, опережая камеру,
/// а корпус и камера следуют за взглядом с естественной инерцией.
/// </summary>
public class DeadzoneAimController : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [Tooltip("Скрипт ввода")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Tooltip("Контроллер персонажа (для поворота туловища)")]
    [SerializeField] private BodycamPlayerController playerController;

    [Tooltip("Pivot оружия, который вращается внутри мертвой зоны")]
    [SerializeField] private Transform weaponRigPivot;

    [Header("Границы Deadzone (в градусах)")]
    [Tooltip("Максимальный угол свободного отклонения ствола влево/вправо")]
    [SerializeField] [Range(2f, 25f)] private float maxDeadzoneYaw = 12f;

    [Tooltip("Максимальный угол свободного отклонения ствола вверх/вниз")]
    [SerializeField] [Range(2f, 20f)] private float maxDeadzonePitch = 8f;

    [Header("Коэффициенты отклика")]
    [Tooltip("Доля поворота тела при движении мыши внутри мертвой зоны")]
    [SerializeField] [Range(0.2f, 1f)] private float bodyFollowRatio = 0.65f;

    [Tooltip("Скорость доводки оружия")]
    [SerializeField] private float weaponTrackingSpeed = 28f;

    [Tooltip("Скорость автоцентрирования оружия при ходьбе")]
    [SerializeField] private float autoCenterSpeed = 4.0f;

    // Текущие локальные углы свободного прицела оружия
    private float currentWeaponYaw;
    private float currentWeaponPitch;
    private float targetWeaponYaw;
    private float targetWeaponPitch;

    public float WeaponYaw => currentWeaponYaw;
    public float WeaponPitch => currentWeaponPitch;

    private void Start()
    {
        if (weaponRigPivot != null)
        {
            Vector3 angles = weaponRigPivot.localEulerAngles;
            currentWeaponYaw = targetWeaponYaw = NormalizeAngle(angles.y);
            currentWeaponPitch = targetWeaponPitch = NormalizeAngle(angles.x);
        }
    }

    private void Update()
    {
        ProcessAiming();
    }

    private void ProcessAiming()
    {
        if (inputHandler == null || weaponRigPivot == null || playerController == null) return;

        Vector2 look = inputHandler.LookDelta;

        // 1. Поворот тела: сразу отдает часть дельты мыши на поворот тела,
        // чтобы камера всегда отзывчиво реагировала на движение мыши
        float directBodyYaw = look.x * bodyFollowRatio;
        playerController.RotateBodyYaw(directBodyYaw);

        // 2. Оставшаяся часть дельты накапливается в стволе как опережение прицела
        float weaponYawDelta = look.x * (1f - bodyFollowRatio);
        targetWeaponYaw += weaponYawDelta;
        targetWeaponPitch -= look.y * 0.45f;

        // 3. Автоцентрирование оружия при перемещении
        if (inputHandler.MoveInput.sqrMagnitude > 0.05f)
        {
            float centerDecay = 1f - Mathf.Exp(-autoCenterSpeed * Time.deltaTime);
            targetWeaponYaw = Mathf.Lerp(targetWeaponYaw, 0f, centerDecay);
            targetWeaponPitch = Mathf.Lerp(targetWeaponPitch, 0f, centerDecay);
        }

        // 4. Ограничение углов в пределах Deadzone
        // Если ствол упирается в границу мертвой зоны, весь остаток мыши разворачивает тело
        if (targetWeaponYaw > maxDeadzoneYaw)
        {
            float excess = targetWeaponYaw - maxDeadzoneYaw;
            targetWeaponYaw = maxDeadzoneYaw;
            playerController.RotateBodyYaw(excess);
        }
        else if (targetWeaponYaw < -maxDeadzoneYaw)
        {
            float excess = targetWeaponYaw + maxDeadzoneYaw;
            targetWeaponYaw = -maxDeadzoneYaw;
            playerController.RotateBodyYaw(excess);
        }

        targetWeaponPitch = Mathf.Clamp(targetWeaponPitch, -maxDeadzonePitch, maxDeadzonePitch);

        // 5. Плавная интерполяция вращения оружия
        float blend = 1f - Mathf.Exp(-weaponTrackingSpeed * Time.deltaTime);
        currentWeaponYaw = Mathf.Lerp(currentWeaponYaw, targetWeaponYaw, blend);
        currentWeaponPitch = Mathf.Lerp(currentWeaponPitch, targetWeaponPitch, blend);

        // Легкий Z-Roll (крен оружия при боковом прицеливании)
        float subtleRoll = -currentWeaponYaw * 0.2f;
        weaponRigPivot.localRotation = Quaternion.Euler(currentWeaponPitch, currentWeaponYaw, subtleRoll);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void SetupReferences(PlayerInputHandler input, BodycamPlayerController player, Transform weaponPivot)
    {
        inputHandler = input;
        playerController = player;
        weaponRigPivot = weaponPivot;
    }
}
