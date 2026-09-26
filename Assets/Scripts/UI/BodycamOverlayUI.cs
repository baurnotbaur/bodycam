using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Реалистичный оверлей тактического нагрудного видеорегистратора (Bodycam HUD).
/// Отображает:
/// - Мигающий индикатор записи '● REC'
/// - Точный таймкод со счетчиком кадров UTC+5
/// - Тактические GPS-координаты локации (Жезказган / Сатпаев)
/// - Статус активного оружия: название, калибр, режим огня [AUTO]/[SEMI], патроны (30 / 120)
/// - Статус фонаря и стойки бойца
/// - Заряд батареи и идентификатор юнита
/// </summary>
public class BodycamOverlayUI : MonoBehaviour
{
    [Header("UI Текстовые поля")]
    [Tooltip("Поле даты и времени (Top-Right)")]
    [SerializeField] private Text timestampText;

    [Tooltip("Индикатор записи '● REC' (Top-Left)")]
    [SerializeField] private Text recIndicatorText;

    [Tooltip("Метаданные оперативника и GPS (Bottom-Left)")]
    [SerializeField] private Text metadataText;

    [Tooltip("Боевой статус оружия и боезапас (Bottom-Right)")]
    [SerializeField] private Text weaponStatusText;

    [Tooltip("Статус батареи (Top-Right под таймкодом)")]
    [SerializeField] private Text batteryText;

    [Header("Ссылки на игровые системы")]
    [SerializeField] private WeaponInventoryController inventoryController;
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Настройки")]
    [Tooltip("Частота мигания REC (Гц)")]
    [SerializeField] private float blinkFrequency = 1.0f;

    [Tooltip("Позывной подразделения")]
    [SerializeField] private string unitCallsign = "UNIT-04 // S-MUNAY TAC-1";

    [Tooltip("Модель бодикамеры")]
    [SerializeField] private string deviceModel = "AXON BODY 3 WIDE-CAM";

    // GPS Координаты (Жезказган, Казахстан)
    private const string GpsLat = "47°47'28\"N";
    private const string GpsLon = "67°42'15\"E";
    private const float BaseAltitude = 345.2f;

    private float blinkTimer;
    private bool isIndicatorVisible = true;
    private float batteryLevel = 98.4f;

    private void Start()
    {
        FindReferencesIfNull();
    }

    private void FindReferencesIfNull()
    {
        if (inventoryController == null)
        {
            inventoryController = FindAnyObjectByType<WeaponInventoryController>();
        }
        if (inputHandler == null)
        {
            inputHandler = FindAnyObjectByType<PlayerInputHandler>();
        }
    }

    private void Update()
    {
        FindReferencesIfNull();
        UpdateBlink();
        UpdateClock();
        UpdateBattery();
        UpdateMetadata();
        UpdateWeaponStatus();
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
                    ? new Color(0.95f, 0.15f, 0.15f, 0.95f) 
                    : new Color(0.95f, 0.15f, 0.15f, 0.15f);
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

        batteryLevel -= Time.deltaTime * 0.0015f;
        if (batteryLevel < 1f) batteryLevel = 100f;

        batteryText.text = $"[BAT: {batteryLevel:F1}% 3.8V  BUF: OK]";
    }

    private void UpdateMetadata()
    {
        if (metadataText == null) return;

        string stance = (inputHandler != null && inputHandler.IsCrouching) ? "CROUCH" : "STAND";
        string ready;
        if (inventoryController != null && inventoryController.IsSwitching)
        {
            ready = "SWITCHING";
        }
        else if (inventoryController != null && inventoryController.ActiveWeapon != null && inventoryController.ActiveWeapon.IsAiming) 
        {
            ready = "POINT-AIM";
        }
        else if (inventoryController != null && inventoryController.ActiveWeapon != null && inventoryController.ActiveWeapon.IsLowReady)
        {
            ready = "LOW-READY";
        }
        else
        {
            ready = "HIGH-READY";
        }

        metadataText.text = $"{unitCallsign}\n" +
                            $"{deviceModel} // 4K/60FPS HDR\n" +
                            $"GPS: {GpsLat} {GpsLon} ALT: {BaseAltitude:F1}M\n" +
                            $"STANCE: [{stance}] // STAGE: [{ready}]";
    }

    private void UpdateWeaponStatus()
    {
        if (weaponStatusText == null) return;

        if (inventoryController == null || inventoryController.ActiveWeapon == null)
        {
            weaponStatusText.text = "NO WEAPON";
            return;
        }

        RaycastWeapon weapon = inventoryController.ActiveWeapon;

        string name = weapon.WeaponName;
        string caliber = weapon.CaliberName;
        string mode = weapon.CurrentFireMode == RaycastWeapon.FireMode.FullAuto ? "[AUTO]" : "[SEMI]";
        string lightStatus = weapon.IsFlashlightOn ? "[LIGHT: ON]" : "[LIGHT: OFF]";

        string ammoDisplay;
        if (inventoryController.IsSwitching)
        {
            ammoDisplay = "<color=#E8941A>[EQUIPPING...]</color>";
        }
        else if (weapon.IsReloading)
        {
            ammoDisplay = "<color=#E8941A>[RELOADING...]</color>";
        }
        else
        {
            int ammo = weapon.CurrentAmmo;
            int reserve = weapon.ReserveAmmo;
            if (ammo <= 5)
            {
                ammoDisplay = $"<color=#FF3333>{ammo:D2}</color> / {reserve:D3}";
            }
            else
            {
                ammoDisplay = $"{ammo:D2} / {reserve:D3}";
            }
        }

        weaponStatusText.text = $"{name} // {caliber}\n" +
                                $"{mode} {lightStatus}\n" +
                                $"AMMO: {ammoDisplay}";
    }

    public void SetupUI(Text timeText, Text recText, Text metaText, Text batText, Text wepText)
    {
        timestampText = timeText;
        recIndicatorText = recText;
        metadataText = metaText;
        batteryText = batText;
        weaponStatusText = wepText;
    }

    public void SetupReferences(WeaponInventoryController inventory, PlayerInputHandler input)
    {
        inventoryController = inventory;
        inputHandler = input;
    }
}
