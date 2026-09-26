using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Тактический инвентарь и переключатель оружия (AK-74 <-> Tactical Pistol).
/// Поддерживает:
/// - Слот 1: АК-74 (клавиша 1)
/// - Слот 2: Tactical Pistol (клавиша 2)
/// - Прокрутку колеса мыши для смены оружия
/// - Процедурную анимацию убирания (Holster) и подъема (Draw) оружия
/// </summary>
public class WeaponInventoryController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Обработчик ввода")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Tooltip("Список доступного оружия")]
    [SerializeField] private List<RaycastWeapon> weapons = new List<RaycastWeapon>();

    [Header("Настройки смены оружия")]
    [Tooltip("Время опускания оружия при смене (сек)")]
    [SerializeField] private float holsterDuration = 0.22f;

    [Tooltip("Время подъема нового оружия (сек)")]
    [SerializeField] private float drawDuration = 0.26f;

    [Tooltip("Величина опускания оружия вниз при кобуре")]
    [SerializeField] private float holsterDropDistance = 0.32f;

    private int activeIndex = 0;
    private bool isSwitching = false;
    private Vector3[] defaultLocalPositions;

    public RaycastWeapon ActiveWeapon => (weapons != null && activeIndex >= 0 && activeIndex < weapons.Count) ? weapons[activeIndex] : null;
    public int ActiveSlotIndex => activeIndex;
    public bool IsSwitching => isSwitching;

    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Start()
    {
        InitializeInventory();
    }

    public void Setup(PlayerInputHandler input, List<RaycastWeapon> weaponList)
    {
        inputHandler = input;
        weapons = weaponList;
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        if (weapons == null || weapons.Count == 0) return;

        defaultLocalPositions = new Vector3[weapons.Count];

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
            {
                defaultLocalPositions[i] = weapons[i].transform.localPosition;
                weapons[i].gameObject.SetActive(i == activeIndex);
            }
        }
    }

    private void Update()
    {
        if (inputHandler == null || isSwitching) return;

        // Не переключаем во время активной перезарядки текущего оружия
        if (ActiveWeapon != null && ActiveWeapon.IsReloading) return;

        // Клавиша 1
        if (inputHandler.Slot1Pressed && activeIndex != 0 && weapons.Count > 0)
        {
            StartCoroutine(SwitchWeaponRoutine(0));
            return;
        }

        // Клавиша 2
        if (inputHandler.Slot2Pressed && activeIndex != 1 && weapons.Count > 1)
        {
            StartCoroutine(SwitchWeaponRoutine(1));
            return;
        }

        // Колесико мыши
        float scroll = inputHandler.ScrollDelta;
        if (Mathf.Abs(scroll) > 0.05f && weapons.Count > 1)
        {
            int nextIndex = activeIndex;
            if (scroll > 0f)
            {
                nextIndex = (activeIndex + 1) % weapons.Count;
            }
            else
            {
                nextIndex = (activeIndex - 1 + weapons.Count) % weapons.Count;
            }

            if (nextIndex != activeIndex)
            {
                StartCoroutine(SwitchWeaponRoutine(nextIndex));
            }
        }
    }

    private IEnumerator SwitchWeaponRoutine(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= weapons.Count || targetIndex == activeIndex) yield break;

        isSwitching = true;
        RaycastWeapon currentWeapon = weapons[activeIndex];
        RaycastWeapon nextWeapon = weapons[targetIndex];

        // 1. Анимация опускания (Holster) текущего оружия
        if (currentWeapon != null && currentWeapon.gameObject.activeSelf)
        {
            Vector3 startPos = currentWeapon.transform.localPosition;
            Vector3 endPos = startPos - new Vector3(0f, holsterDropDistance, 0.05f);
            float elapsed = 0f;

            while (elapsed < holsterDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / holsterDuration);
                currentWeapon.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            currentWeapon.transform.localPosition = defaultLocalPositions[activeIndex];
            currentWeapon.gameObject.SetActive(false);
        }

        // 2. Активация нового оружия из нижнего положения
        activeIndex = targetIndex;
        if (nextWeapon != null)
        {
            nextWeapon.gameObject.SetActive(true);
            Vector3 basePos = defaultLocalPositions[targetIndex];
            Vector3 lowerPos = basePos - new Vector3(0f, holsterDropDistance, 0.05f);
            nextWeapon.transform.localPosition = lowerPos;

            // Звук экипировки / тактического сброса
            BodycamAudioEngine.PlaySelectorClick(nextWeapon.transform.position);

            float elapsed = 0f;
            while (elapsed < drawDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / drawDuration);
                nextWeapon.transform.localPosition = Vector3.Lerp(lowerPos, basePos, t);
                yield return null;
            }

            nextWeapon.transform.localPosition = basePos;
        }

        isSwitching = false;
    }
}
