using UnityEngine;

/// <summary>
/// Контроллер хода затворной рамы (Reciprocating Slide) тактического пистолета.
/// Реализует:
/// 1. Мгновенный откат затвора назад при выстреле (Blowback recoil animation)
/// 2. Пружинный возврат вперед при наличии патронов в магазине
/// 3. Постановку на затворную задержку (Slide-lock) при опустошении магазина
/// 4. Сброс затворной задержки при перезарядке
/// </summary>
public class PistolSlideController : MonoBehaviour
{
    [Header("Параметры хода затвора")]
    [Tooltip("Величина отката затвора назад (м)")]
    [SerializeField] private float blowbackDistance = 0.038f;

    [Tooltip("Скорость отката назад (м/с)")]
    [SerializeField] private float blowbackSpeed = 28f;

    [Tooltip("Скорость возврата пружины вперед (м/с)")]
    [SerializeField] private float returnSpeed = 16f;

    private Vector3 initialLocalPos;
    private float currentZOffset;
    private float targetZOffset;
    private bool isLockedBack;

    public bool IsLockedBack => isLockedBack;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float speed = (currentZOffset > targetZOffset) ? blowbackSpeed : returnSpeed;
        float blend = 1f - Mathf.Exp(-speed * Time.deltaTime);
        currentZOffset = Mathf.Lerp(currentZOffset, targetZOffset, blend);

        // Если затвор почти дошел назад и не на задержке, начинаем возврат вперед
        if (!isLockedBack && Mathf.Abs(currentZOffset - (-blowbackDistance)) < 0.005f)
        {
            targetZOffset = 0f;
        }

        Vector3 pos = initialLocalPos;
        pos.z += currentZOffset;
        transform.localPosition = pos;
    }

    /// <summary>
    /// Вызывается при выстреле.
    /// </summary>
    public void OnFire(bool magazineEmptyAfterShot)
    {
        targetZOffset = -blowbackDistance;
        isLockedBack = magazineEmptyAfterShot;
    }

    /// <summary>
    /// Сброс затворной задержки при перезарядке.
    /// </summary>
    public void ReleaseSlide()
    {
        isLockedBack = false;
        targetZOffset = 0f;
    }

    /// <summary>
    /// Принудительная установка состояния задержки.
    /// </summary>
    public void SetSlideLock(bool locked)
    {
        isLockedBack = locked;
        targetZOffset = locked ? -blowbackDistance : 0f;
    }
}
