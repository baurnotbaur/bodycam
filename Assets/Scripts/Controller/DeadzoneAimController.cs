using UnityEngine;

/// <summary>
/// Контроллер свободного прицеливания в пределах экранной мертвой зоны (Deadzone Aiming).
/// Оружие двигается свободно до границ мертвой зоны, после чего начинает поворачиваться камера и тело.
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

    [Tooltip("Transform рига камеры для наклона по вертикали (Pitch)")]
    [SerializeField] private Transform cameraRigTransform;

    [Header("Границы Deadzone (в градусах)")]
    [Tooltip("Максимальный угол отклонения ствола влево/вправо до поворота корпуса")]
    [SerializeField] [Range(2f, 25f)] private float maxDeadzoneYaw = 14f;

    [Tooltip("Максимальный угол отклонения ствола вверх/вниз до наклона камеры")]
    [SerializeField] [Range(2f, 20f)] private float maxDeadzonePitch = 10f;

    [Header("Динамика доводки и возврата")]
    [Tooltip("Коэффициент сглаживания движения оружия")]
    [SerializeField] private float weaponTrackingSpeed = 35f;

    [Tooltip("Скорость автоматического центрирования оружия при движении вперед")]
    [SerializeField] private float centeringSpeed = 3.5f;

    [Tooltip("Вертикальные лимиты наклона взгляда (Pitch Clamping)")]
    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-75f, 75f);

    private float currentWeaponYaw;
    private float currentWeaponPitch;
    private float targetWeaponYaw;
    private float targetWeaponPitch;
    private float cameraPitchAngle;

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

        if (cameraRigTransform != null)
        {
            cameraPitchAngle = NormalizeAngle(cameraRigTransform.localEulerAngles.x);
        }
    }

    private void Update()
    {
        ProcessAiming();
    }

    private void ProcessAiming()
    {
        if (inputHandler == null || weaponRigPivot == null) return;

        Vector2 look = inputHandler.LookDelta;

        targetWeaponYaw += look.x;
        targetWeaponPitch -= look.y;

        if (inputHandler.MoveInput.sqrMagnitude > 0.1f)
        {
            float centerDecay = 1f - Mathf.Exp(-centeringSpeed * Time.deltaTime);
            targetWeaponYaw = Mathf.Lerp(targetWeaponYaw, 0f, centerDecay);
            targetWeaponPitch = Mathf.Lerp(targetWeaponPitch, 0f, centerDecay);
        }

        float excessYaw = 0f;
        if (targetWeaponYaw > maxDeadzoneYaw)
        {
            excessYaw = targetWeaponYaw - maxDeadzoneYaw;
            targetWeaponYaw = maxDeadzoneYaw;
        }
        else if (targetWeaponYaw < -maxDeadzoneYaw)
        {
            excessYaw = targetWeaponYaw + maxDeadzoneYaw;
            targetWeaponYaw = -maxDeadzoneYaw;
        }

        float excessPitch = 0f;
        if (targetWeaponPitch > maxDeadzonePitch)
        {
            excessPitch = targetWeaponPitch - maxDeadzonePitch;
            targetWeaponPitch = maxDeadzonePitch;
        }
        else if (targetWeaponPitch < -maxDeadzonePitch)
        {
            excessPitch = targetWeaponPitch + maxDeadzonePitch;
            targetWeaponPitch = -maxDeadzonePitch;
        }

        if (Mathf.Abs(excessYaw) > Mathf.Epsilon && playerController != null)
        {
            playerController.RotateBodyYaw(excessYaw);
        }

        if (Mathf.Abs(excessPitch) > Mathf.Epsilon && cameraRigTransform != null)
        {
            cameraPitchAngle -= excessPitch;
            cameraPitchAngle = Mathf.Clamp(cameraPitchAngle, verticalLookLimits.x, verticalLookLimits.y);
            cameraRigTransform.localRotation = Quaternion.Euler(cameraPitchAngle, 0f, 0f);
        }

        float weaponBlend = 1f - Mathf.Exp(-weaponTrackingSpeed * Time.deltaTime);
        currentWeaponYaw = Mathf.Lerp(currentWeaponYaw, targetWeaponYaw, weaponBlend);
        currentWeaponPitch = Mathf.Lerp(currentWeaponPitch, targetWeaponPitch, weaponBlend);

        float subtleRoll = -currentWeaponYaw * 0.15f;
        weaponRigPivot.localRotation = Quaternion.Euler(currentWeaponPitch, currentWeaponYaw, subtleRoll);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void SetupReferences(PlayerInputHandler input, BodycamPlayerController player, Transform weaponPivot, Transform cameraRig)
    {
        inputHandler = input;
        playerController = player;
        weaponRigPivot = weaponPivot;
        cameraRigTransform = cameraRig;
    }
}
