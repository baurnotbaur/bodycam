using UnityEngine;

/// <summary>
/// Контроллер тактического дульного огня (Muzzle Flash).
/// Создает динамическое освещение окружения импульсом света и короткой вспышкой.
/// Не зависит от внешних тяжелых сборок частиц, что гарантирует мгновенную компиляцию.
/// </summary>
public class MuzzleFlashController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Точечный источник света для освещения стен/пола")]
    [SerializeField] private Light muzzleLight;

    [Tooltip("Объект визуального эффекта вспышки (меш, спрайт или квад)")]
    [SerializeField] private GameObject muzzleFlashVisual;

    [Header("Параметры вспышки")]
    [Tooltip("Длительность свечения точечного источника (секунды)")]
    [SerializeField] private float flashDuration = 0.05f;

    [Tooltip("Максимальная интенсивность света при выстреле")]
    [SerializeField] private float lightIntensity = 4.5f;

    private float lightTimer;

    private void Awake()
    {
        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
            muzzleLight.intensity = lightIntensity;
        }

        if (muzzleFlashVisual != null)
        {
            muzzleFlashVisual.SetActive(false);
        }
    }

    private void Update()
    {
        if (lightTimer > 0f)
        {
            lightTimer -= Time.deltaTime;
            if (lightTimer <= 0f)
            {
                if (muzzleLight != null) muzzleLight.enabled = false;
                if (muzzleFlashVisual != null) muzzleFlashVisual.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Активирует вспышку выстрела.
    /// </summary>
    public void TriggerFlash()
    {
        if (muzzleFlashVisual != null)
        {
            // Случайный поворот вспышки для реалистичности каждого выстрела
            muzzleFlashVisual.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            muzzleFlashVisual.SetActive(true);
        }

        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            muzzleLight.intensity = lightIntensity * Random.Range(0.85f, 1.15f);
        }

        lightTimer = flashDuration;
    }

    public void SetupReferences(Light lightComp, GameObject visualObj = null)
    {
        muzzleLight = lightComp;
        muzzleFlashVisual = visualObj;
    }
}
