using UnityEngine;

/// <summary>
/// Физический риг нагрудной камеры:
/// Воссоздает эффект съемки с нагрудного регистратора:
/// - Запаздывание за движением торса
/// - Тяжелое покачивание шагов с акцентом на каждый шаг
/// - Центробежный наклон при поворотах корпуса
/// - Микровибрации крепления
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

    [Header("Инерция и задержка камеры (Camera Lag)")]
    [Tooltip("Скорость следования позиции камеры за грудью")]
    [SerializeField] private float positionFollowSpeed = 25f;

    [Tooltip("Скорость углового следования за вращением персонажа")]
    [SerializeField] private float rotationLagSpeed = 18f;

    [Header("Покачивание при ходьбе (Bodycam Wobble)")]
    [Tooltip("Частота шагов при ходьбе")]
    [SerializeField] private float walkBobFrequency = 6.2f;

    [Tooltip("Частота шагов при беге")]
    [SerializeField] private float runBobFrequency = 9.8f;

    [Tooltip("Амплитуда вертикального толчка шага (м)")]
    [SerializeField] private float verticalBobAmount = 0.045f;

    [Tooltip("Амплитуда бокового переноса веса (м)")]
    [SerializeField] private float horizontalBobAmount = 0.035f;

    [Tooltip("Амплитуда вращательного наклона от шага (Roll в градусах)")]
    [SerializeField] private float rollBobAmount = 1.8f;

    [Header("Инерционный крен (Centrifugal Lean)")]
    [Tooltip("Наклон камеры в сторону при быстром повороте мыши")]
    [SerializeField] private float turnTiltMultiplier = -0.4f;

    [Tooltip("Максимальный угол наклона от поворота")]
    [SerializeField] private float maxTurnTilt = 4.0f;

    [Header("Микровибрации крепления")]
    [Tooltip("Частота шума Перлина для симуляции дрожания регистратора")]
    [SerializeField] private float microJitterSpeed = 14f;

    [Tooltip("Интенсивность микровибраций")]
    [SerializeField] private float microJitterIntensity = 0.0015f;

    private float bobTimer;
    private Vector3 currentBobPosition;
    private Quaternion currentBobRotation = Quaternion.identity;
    private Vector3 smoothedWorldPos;
    private Quaternion smoothedWorldRot;
    private float currentTurnTilt;

    public float BobCycle => bobTimer;

    private void Start()
    {
        if (chestPivotTarget != null)
        {
            smoothedWorldPos = chestPivotTarget.position;
            smoothedWorldRot = chestPivotTarget.rotation;
            transform.position = smoothedWorldPos;
            transform.rotation = smoothedWorldRot;
        }
    }

    private void LateUpdate()
    {
        if (chestPivotTarget == null) return;

        UpdateLagPhysics();
        UpdateProceduralBobbing();
        ApplyFinalTransforms();
    }

    private void UpdateLagPhysics()
    {
        float posBlend = 1f - Mathf.Exp(-positionFollowSpeed * Time.deltaTime);
        smoothedWorldPos = Vector3.Lerp(smoothedWorldPos, chestPivotTarget.position, posBlend);

        float rotBlend = 1f - Mathf.Exp(-rotationLagSpeed * Time.deltaTime);
        smoothedWorldRot = Quaternion.Slerp(smoothedWorldRot, chestPivotTarget.rotation, rotBlend);

        float mouseYawDelta = inputHandler != null ? inputHandler.LookDelta.x : 0f;
        float targetTilt = Mathf.Clamp(mouseYawDelta * turnTiltMultiplier, -maxTurnTilt, maxTurnTilt);
        float tiltBlend = 1f - Mathf.Exp(12f * Time.deltaTime);
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
            currentBobRotation = Quaternion.Euler(sinVertical * 0.8f, 0f, rollImpact);
        }
        else
        {
            float decay = 1f - Mathf.Exp(-8f * Time.deltaTime);
            currentBobPosition = Vector3.Lerp(currentBobPosition, Vector3.zero, decay);
            currentBobRotation = Quaternion.Slerp(currentBobRotation, Quaternion.identity, decay);
            bobTimer = 0f;
        }

        float noiseX = (Mathf.PerlinNoise(Time.time * microJitterSpeed, 0f) - 0.5f) * 2f * microJitterIntensity;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * microJitterSpeed) - 0.5f) * 2f * microJitterIntensity;
        currentBobPosition += new Vector3(noiseX, noiseY, 0f);
    }

    private void ApplyFinalTransforms()
    {
        transform.position = smoothedWorldPos;
        Quaternion dynamicTilt = Quaternion.Euler(0f, 0f, currentTurnTilt);
        transform.rotation = smoothedWorldRot * dynamicTilt * currentBobRotation;
        transform.Translate(currentBobPosition, Space.Self);
    }

    public void SetupReferences(Transform chestPivot, BodycamPlayerController player, PlayerInputHandler input)
    {
        chestPivotTarget = chestPivot;
        playerController = player;
        inputHandler = input;
    }
}
