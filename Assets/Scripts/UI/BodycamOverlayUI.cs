using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Реалистичный оверлей тактического нагрудного видеорегистратора (Bodycam HUD).
/// Отображает мигающий индикатор записи, таймкод, статус батареи и данные юнита.
/// </summary>
public class BodycamOverlayUI : MonoBehaviour
{
    [Header("UI Элементы")]
    [Tooltip("Текстовое поле для даты и времени")]
    [SerializeField] private Text timestampText;

    [Tooltip("Иконка или текст индикатора записи '● REC'")]
    [SerializeField] private Text recIndicatorText;

    [Tooltip("Текстовое поле метаданных оперативника")]
    [SerializeField] private Text metadataText;

    [Tooltip("Текстовое поле статуса батареи")]
    [SerializeField] private Text batteryText;

    [Header("Настройки")]
    [Tooltip("Частота мигания индикатора REC (Гц)")]
    [SerializeField] private float blinkFrequency = 1.0f;

    [Tooltip("Идентификатор подразделения")]
    [SerializeField] private string unitCallsign = "UNIT-04 // S-MUNAY TAC-1";

    [Tooltip("Код устройства")]
    [SerializeField] private string deviceModel = "AXON BODY 3 WIDE-CAM";

    private float blinkTimer;
    private bool isIndicatorVisible = true;
    private float batteryLevel = 98.4f;

    private void Start()
    {
        if (metadataText != null)
        {
            metadataText.text = $"{unitCallsign}\n{deviceModel}\nSECURE BUFFER 4K/60";
        }
    }

    private void Update()
    {
        UpdateBlink();
        UpdateClock();
        UpdateBattery();
    }

    private void UpdateBlink()
    {
        blinkTimer += Time.deltaTime * blinkFrequency;
        if (blinkTimer >= 1f)
        {
            blinkTimer = 0f;
            isIndicatorVisible = !isIndicatorVisible;

            if (recIndicatorText != null)
            {
                recIndicatorText.color = isIndicatorVisible 
                    ? new Color(0.9f, 0.15f, 0.15f, 0.95f) 
                    : new Color(0.9f, 0.15f, 0.15f, 0.15f);
            }
        }
    }

    private void UpdateClock()
    {
        if (timestampText == null) return;

        DateTime now = DateTime.Now;
        int frame = (int)((Time.time % 1f) * 60f);
        timestampText.text = $"{now:yyyy-MM-dd HH:mm:ss}:{frame:D2} UTC+5";
    }

    private void UpdateBattery()
    {
        if (batteryText == null) return;

        // Медленный разряд батареи со временем
        batteryLevel -= Time.deltaTime * 0.002f;
        if (batteryLevel < 1f) batteryLevel = 100f;

        batteryText.text = $"[BAT: {batteryLevel:F1}% 3.8V]";
    }

    public void SetupUI(Text timeText, Text recText, Text metaText, Text batText)
    {
        timestampText = timeText;
        recIndicatorText = recText;
        metadataText = metaText;
        batteryText = batText;
    }
}
