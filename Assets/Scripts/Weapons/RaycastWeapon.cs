using System.Collections;
using UnityEngine;

/// <summary>
/// Продвинутый тактический контроллер огнестрельного оружия.
/// Поддерживает:
/// - АК-74 (5.45x39, 30 патронов, Full-Auto / Semi-Auto)
/// - Тактический пистолет (9x19, 17 патронов, Semi-Auto, затворная задержка)
/// - Точечное прицеливание Point-Aim (ПКМ) со снижением разброса на 70%
/// - Стойку Sprint Low-Ready при тактическом беге
/// - Физический выброс гильз с рикошетом и звоном латуни
/// - Тактическую и полную перезарядку с процедурным звуковым сопровождением
/// - Тактический фонарь и ЛЦУ
/// </summary>
public class RaycastWeapon : MonoBehaviour
{
    public enum WeaponType { AK74, Pistol }
    public enum FireMode { SemiAuto, FullAuto }

    [Header("Идентификация оружия")]
    [Tooltip("Тип оружия")]
    [SerializeField] private WeaponType weaponType = WeaponType.AK74;

    [Tooltip("Название для HUD")]
    [SerializeField] private string weaponName = "AK-74";

    [Tooltip("Калибр")]
    [SerializeField] private string caliberName = "5.45x39 MM";

    [Header("Режим и темп огня")]
    [Tooltip("Текущий режим стрельбы")]
    [SerializeField] private FireMode fireMode = FireMode.FullAuto;

    [Tooltip("Возможность переключения режимов (B)")]
    [SerializeField] private bool canSwitchFireMode = true;

    [Tooltip("Скорострельность в выстрелах в минуту (RPM)")]
    [SerializeField] private float roundsPerMinute = 650f;

    [Tooltip("Урон от одного попадания")]
    [SerializeField] private float damage = 42f;

    [Tooltip("Максимальная эффективная дистанция стрельбы (м)")]
    [SerializeField] private float maxRange = 150f;

    [Header("Боезапас и перезарядка")]
    [Tooltip("Вместимость магазина")]
    [SerializeField] private int magazineCapacity = 30;

    [Tooltip("Текущее количество патронов в магазине")]
    [SerializeField] private int currentAmmo = 30;

    [Tooltip("Запасные патроны в разгрузке")]
    [SerializeField] private int reserveAmmo = 120;

    [Tooltip("Время тактической перезарядки (сек)")]
    [SerializeField] private float tacticalReloadTime = 2.2f;

    [Tooltip("Время полной перезарядки при пустом патроннике (сек)")]
    [SerializeField] private float emptyReloadTime = 2.9f;

    [Header("Баллистический разброс (Cone of Fire)")]
    [Tooltip("Базовый угол разброса (в градусах)")]
    [SerializeField] private float baseSpreadAngle = 0.35f;

    [Tooltip("Максимальный угол разброса при непрерывной стрельбе")]
    [SerializeField] private float maxSpreadAngle = 2.6f;

    [Tooltip("Прирост разброса за один выстрел")]
    [SerializeField] private float spreadPerShot = 0.32f;

    [Tooltip("Скорость восстановления кучности (град/сек)")]
    [SerializeField] private float spreadRecoverySpeed = 5.5f;

    [Tooltip("Множитель разброса при прицеливании Point-Aim (уменьшение на 70%)")]
    [SerializeField] private float aimSpreadMultiplier = 0.30f;

    [Header("Тактические стойки (Позиции)")]
    [Tooltip("Локальное смещение при Point-Aim (ПКМ)")]
    [SerializeField] private Vector3 aimPosOffset = new Vector3(-0.06f, 0.04f, 0.06f);

    [Tooltip("Локальный доворот при Point-Aim (ПКМ)")]
    [SerializeField] private Vector3 aimRotOffset = new Vector3(-1f, 1.5f, 2f);

    [Tooltip("Локальное смещение при Low-Ready (бег)")]
    [SerializeField] private Vector3 lowReadyPosOffset = new Vector3(0.02f, -0.14f, -0.05f);

    [Tooltip("Локальный наклон вниз при Low-Ready")]
    [SerializeField] private Vector3 lowReadyRotOffset = new Vector3(25f, -15f, 10f);

    [Header("Кобура и смена оружия (Holster / Draw)")]
    [Tooltip("Величина опускания оружия при уборке в кобуру")]
    [SerializeField] private Vector3 holsterPosOffset = new Vector3(0.04f, -0.32f, -0.06f);

    [Tooltip("Наклон оружия при уборке в кобуру")]
    [SerializeField] private Vector3 holsterRotOffset = new Vector3(22f, -12f, 8f);

    [Tooltip("Скорость перехода между стойками")]
    [SerializeField] private float stanceTransitionSpeed = 14f;

    [Header("Слои и коллизии")]
    [Tooltip("Слои регистрации попаданий")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Ссылки на компоненты")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private Transform ejectionPortTransform;
    [SerializeField] private WeaponRecoilSpring recoilSpring;
    [SerializeField] private MuzzleFlashController muzzleFlash;
    [SerializeField] private Light tacticalFlashlight;
    [SerializeField] private PistolSlideController pistolSlide;

    // Внутреннее состояние
    private float nextFireTime;
    private float currentSpread;
    private bool hasReleasedTrigger = true;
    private bool isReloading;
    private bool isFlashlightOn;
    private float holsterProgress = 0f; // 0 = ready, 1 = fully holstered
    private Vector3 initialBasePos;
    private Quaternion initialBaseRot;

    // Кешированные материалы (предотвращение утечек памяти)
    private static Material akBrassMat;
    private static Material pistolBrassMat;
    private static Material sparkMat;

    // Публичные свойства для HUD и систем
    public WeaponType CurrentWeaponType => weaponType;
    public string WeaponName => weaponName;
    public string CaliberName => caliberName;
    public FireMode CurrentFireMode => fireMode;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineCapacity => magazineCapacity;
    public bool IsReloading => isReloading;
    public bool IsFlashlightOn => isFlashlightOn;
    public bool IsAiming => inputHandler != null && inputHandler.IsAiming;
    public bool IsLowReady => inputHandler != null && inputHandler.IsRunning && inputHandler.MoveInput.sqrMagnitude > 0.05f;
    public bool IsHolsteredOrSwitching => holsterProgress > 0.01f;
    public float HolsterProgress => holsterProgress;
    public float Damage => damage;
    public float CurrentSpread => currentSpread;

    public void SetHolsterProgress(float progress)
    {
        holsterProgress = Mathf.Clamp01(progress);
    }

    private void Awake()
    {
        if (initialBasePos == Vector3.zero && transform.localPosition != Vector3.zero)
        {
            initialBasePos = transform.localPosition;
            initialBaseRot = transform.localRotation;
        }
        currentSpread = baseSpreadAngle;
    }

    private void OnEnable()
    {
        if (tacticalFlashlight != null)
        {
            tacticalFlashlight.enabled = isFlashlightOn;
        }
    }

    private void OnDisable()
    {
        if (isReloading)
        {
            isReloading = false;
        }
    }

    private void Start()
    {
        if (initialBasePos == Vector3.zero)
        {
            initialBasePos = transform.localPosition;
            initialBaseRot = transform.localRotation;
        }

        if (tacticalFlashlight != null)
        {
            tacticalFlashlight.enabled = isFlashlightOn;
        }
    }

    private void Update()
    {
        UpdateSpreadRecovery();
        HandleStanceTransform();
        HandleCombatInput();
    }

    private void UpdateSpreadRecovery()
    {
        if (currentSpread > baseSpreadAngle)
        {
            currentSpread -= spreadRecoverySpeed * Time.deltaTime;
            if (currentSpread < baseSpreadAngle) currentSpread = baseSpreadAngle;
        }
    }

    /// <summary>
    /// Плавное позиционирование оружия между обычной стойкой, Point-Aim (ПКМ), Low-Ready (Sprint) и Holster (смена).
    /// </summary>
    private void HandleStanceTransform()
    {
        Vector3 targetPos = initialBasePos;
        Quaternion targetRot = initialBaseRot;

        if (IsLowReady)
        {
            targetPos = initialBasePos + lowReadyPosOffset;
            targetRot = initialBaseRot * Quaternion.Euler(lowReadyRotOffset);
        }
        else if (IsAiming)
        {
            targetPos = initialBasePos + aimPosOffset;
            targetRot = initialBaseRot * Quaternion.Euler(aimRotOffset);
        }

        // Плавный уход в кобуру при смене оружия
        if (holsterProgress > 0.001f)
        {
            targetPos = Vector3.Lerp(targetPos, initialBasePos + holsterPosOffset, holsterProgress);
            targetRot = Quaternion.Slerp(targetRot, initialBaseRot * Quaternion.Euler(holsterRotOffset), holsterProgress);
        }

        float blend = 1f - Mathf.Exp(-stanceTransitionSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, blend);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, blend);
    }

    private void HandleCombatInput()
    {
        // Полная блокировка ввода во время смены оружия
        if (inputHandler == null || IsHolsteredOrSwitching) return;

        // Переключение фонаря (F)
        if (inputHandler.FlashlightTogglePressed)
        {
            ToggleFlashlight();
        }

        // Переключение режима огня (B)
        if (inputHandler.FireModeSwitchPressed && canSwitchFireMode)
        {
            ToggleFireMode();
        }

        // Перезарядка (R)
        if (inputHandler.ReloadPressed && !isReloading && currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
        }

        // Блокировка стрельбы во время перезарядки или спринта Low-Ready
        if (isReloading || IsLowReady) return;

        bool firePressed = inputHandler.IsFiring;

        if (fireMode == FireMode.SemiAuto)
        {
            if (firePressed && hasReleasedTrigger)
            {
                hasReleasedTrigger = false;
                if (currentAmmo > 0)
                {
                    if (Time.time >= nextFireTime) ExecuteShot();
                }
                else
                {
                    // Холостой спуск курка (Dry fire)
                    BodycamAudioEngine.PlayDryFire(transform.position);
                }
            }
            else if (!firePressed)
            {
                hasReleasedTrigger = true;
            }
        }
        else // FullAuto
        {
            if (firePressed)
            {
                if (currentAmmo > 0)
                {
                    if (Time.time >= nextFireTime) ExecuteShot();
                }
                else if (hasReleasedTrigger)
                {
                    hasReleasedTrigger = false;
                    BodycamAudioEngine.PlayDryFire(transform.position);
                }
            }
            else
            {
                hasReleasedTrigger = true;
            }
        }
    }

    private void ExecuteShot()
    {
        float fireInterval = 60f / roundsPerMinute;
        nextFireTime = Time.time + fireInterval;

        // Уменьшаем боезапас
        currentAmmo--;

        // 1. Звук выстрела
        Vector3 shotPos = muzzleTransform != null ? muzzleTransform.position : transform.position;
        if (weaponType == WeaponType.AK74)
        {
            BodycamAudioEngine.PlayAkShot(shotPos);
        }
        else
        {
            BodycamAudioEngine.PlayPistolShot(shotPos);
        }

        // 2. Отдача
        if (recoilSpring != null)
        {
            recoilSpring.ApplyRecoilImpulse();
        }

        // 3. Дульная вспышка
        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        // 4. Анимация затвора пистолета (Blowback / Slide-Lock)
        if (pistolSlide != null)
        {
            pistolSlide.OnFire(currentAmmo <= 0);
        }

        // 5. Физический выброс гильзы
        EjectShellCasing();

        // 6. Расчет разброса с учетом Point-Aim
        float effectiveSpread = currentSpread;
        if (IsAiming)
        {
            effectiveSpread *= aimSpreadMultiplier;
        }

        Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        Vector3 forward = muzzleTransform != null ? muzzleTransform.forward : transform.forward;
        Vector3 spreadDir = CalculateSpreadDirection(forward, effectiveSpread);

        // Наращиваем разброс от выстрела
        currentSpread = Mathf.Min(currentSpread + spreadPerShot, maxSpreadAngle);

        // 7. Raycast баллистика
        Ray ray = new Ray(origin, spreadDir);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            ProcessHit(hit);
        }
    }

    private static Material GetOrCreateBrassMaterial(bool isAk)
    {
        if (isAk)
        {
            if (akBrassMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                akBrassMat = new Material(s);
                akBrassMat.name = "AK_Brass_Material";
                akBrassMat.color = new Color(0.85f, 0.68f, 0.24f, 1f);
                akBrassMat.SetFloat("_Metallic", 0.9f);
                akBrassMat.SetFloat("_Smoothness", 0.82f);
            }
            return akBrassMat;
        }
        else
        {
            if (pistolBrassMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                pistolBrassMat = new Material(s);
                pistolBrassMat.name = "Pistol_Brass_Material";
                pistolBrassMat.color = new Color(0.88f, 0.72f, 0.28f, 1f);
                pistolBrassMat.SetFloat("_Metallic", 0.88f);
                pistolBrassMat.SetFloat("_Smoothness", 0.80f);
            }
            return pistolBrassMat;
        }
    }

    private static Material GetOrCreateSparkMaterial()
    {
        if (sparkMat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            sparkMat = new Material(unlit);
            sparkMat.name = "Impact_Spark_Material";
            sparkMat.color = new Color(1f, 0.75f, 0.2f, 1f);
        }
        return sparkMat;
    }

    private void EjectShellCasing()
    {
        Vector3 ejectPos = ejectionPortTransform != null ? ejectionPortTransform.position : transform.position + transform.right * 0.05f;
        Quaternion ejectRot = ejectionPortTransform != null ? ejectionPortTransform.rotation : transform.rotation;

        GameObject casingObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        casingObj.name = "ShellCasing";
        casingObj.transform.position = ejectPos;
        casingObj.transform.rotation = ejectRot;

        if (weaponType == WeaponType.AK74)
        {
            // Калибр 5.45x39: удлиненная бутылочная гильза
            casingObj.transform.localScale = new Vector3(0.012f, 0.024f, 0.012f);
        }
        else
        {
            // Калибр 9x19: компактная цилиндрическая гильза
            casingObj.transform.localScale = new Vector3(0.010f, 0.014f, 0.010f);
        }

        // Латунный материал (Brass) из общего кеша (без утечек памяти)
        Renderer rend = casingObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetOrCreateBrassMaterial(weaponType == WeaponType.AK74);
        }

        // Коллайдер
        Collider col = casingObj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        CapsuleCollider capCol = casingObj.AddComponent<CapsuleCollider>();
        capCol.radius = 0.5f;
        capCol.height = 2f;
        capCol.direction = 1; // Y-axis

        // Игнорируем коллизии с телом игрока
        CharacterController playerCC = GetComponentInParent<CharacterController>();
        if (playerCC != null)
        {
            Physics.IgnoreCollision(capCol, playerCC, true);
        }

        // Rigidbody и скрипт гильзы
        Rigidbody rb = casingObj.AddComponent<Rigidbody>();
        rb.mass = weaponType == WeaponType.AK74 ? 0.012f : 0.008f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        ShellCasing shell = casingObj.AddComponent<ShellCasing>();
        if (playerCC != null)
        {
            shell.IgnoreCollisionWith(playerCC);
        }

        // Направление выброса: вправо и назад (outward and backward)
        Vector3 rightDir = ejectionPortTransform != null ? ejectionPortTransform.right : transform.right;
        Vector3 upDir = ejectionPortTransform != null ? ejectionPortTransform.up : transform.up;
        Vector3 fwdDir = ejectionPortTransform != null ? ejectionPortTransform.forward : transform.forward;

        Vector3 linearImpulse;
        if (weaponType == WeaponType.AK74)
        {
            linearImpulse = (rightDir * Random.Range(3.0f, 4.0f) + upDir * Random.Range(1.8f, 2.6f) - fwdDir * Random.Range(0.6f, 1.4f)) + Random.insideUnitSphere * 0.2f;
        }
        else
        {
            linearImpulse = (rightDir * Random.Range(2.4f, 3.2f) + upDir * Random.Range(1.4f, 2.0f) - fwdDir * Random.Range(0.4f, 0.9f)) + Random.insideUnitSphere * 0.15f;
        }

        Vector3 torqueImpulse = new Vector3(
            Random.Range(-40f, 40f),
            Random.Range(-60f, 60f),
            Random.Range(-40f, 40f)
        );

        shell.Eject(linearImpulse, torqueImpulse);
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
        // 1. Посылаем урон компонентам цели
        hit.collider.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);

        // 2. Импульс физическим объектам
        Rigidbody rb = hit.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(-hit.normal * (damage * 0.4f), hit.point, ForceMode.Impulse);
        }

        // 3. Эффект попадания (искры)
        CreateImpactEffect(hit.point, hit.normal);
    }

    private void CreateImpactEffect(Vector3 point, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = point + normal * 0.02f;
        spark.transform.localScale = Vector3.one * 0.07f;

        Collider col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = spark.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetOrCreateSparkMaterial();
        }

        Destroy(spark, 0.12f);
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        bool isEmpty = currentAmmo <= 0;
        float duration = isEmpty ? emptyReloadTime : tacticalReloadTime;

        // 1. Извлечение пустого магазина
        BodycamAudioEngine.PlayMagOut(transform.position);

        yield return new WaitForSeconds(duration * 0.42f);

        // 2. Вставка нового магазина
        BodycamAudioEngine.PlayMagIn(transform.position);

        yield return new WaitForSeconds(duration * 0.38f);

        // 3. Досылание патрона в патронник при пустой перезарядке
        if (isEmpty)
        {
            if (weaponType == WeaponType.AK74)
            {
                BodycamAudioEngine.PlayBoltRack(transform.position);
            }
            else
            {
                BodycamAudioEngine.PlaySlideRelease(transform.position);
                if (pistolSlide != null)
                {
                    pistolSlide.ReleaseSlide();
                }
            }
            yield return new WaitForSeconds(duration * 0.20f);
        }

        // Заполнение боеприпасами
        int needed = magazineCapacity - currentAmmo;
        int toLoad = Mathf.Min(needed, reserveAmmo);
        currentAmmo += toLoad;
        reserveAmmo -= toLoad;

        isReloading = false;
    }

    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        if (tacticalFlashlight != null)
        {
            tacticalFlashlight.enabled = isFlashlightOn;
        }
        BodycamAudioEngine.PlayFlashlightClick(transform.position);
    }

    public void ToggleFireMode()
    {
        fireMode = (fireMode == FireMode.FullAuto) ? FireMode.SemiAuto : FireMode.FullAuto;
        BodycamAudioEngine.PlaySelectorClick(transform.position);
    }

    public void SetupReferences(
        PlayerInputHandler input,
        Transform muzzle,
        Transform ejectionPort,
        WeaponRecoilSpring recoil,
        MuzzleFlashController flash,
        Light flashlight,
        PistolSlideController slide)
    {
        inputHandler = input;
        muzzleTransform = muzzle;
        ejectionPortTransform = ejectionPort;
        recoilSpring = recoil;
        muzzleFlash = flash;
        tacticalFlashlight = flashlight;
        pistolSlide = slide;
    }

    public void ConfigureWeaponSpecs(WeaponType type, string name, string caliber, int magCap, int current, int reserve, float rpm, float dmg, FireMode mode, bool canSwitch)
    {
        weaponType = type;
        weaponName = name;
        caliberName = caliber;
        magazineCapacity = magCap;
        currentAmmo = current;
        reserveAmmo = reserve;
        roundsPerMinute = rpm;
        damage = dmg;
        fireMode = mode;
        canSwitchFireMode = canSwitch;
    }

    public void ConfigureStanceOffsets(
        Vector3 aimPos, Vector3 aimRot,
        Vector3 lowReadyPos, Vector3 lowReadyRot,
        Vector3 holsterPos, Vector3 holsterRot)
    {
        aimPosOffset = aimPos;
        aimRotOffset = aimRot;
        lowReadyPosOffset = lowReadyPos;
        lowReadyRotOffset = lowReadyRot;
        holsterPosOffset = holsterPos;
        holsterRotOffset = holsterRot;
    }

    public void InitializeBaseTransform(Vector3 pos, Quaternion rot)
    {
        initialBasePos = pos;
        initialBaseRot = rot;
    }
}
