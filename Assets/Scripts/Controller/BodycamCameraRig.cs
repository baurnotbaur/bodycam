using UnityEngine;

/// <summary>
/// Физический риг нагрудной камеры Bodycam:
/// - Вертикальный наклон (Pitch) с плавной физической задержкой
/// - Запаздывание за движением торса (Rotational Inertia)
/// - Процедурный боббинг шагов Лиссажу с акцентом на упор стопы
/// - Центробежный крен при резких разворотах
/// - Микровибрации клипсы регистратора
/// </summary>
public class BodycamCameraRig : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Точка привязки камеры на груди")]
    [SerializeField] private Transform chestPivotTarget;

    [Tooltip("Контроллер игрока для чтения скорости")]
    [SerializeField] private BodycamPlayerController playerController;

    [Tooltip("Обработчик ввода")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Вертикальный обзор (Pitch)")]
    [Tooltip("Лимиты наклона взгляда вверх/вниз")]
    [SerializeField] private Vector2 pitchLimits = new Vector2(-75f, 75f);

    [Tooltip("Скорость сглаживания вертикального наклона")]
    [SerializeField] private float pitchSmoothSpeed = 25f;

    [Header("Инерция и задержка камеры (Camera Lag)")]
    [Tooltip("Скорость следования позиции камеры за грудью")]
    [SerializeField] private float positionFollowSpeed = 30f;

    [Tooltip("Скорость углового следования за вращением персонажа (чем ниже, тем тяжелее камера)")]
    [SerializeField] private float rotationLagSpeed = 16f;

    [Header("Покачивание при ходьбе (Bodycam Wobble)")]
    [Tooltip("Частота шагов при ходьбе")]
    [SerializeField] private float walkBobFrequency = 6.0f;

    [Tooltip("Частота шагов при беге")]
    [SerializeField] private float runBobFrequency = 9.5f;

    [Tooltip("Амплитуда вертикального толчка шага (м)")]
    [SerializeField] private float verticalBobAmount = 0.04f;

    [Tooltip("Амплитуда бокового переноса веса (м)")]
    [SerializeField] private float horizontalBobAmount = 0.03f;

    [Tooltip("Амплитуда вращательного наклона от шага (Roll в градусах)")]
    [SerializeField] private float rollBobAmount = 1.6f;

    [Header("Инерционный крен (Centrifugal Lean)")]
    [Tooltip("Наклон камеры в сторону при быстром повороте мыши")]
    [SerializeField] private float turnTiltMultiplier = -0.35f;

    [Tooltip("Максимальный угол наклона от поворота")]
    [SerializeField] private float maxTurnTilt = 4.0f;

    [Header("Микровибрации крепления")]
    [Tooltip("Частота шума Перлина для симуляции дрожания регистратора")]
    [SerializeField] private float microJitterSpeed = 12f;

    [Tooltip("Интенсивность микровибраций")]
    [SerializeField] private float microJitterIntensity = 0.0012f;

    private float bobTimer;
    private Vector3 currentBobPosition;
    private Quaternion currentBobRotation = Quaternion.identity;

    private Vector3 smoothedWorldPos;
    private Quaternion smoothedYawRot;
    private float targetPitch;
    private float currentPitch;
    private float currentTurnTilt;

    public float CurrentPitch => currentPitch;

    private void Start()
    {
        if (chestPivotTarget != null)
        {
            smoothedWorldPos = chestPivotTarget.position;
            smoothedYawRot = chestPivotTarget.rotation;
            transform.position = smoothedWorldPos;
            transform.rotation = smoothedYawRot;
        }
    }

    private void LateUpdate()
    {
        if (chestPivotTarget == null) return;

        UpdatePitch();
        UpdateLagPhysics();
        UpdateProceduralBobbing();
        ApplyFinalTransforms();
    }

    private void UpdatePitch()
    {
        if (inputHandler == null) return;

        // Накопление вертикального угла наклона мыши
        float mouseY = inputHandler.LookDelta.y;
        targetPitch -= mouseY;
        targetPitch = Mathf.Clamp(targetPitch, pitchLimits.x, pitchLimits.y);

        float pitchBlend = 1f - Mathf.Exp(-pitchSmoothSpeed * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, pitchBlend);
    }

    private void UpdateLagPhysics()
    {
        // 1. Позиционное следование за точкой крепления на груди
        float posBlend = 1f - Mathf.Exp(-positionFollowSpeed * Time.deltaTime);
        smoothedWorldPos = Vector3.Lerp(smoothedWorldPos, chestPivotTarget.position, posBlend);

        // 2. Горизонтальное угловое запаздывание за торсом
        float rotBlend = 1f - Mathf.Exp(-rotationLagSpeed * Time.deltaTime);
        smoothedYawRot = Quaternion.Slerp(smoothedYawRot, chestPivotTarget.rotation, rotBlend);

        // 3. Динамический центробежный крен при резких разворотах
        float mouseYawDelta = inputHandler != null ? inputHandler.LookDelta.x : 0f;
        float targetTilt = Mathf.Clamp(mouseYawDelta * turnTiltMultiplier, -maxTurnTilt, maxTurnTilt);
        float tiltBlend = 1f - Mathf.Exp(14f * Time.deltaTime);
        currentTurnTilt = Mathf.Lerp(currentTurnTilt, targetTilt, tiltBlend);
    }

    private void UpdateProceduralBobbing()
    {
        if (playerController == null) return;

        Vector3 speed = playerController.WorldVelocity;
        speed.y = 0f;
        float horizontalSpeed = speed.magnitude;

        if (horizontalSpeed > 0.1f && playerController.IsGrounded)
        {
            float freq = inputHandler != null && inputHandler.IsRunning ? runBobFrequency : walkBobFrequency;
            bobTimer += Time.deltaTime * freq;

            float sinVertical = Mathf.Sin(bobTimer * 2f);
            float cosHorizontal = Mathf.Cos(bobTimer);

            float verticalImpact = -Mathf.Abs(sinVertical) * verticalBobAmount;
            float horizontalSway = cosHorizontal * horizontalBobAmount;
            float rollImpact = cosHorizontal * rollBobAmount;

            currentBobPosition = new Vector3(horizontalSway, verticalImpact, 0f);
            currentBobRotation = Quaternion.Euler(sinVertical * 0.7f, 0f, rollImpact);
        }
        else
        {
            float decay = 1f - Mathf.Exp(-8f * Time.deltaTime);
            currentBobPosition = Vector3.Lerp(currentBobPosition, Vector3.zero, decay);
            currentBobRotation = Quaternion.Slerp(currentBobRotation, Quaternion.identity, decay);
            bobTimer = 0f;
        }

        // Микровибрации крепления экшн-камеры
        float noiseX = (Mathf.PerlinNoise(Time.time * microJitterSpeed, 0f) - 0.5f) * 2f * microJitterIntensity;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * microJitterSpeed) - 0.5f) * 2f * microJitterIntensity;
        currentBobPosition += new Vector3(noiseX, noiseY, 0f);
    }

    private void ApplyFinalTransforms()
    {
        transform.position = smoothedWorldPos;

        // Итоговое вращение камеры:
        // [Сглаженный поворот торса по Yaw] * [Вертикальный наклон Pitch] * [Крен поворота] * [Боббинг шагов]
        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion tiltRotation = Quaternion.Euler(0f, 0f, currentTurnTilt);

        transform.rotation = smoothedYawRot * pitchRotation * tiltRotation * currentBobRotation;

        // Локальное смещение от переноса веса шага
        transform.Translate(currentBobPosition, Space.Self);
    }

    public void SetupReferences(Transform chestPivot, BodycamPlayerController player, PlayerInputHandler input)
    {
        chestPivotTarget = chestPivot;
        playerController = player;
        inputHandler = input;
    }
}
