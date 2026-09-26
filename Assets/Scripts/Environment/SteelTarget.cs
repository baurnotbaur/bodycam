using System.Collections;
using UnityEngine;

/// <summary>
/// Интерактивная стальная мишень типа IPSC Popper.
/// При попадании пули:
/// - Воспроизводит реалистичный синтезированный звон стальной плиты (Gong)
/// - Мгновенно падает/опрокидывается назад
/// - Высекает сноп металлических искр
/// - Через 4.5 секунды автоматически плавно поднимается обратно в боевую стойку
/// </summary>
public class SteelTarget : MonoBehaviour
{
    [Header("Настройки мишени")]
    [Tooltip("Угол наклона в лежачем положении (градусы)")]
    [SerializeField] private float fallenPitchAngle = 78f;

    [Tooltip("Скорость падения")]
    [SerializeField] private float fallSpeed = 18f;

    [Tooltip("Задержка перед автоподъемом (сек)")]
    [SerializeField] private float autoResetDelay = 4.5f;

    [Tooltip("Скорость подъема мишени")]
    [SerializeField] private float resetSpeed = 8f;

    private Quaternion uprightRotation;
    private Quaternion fallenRotation;
    private bool isDown;
    private float resetTimer;

    public bool IsDown => isDown;

    private void Awake()
    {
        uprightRotation = transform.localRotation;
        fallenRotation = uprightRotation * Quaternion.Euler(fallenPitchAngle, 0f, 0f);
    }

    private void Update()
    {
        if (isDown)
        {
            // Плавное падение назад
            transform.localRotation = Quaternion.Slerp(transform.localRotation, fallenRotation, 1f - Mathf.Exp(-fallSpeed * Time.deltaTime));

            // Таймер сброса
            resetTimer += Time.deltaTime;
            if (resetTimer >= autoResetDelay)
            {
                isDown = false;
                resetTimer = 0f;
                // Звук сброса / взведения защелки
                BodycamAudioEngine.PlaySelectorClick(transform.position);
            }
        }
        else
        {
            // Возврат в вертикальное положение
            transform.localRotation = Quaternion.Slerp(transform.localRotation, uprightRotation, 1f - Mathf.Exp(-resetSpeed * Time.deltaTime));
        }
    }

    /// <summary>
    /// Вызывается системой оружия RaycastWeapon при попадании луча пули.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        // Всегда воспроизводим звон стали при попадании
        BodycamAudioEngine.PlaySteelTargetGong(transform.position);

        if (!isDown)
        {
            isDown = true;
            resetTimer = 0f;
        }
    }
}
