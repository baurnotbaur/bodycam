using UnityEngine;

/// <summary>
/// Контроллер тактического дульного огня (Muzzle Flash).
/// Создает динамическое освещение окружения импульсом света и короткой вспышкой.
/// </summary>
public class MuzzleFlashController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Точечный источник света для освещения стен/пола")]
    [SerializeField] private Light muzzleLight;

    [Tooltip("Партикловая система вспышки или объект меша вспышки")]
    [SerializeField] private ParticleSystem muzzleParticles;

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
    }

    private void Update()
    {
        if (lightTimer > 0f)
        {
            lightTimer -= Time.deltaTime;
            if (lightTimer <= 0f && muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }
        }
    }

    /// <summary>
    /// Активирует вспышку выстрела.
    /// </summary>
    public void TriggerFlash()
    {
        if (muzzleParticles != null)
        {
            muzzleParticles.Play(true);
        }

        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            muzzleLight.intensity = lightIntensity * Random.Range(0.85f, 1.15f);
            lightTimer = flashDuration;
        }
    }

    public void SetupReferences(Light lightComp, ParticleSystem particles)
    {
        muzzleLight = lightComp;
        muzzleParticles = particles;
    }
}
