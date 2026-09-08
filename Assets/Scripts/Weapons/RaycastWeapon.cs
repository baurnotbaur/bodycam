using UnityEngine;

/// <summary>
/// Основной компонент огнестрельного оружия на Raycast-баллистике.
/// Управляет темпом стрельбы, динамическим конусом разброса, отдачей и эффектами попадания.
/// </summary>
public class RaycastWeapon : MonoBehaviour
{
    public enum FireMode { SemiAuto, FullAuto }

    [Header("Режим и темп огня")]
    [Tooltip("Режим стрельбы")]
    [SerializeField] private FireMode fireMode = FireMode.FullAuto;

    [Tooltip("Скорострельность в выстрелах в минуту (RPM)")]
    [SerializeField] private float roundsPerMinute = 650f;

    [Tooltip("Урон от одного попадания")]
    [SerializeField] private float damage = 35f;

    [Tooltip("Максимальная эффективная дистанция стрельбы (м)")]
    [SerializeField] private float maxRange = 120f;

    [Header("Баллистический разброс (Cone of Fire)")]
    [Tooltip("Базовый угол разброса (в градусах)")]
    [SerializeField] private float baseSpreadAngle = 0.4f;

    [Tooltip("Максимальный угол разброса при непрерывной стрельбе")]
    [SerializeField] private float maxSpreadAngle = 2.8f;

    [Tooltip("Прирост разброса за один выстрел")]
    [SerializeField] private float spreadPerShot = 0.35f;

    [Tooltip("Скорость восстановления кучности (град/сек)")]
    [SerializeField] private float spreadRecoverySpeed = 5.0f;

    [Header("Слои и коллизии")]
    [Tooltip("Слои, по которым регистрируются попадания")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Ссылки на компоненты")]
    [Tooltip("Скрипт захвата ввода")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Tooltip("Точка среза ствола (Muzzle Point)")]
    [SerializeField] private Transform muzzleTransform;

    [Tooltip("Пружинная система отдачи")]
    [SerializeField] private WeaponRecoilSpring recoilSpring;

    [Tooltip("Контроллер дульной вспышки")]
    [SerializeField] private MuzzleFlashController muzzleFlash;

    // Внутреннее состояние
    private float nextFireTime;
    private float currentSpread;
    private bool hasReleasedTrigger = true;

    public float CurrentSpread => currentSpread;

    private void Update()
    {
        UpdateSpreadRecovery();
        HandleShootingInput();
    }

    private void UpdateSpreadRecovery()
    {
        if (currentSpread > baseSpreadAngle)
        {
            currentSpread -= spreadRecoverySpeed * Time.deltaTime;
            if (currentSpread < baseSpreadAngle) currentSpread = baseSpreadAngle;
        }
    }

    private void HandleShootingInput()
    {
        if (inputHandler == null) return;

        bool firePressed = inputHandler.IsFiring;

        if (fireMode == FireMode.SemiAuto)
        {
            if (firePressed && hasReleasedTrigger && Time.time >= nextFireTime)
            {
                hasReleasedTrigger = false;
                ExecuteShot();
            }
            else if (!firePressed)
            {
                hasReleasedTrigger = true;
            }
        }
        else // FullAuto
        {
            if (firePressed && Time.time >= nextFireTime)
            {
                ExecuteShot();
            }
        }
    }

    private void ExecuteShot()
    {
        float fireInterval = 60f / roundsPerMinute;
        nextFireTime = Time.time + fireInterval;

        // 1. Физическая отдача
        if (recoilSpring != null)
        {
            recoilSpring.ApplyRecoilImpulse();
        }

        // 2. Вспышка дульного огня
        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        // 3. Вычисление направления выстрела с разбросом
        Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        Vector3 forward = muzzleTransform != null ? muzzleTransform.forward : transform.forward;

        // Рассчитываем случайный вектор отклонения пули внутри конуса
        Vector3 spreadDir = CalculateSpreadDirection(forward, currentSpread);

        // Увеличиваем динамический разброс
        currentSpread = Mathf.Min(currentSpread + spreadPerShot, maxSpreadAngle);

        // 4. Raycast трассировка пули
        Ray ray = new Ray(origin, spreadDir);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            ProcessHit(hit);
        }
    }

    private Vector3 CalculateSpreadDirection(Vector3 forward, float spreadAngleDegrees)
    {
        if (spreadAngleDegrees <= 0.001f) return forward;

        float spreadRad = spreadAngleDegrees * Mathf.Deg2Rad;
        Vector2 randomCircle = Random.insideUnitCircle * Mathf.Tan(spreadRad);

        Vector3 right = Vector3.Cross(forward, Vector3.up);
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        right.Normalize();

        Vector3 up = Vector3.Cross(right, forward).normalized;

        return (forward + right * randomCircle.x + up * randomCircle.y).normalized;
    }

    private void ProcessHit(RaycastHit hit)
    {
        // Применяем физический импульс Rigidbody, если объект подвижный
        Rigidbody rb = hit.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(-hit.normal * 12f, hit.point, ForceMode.Impulse);
        }

        // Создаем маркер попадания (спарк/вспышку) в точке удара
        CreateImpactEffect(hit.point, hit.normal);
    }

    private void CreateImpactEffect(Vector3 point, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = point + normal * 0.02f;
        spark.transform.localScale = Vector3.one * 0.08f;
        
        Collider col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = spark.GetComponent<Renderer>();
        if (rend != null)
        {
            Material sparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sparkMat.color = new Color(1f, 0.7f, 0.2f, 1f);
            rend.material = sparkMat;
        }

        Destroy(spark, 0.12f);
    }

    public void SetupReferences(PlayerInputHandler input, Transform muzzle, WeaponRecoilSpring recoil, MuzzleFlashController flash)
    {
        inputHandler = input;
        muzzleTransform = muzzle;
        recoilSpring = recoil;
        muzzleFlash = flash;
        currentSpread = baseSpreadAngle;
    }
}
