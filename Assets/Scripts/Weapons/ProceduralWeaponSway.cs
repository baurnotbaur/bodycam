using UnityEngine;

/// <summary>
/// Процедурное раскачивание оружия (Weapon Sway & Walk Bobbing).
/// Придает оружию вес и ощущение того, что оперативник держит его в руках перед грудью.
/// </summary>
public class ProceduralWeaponSway : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [Tooltip("Обработчик ввода")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Tooltip("Контроллер персонажа")]
    [SerializeField] private BodycamPlayerController playerController;

    [Header("Инерционное вращение ствола (Rotational Sway)")]
    [Tooltip("Сила запаздывания поворота оружия за мышью")]
    [SerializeField] private float swayAmount = 1.6f;

    [Tooltip("Максимальный угол отклонения ствола при повороте")]
    [SerializeField] private float maxSwayAngle = 8f;

    [Tooltip("Скорость возврата ствола в нейтральное положение")]
    [SerializeField] private float swaySmoothSpeed = 16f;

    [Header("Позиционная инерция (Positional Lag)")]
    [Tooltip("Сила смещения оружия в противоположную сторону движения")]
    [SerializeField] private float positionLagAmount = 0.015f;

    [Tooltip("Максимальное линейное смещение оружия")]
    [SerializeField] private float maxPositionLag = 0.05f;

    [Header("Покачивание оружия при ходьбе (Weapon Bobbing)")]
    [Tooltip("Частота шагов оружия")]
    [SerializeField] private float bobFrequency = 6.2f;

    [Tooltip("Амплитуда горизонтального покачивания ствола")]
    [SerializeField] private float bobHorizontalAmount = 0.018f;

    [Tooltip("Амплитуда вертикального покачивания ствола")]
    [SerializeField] private float bobVerticalAmount = 0.024f;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private float bobTimer;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (inputHandler == null) return;

        UpdateSway();
    }

    private void UpdateSway()
    {
        Vector2 look = inputHandler.LookDelta;

        // 1. Вычисление вращательного Sway (оружие отстает от резких разворотов мыши)
        float targetSwayPitch = Mathf.Clamp(look.y * swayAmount, -maxSwayAngle, maxSwayAngle);
        float targetSwayYaw = Mathf.Clamp(-look.x * swayAmount, -maxSwayAngle, maxSwayAngle);
        float targetSwayRoll = Mathf.Clamp(look.x * (swayAmount * 0.5f), -maxSwayAngle, maxSwayAngle);

        Quaternion targetRotOffset = Quaternion.Euler(targetSwayPitch, targetSwayYaw, targetSwayRoll);

        // 2. Вычисление позиционного лага от движения мыши
        float lagX = Mathf.Clamp(-look.x * positionLagAmount, -maxPositionLag, maxPositionLag);
        float lagY = Mathf.Clamp(-look.y * positionLagAmount, -maxPositionLag, maxPositionLag);
        Vector3 targetPosOffset = new Vector3(lagX, lagY, 0f);

        // 3. Синхронизированный боббинг оружия при ходьбе/беге
        if (playerController != null && playerController.IsGrounded && playerController.WorldVelocity.sqrMagnitude > 0.1f)
        {
            float freq = inputHandler.IsRunning ? bobFrequency * 1.5f : bobFrequency;
            bobTimer += Time.deltaTime * freq;

            float bobX = Mathf.Cos(bobTimer) * bobHorizontalAmount;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobVerticalAmount;

            targetPosOffset += new Vector3(bobX, bobY, 0f);
        }
        else
        {
            bobTimer = 0f;
        }

        // 4. Плавное применение смещений
        float blend = 1f - Mathf.Exp(-swaySmoothSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos + targetPosOffset, blend);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot * targetRotOffset, blend);
    }

    public void SetupReferences(PlayerInputHandler input, BodycamPlayerController player)
    {
        inputHandler = input;
        playerController = player;
    }
}
