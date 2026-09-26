#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Автоматический генератор полноценного прототипа тактического шутера Bodycam в Unity Editor.
/// Создает:
/// 1. Тактический полигон CQB Killhouse с комнатами, проемами, окнами, баррикадами, ящиками и бочками
/// 2. Интерактивные стальные попперы (SteelTarget) со звоном гонга и автоподъемом + силуэтные мишени
/// 3. Игрока с кинематической цепью нагрудной камеры (Bodycam Rig + Lissajous Bobbing + Point-Aim Zoom)
/// 4. Детализированный автомат АК-74 (ДТК-74, ребристая крышка, бакелитовый изогнутый рожок, цевье, коллиматор, ЛЦУ)
/// 5. Детализированный тактический пистолет (затворная рама с блоубэком и затворной задержкой, фонарь)
/// 6. Инвентарь переключения оружия (WeaponInventoryController, клавиши 1, 2, колесо мыши)
/// 7. Физический выброс гильз (ShellCasing) с рикошетом и звоном латуни
/// 8. Процедурный синтезатор звука (BodycamAudioEngine)
/// 9. Тактический оверлей бодикамеры (BodycamOverlayUI) с GPS Жезказгана, таймкодом и счетчиком патронов
/// </summary>
public static class BodycamSceneBuilder
{
    [MenuItem("Tools/Bodycam/Setup Complete Prototype Scene", priority = 10)]
    public static void BuildCompleteScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        // 1. Создаем чистую сцену
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Создаем тактический полигон CQB Killhouse (Стены, Комнаты, Мишени, Пропсы)
        CreateKillhouseEnvironment();

        // 3. Создаем Игрока с двумя детализированными стволами (АК-74 и Пистолет)
        GameObject playerRoot = CreatePlayerWithWeapons();
        WeaponInventoryController inventory = playerRoot.GetComponent<WeaponInventoryController>();
        PlayerInputHandler input = playerRoot.GetComponent<PlayerInputHandler>();

        // 4. Создаем HUD оверлей бодикамеры
        CreateBodycamHUD(inventory, input);

        // 5. Сохраняем сцену и регистрируем в Build Settings
        string scenePath = "Assets/Scenes/BodycamPrototype.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(scenePath);

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        Debug.Log("<color=green><b>[Bodycam] Сцена CQB Killhouse с АК-74 и пистолетом успешно собрана: Assets/Scenes/BodycamPrototype.unity!</b></color>");
    }

    // =========================================================================
    // ОКРУЖЕНИЕ: CQB KILLHOUSE ТРЕНИРОВОЧНЫЙ КОМПЛЕКС
    // =========================================================================
    private static void CreateKillhouseEnvironment()
    {
        // 1. Освещение сцены
        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.95f;
        light.color = new Color(0.92f, 0.94f, 0.98f);
        sun.transform.rotation = Quaternion.Euler(42f, -30f, 0f);

        // Потолочные прожекторы полигона (теплый свет для атмосферы бодикама)
        CreateCeilingLamp(new Vector3(0f, 4.6f, -4f));
        CreateCeilingLamp(new Vector3(0f, 4.6f, 10f));
        CreateCeilingLamp(new Vector3(-8f, 4.6f, 18f));
        CreateCeilingLamp(new Vector3(8f, 4.6f, 22f));

        // 2. Пол полигона (Бетон с разметкой)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Concrete_Arena";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(6f, 1f, 7f); // 60x70 метров

        Material floorMat = CreateLitMaterial(new Color(0.24f, 0.25f, 0.28f), 0.2f, 0.4f);
        floor.GetComponent<Renderer>().material = floorMat;

        // Желтые тактические линии безопасности на полу
        CreateFloorStripe(new Vector3(0f, 0.01f, -10f), new Vector3(14f, 0.01f, 0.15f), new Color(0.85f, 0.70f, 0.15f));
        CreateFloorStripe(new Vector3(0f, 0.01f, 4f), new Vector3(20f, 0.01f, 0.15f), new Color(0.85f, 0.70f, 0.15f));

        // 3. Внешний периметр полигона (Капитальные бетонные стены 5м)
        Color outerWallColor = new Color(0.20f, 0.21f, 0.23f);
        CreateWall(new Vector3(0f, 2.5f, 35f), new Vector3(60f, 5f, 1.2f), outerWallColor);
        CreateWall(new Vector3(0f, 2.5f, -25f), new Vector3(60f, 5f, 1.2f), outerWallColor);
        CreateWall(new Vector3(25f, 2.5f, 5f), new Vector3(1.2f, 5f, 60f), outerWallColor);
        CreateWall(new Vector3(-25f, 2.5f, 5f), new Vector3(1.2f, 5f, 60f), outerWallColor);

        // 4. CQB Killhouse: Внутренние тренировочные комнаты и перегородки
        Color plywoodColor = new Color(0.55f, 0.45f, 0.32f); // Тактическая фанера
        Color partitionColor = new Color(0.32f, 0.34f, 0.37f); // Шлакоблок

        // Комната 1: Breach Room (Зона входа)
        CreateWall(new Vector3(-4.5f, 1.6f, -3f), new Vector3(6f, 3.2f, 0.35f), partitionColor);
        CreateWall(new Vector3(4.5f, 1.6f, -3f), new Vector3(6f, 3.2f, 0.35f), partitionColor);
        // Дверной проем шириной 1.5м по центру (Z = -3)

        // Комната 2: Central Shoot-House Hall
        CreateWall(new Vector3(-7.5f, 1.6f, 8f), new Vector3(0.35f, 3.2f, 12f), partitionColor);
        CreateWall(new Vector3(7.5f, 1.6f, 8f), new Vector3(0.35f, 3.2f, 12f), partitionColor);

        // Фанерные тактические укрытия (Barricades)
        CreateWall(new Vector3(-2.2f, 1.1f, 2f), new Vector3(2.4f, 2.2f, 0.15f), plywoodColor);
        CreateWall(new Vector3(2.2f, 1.1f, 6f), new Vector3(2.4f, 2.2f, 0.15f), plywoodColor);
        CreateWall(new Vector3(-1.5f, 0.6f, 11f), new Vector3(3.2f, 1.2f, 0.25f), plywoodColor); // Низкий парапет

        // Оконный проем в стене коридора
        CreateWall(new Vector3(2.5f, 0.6f, 14f), new Vector3(5f, 1.2f, 0.35f), partitionColor); // Нижняя часть окна
        CreateWall(new Vector3(2.5f, 2.7f, 14f), new Vector3(5f, 1.0f, 0.35f), partitionColor); // Верхняя часть окна
        CreateWall(new Vector3(-3.5f, 1.6f, 14f), new Vector3(7f, 3.2f, 0.35f), partitionColor); // Глухая часть

        // Комната 3: Deep Target Room (Z: 16 - 28)
        CreateWall(new Vector3(-14f, 1.6f, 20f), new Vector3(0.35f, 3.2f, 14f), partitionColor);
        CreateWall(new Vector3(14f, 1.6f, 20f), new Vector3(0.35f, 3.2f, 14f), partitionColor);
        CreateWall(new Vector3(0f, 1.6f, 28f), new Vector3(20f, 3.2f, 0.35f), partitionColor);

        // 5. Интерактивные стальные попперы (SteelTarget) со звуком гонга
        // Линейка ближних попперов в центральном зале
        CreateInteractiveSteelPopper(new Vector3(-3.5f, 0f, 9f));
        CreateInteractiveSteelPopper(new Vector3(3.5f, 0f, 9f));
        CreateInteractiveSteelPopper(new Vector3(0f, 0f, 13.5f));

        // Линейка дальних попперов в глубине комнаты
        CreateInteractiveSteelPopper(new Vector3(-6f, 0f, 22f));
        CreateInteractiveSteelPopper(new Vector3(-2f, 0f, 24f));
        CreateInteractiveSteelPopper(new Vector3(2f, 0f, 24f));
        CreateInteractiveSteelPopper(new Vector3(6f, 0f, 22f));

        // 6. Силуэтные ростовые мишени с зонами поражения (ShootingTarget)
        CreateSilhouetteTarget(new Vector3(-4.5f, 0f, 4f));
        CreateSilhouetteTarget(new Vector3(4.5f, 0f, 7f));
        CreateSilhouetteTarget(new Vector3(-8f, 0f, 19f));
        CreateSilhouetteTarget(new Vector3(9f, 0f, 23f));

        // 7. Физические пропсы: тактические бочки и деревянные ящики
        CreatePhysicsBarrel(new Vector3(-1.5f, 0.5f, 0.5f), new Color(0.2f, 0.35f, 0.6f)); // Синяя бочка
        CreatePhysicsBarrel(new Vector3(-0.9f, 0.5f, 1.1f), new Color(0.7f, 0.15f, 0.15f)); // Красная бочка
        CreatePhysicsBarrel(new Vector3(4.5f, 0.5f, 11f), new Color(0.7f, 0.15f, 0.15f));

        CreatePhysicsCrate(new Vector3(3.2f, 0.5f, 1.5f), new Vector3(1f, 1f, 1f));
        CreatePhysicsCrate(new Vector3(3.2f, 1.4f, 1.5f), new Vector3(0.8f, 0.8f, 0.8f));
        CreatePhysicsCrate(new Vector3(-5.5f, 0.5f, 12f), new Vector3(1.2f, 1f, 1.2f));
    }

    private static void CreateCeilingLamp(Vector3 pos)
    {
        GameObject lamp = new GameObject("Ceiling_Lamp");
        lamp.transform.position = pos;

        // Корпус светильника
        GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = "Fixture";
        fixture.transform.SetParent(lamp.transform, false);
        fixture.transform.localScale = new Vector3(1.6f, 0.12f, 0.4f);
        Collider col = fixture.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        fixture.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.18f, 0.18f, 0.18f), 0.5f, 0.5f);

        // Источник света
        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 16f;
        light.intensity = 2.4f;
        light.color = new Color(1f, 0.94f, 0.86f);
    }

    private static void CreateWall(Vector3 pos, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall_Structure";
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = CreateLitMaterial(color, 0.05f, 0.25f);
    }

    private static void CreateFloorStripe(Vector3 pos, Vector3 scale, Color color)
    {
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Stripe_Demarcation";
        stripe.transform.position = pos;
        stripe.transform.localScale = scale;
        Collider col = stripe.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        stripe.GetComponent<Renderer>().material = CreateLitMaterial(color, 0.1f, 0.6f);
    }

    private static void CreateInteractiveSteelPopper(Vector3 pos)
    {
        GameObject popperRoot = new GameObject("Steel_Popper_Target");
        popperRoot.transform.position = pos;

        // 1. Массивное стальное основание
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Popper_Base";
        baseObj.transform.SetParent(popperRoot.transform, false);
        baseObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        baseObj.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
        baseObj.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.18f, 0.19f, 0.21f), 0.7f, 0.4f);

        // 2. Подвижная пластина мишени (Hinged Plate)
        GameObject plateHinge = new GameObject("Popper_Plate");
        plateHinge.transform.SetParent(popperRoot.transform, false);
        plateHinge.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        // Штанга поппера
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stem.name = "Popper_Stem";
        stem.transform.SetParent(plateHinge.transform, false);
        stem.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        stem.transform.localScale = new Vector3(0.12f, 0.9f, 0.035f);

        // Круглая голова поппера (IPSC round head)
        GameObject roundHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        roundHead.name = "Popper_Head";
        roundHead.transform.SetParent(plateHinge.transform, false);
        roundHead.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        roundHead.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        roundHead.transform.localScale = new Vector3(0.38f, 0.018f, 0.38f);

        // Яркий контрастный белый цвет стального поппера с темным кантом
        Material steelMat = CreateLitMaterial(new Color(0.92f, 0.92f, 0.94f), 0.85f, 0.65f);
        stem.GetComponent<Renderer>().material = steelMat;
        roundHead.GetComponent<Renderer>().material = steelMat;

        // Коллайдер пластины
        BoxCollider plateCol = plateHinge.AddComponent<BoxCollider>();
        plateCol.center = new Vector3(0f, 0.65f, 0f);
        plateCol.size = new Vector3(0.42f, 1.1f, 0.08f);

        // Скрипт стальной мишени (падает при попадании, воспроизводит гонг, поднимается через 4.5с)
        plateHinge.AddComponent<SteelTarget>();
    }

    private static void CreateSilhouetteTarget(Vector3 pos)
    {
        GameObject targetRoot = new GameObject("Silhouette_Human_Target");
        targetRoot.transform.position = pos;

        // Деревянная стойка (Stand)
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Target_Stand";
        stand.transform.SetParent(targetRoot.transform, false);
        stand.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        stand.transform.localScale = new Vector3(0.08f, 1.3f, 0.08f);
        stand.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.48f, 0.36f, 0.22f), 0.05f, 0.2f);

        Material cardboardMat = CreateLitMaterial(new Color(0.82f, 0.76f, 0.65f), 0.0f, 0.15f);

        // Торс (Chest zone)
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "Torso_Zone";
        torso.transform.SetParent(targetRoot.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        torso.transform.localScale = new Vector3(0.45f, 0.65f, 0.04f);
        torso.GetComponent<Renderer>().material = cardboardMat;
        ShootingTarget torsoTarget = torso.AddComponent<ShootingTarget>();
        torsoTarget.SetHitZone(ShootingTarget.HitZone.Chest);

        // Голова (Head zone)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head_Zone";
        head.transform.SetParent(targetRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.80f, 0f);
        head.transform.localScale = new Vector3(0.22f, 0.25f, 0.04f);
        head.GetComponent<Renderer>().material = cardboardMat;
        ShootingTarget headTarget = head.AddComponent<ShootingTarget>();
        headTarget.SetHitZone(ShootingTarget.HitZone.Head);
    }

    private static void CreatePhysicsBarrel(Vector3 pos, Color col)
    {
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Physics_Barrel";
        barrel.transform.position = pos;
        barrel.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f); // Высота 1м

        Material barrelMat = CreateLitMaterial(col, 0.75f, 0.55f);
        barrel.GetComponent<Renderer>().material = barrelMat;

        Rigidbody rb = barrel.AddComponent<Rigidbody>();
        rb.mass = 28f;
    }

    private static void CreatePhysicsCrate(Vector3 pos, Vector3 size)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "Physics_Crate";
        crate.transform.position = pos;
        crate.transform.localScale = size;

        Material woodMat = CreateLitMaterial(new Color(0.46f, 0.35f, 0.22f), 0.05f, 0.25f);
        crate.GetComponent<Renderer>().material = woodMat;

        Rigidbody rb = crate.AddComponent<Rigidbody>();
        rb.mass = 18f;
    }

    // =========================================================================
    // ИГРОК, БОДИКАМ, ИНВЕНТАРЬ И ОРУЖИЕ
    // =========================================================================
    private static GameObject CreatePlayerWithWeapons()
    {
        // 1. Корневой объект игрока
        GameObject player = new GameObject("Player_Root");
        player.transform.position = new Vector3(0f, 0f, -12f); // Стартовая позиция перед входом в Killhouse

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        BodycamPlayerController movement = player.AddComponent<BodycamPlayerController>();

        // 2. Точка крепления камеры на бронежилете (Chest Mount)
        GameObject chestMount = new GameObject("Chest_Mount_Pivot");
        chestMount.transform.SetParent(player.transform, false);
        chestMount.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        movement.SetChestMountPivot(chestMount.transform);

        // 3. Звуковой движок (BodycamAudioEngine)
        GameObject audioObj = new GameObject("Audio_Engine");
        audioObj.transform.SetParent(player.transform, false);
        audioObj.AddComponent<BodycamAudioEngine>();

        // 4. Camera Rig (подвес камеры, инерция, Pitch, боббинг Лиссажу, FOV Zoom)
        GameObject cameraRig = new GameObject("Camera_Rig");
        cameraRig.transform.SetParent(player.transform, false);
        cameraRig.transform.position = chestMount.transform.position;

        BodycamCameraRig camRigComp = cameraRig.AddComponent<BodycamCameraRig>();
        camRigComp.SetupReferences(chestMount.transform, movement, input);

        // Main Camera
        GameObject mainCam = new GameObject("Main Camera");
        mainCam.transform.SetParent(cameraRig.transform, false);
        Camera cam = mainCam.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 92f;
        cam.nearClipPlane = 0.03f;
        mainCam.AddComponent<AudioListener>();

        // 5. Weapon Rig Pivot (свободный прицел Deadzone Aiming с зажатием до 3° при ПКМ)
        GameObject weaponPivot = new GameObject("Weapon_Rig_Pivot");
        weaponPivot.transform.SetParent(cameraRig.transform, false);
        weaponPivot.transform.localPosition = new Vector3(0.15f, -0.15f, 0.38f);
        weaponPivot.transform.localRotation = Quaternion.Euler(1.5f, -2.0f, 0f);

        DeadzoneAimController aimController = player.AddComponent<DeadzoneAimController>();
        aimController.SetupReferences(input, movement, weaponPivot.transform);

        // 6. Уровень Sway: инерция веса оружия и шаги
        GameObject weaponSwayObj = new GameObject("Weapon_Sway");
        weaponSwayObj.transform.SetParent(weaponPivot.transform, false);
        ProceduralWeaponSway sway = weaponSwayObj.AddComponent<ProceduralWeaponSway>();
        sway.SetupReferences(input, movement);

        // 7. Уровень Recoil Springs: гармонические пружины отдачи
        GameObject weaponRecoilObj = new GameObject("Weapon_Recoil");
        weaponRecoilObj.transform.SetParent(weaponSwayObj.transform, false);
        WeaponRecoilSpring recoil = weaponRecoilObj.AddComponent<WeaponRecoilSpring>();

        // 8. Создание двух видов оружия: Слот 1 — АК-74, Слот 2 — Тактический пистолет
        RaycastWeapon ak74Weapon = BuildDetailedAK74(weaponRecoilObj.transform, input, recoil);
        RaycastWeapon pistolWeapon = BuildDetailedTacticalPistol(weaponRecoilObj.transform, input, recoil);

        // 9. Контроллер тактического инвентаря
        WeaponInventoryController inventory = player.AddComponent<WeaponInventoryController>();
        inventory.Setup(input, new List<RaycastWeapon> { ak74Weapon, pistolWeapon });

        return player;
    }

    // =========================================================================
    // МОДЕЛИРОВАНИЕ: ДЕТАЛИЗИРОВАННЫЙ АК-74 (5.45x39)
    // =========================================================================
    private static RaycastWeapon BuildDetailedAK74(Transform parent, PlayerInputHandler input, WeaponRecoilSpring recoil)
    {
        GameObject akObj = new GameObject("Weapon_AK74");
        akObj.transform.SetParent(parent, false);

        // Материалы АК-74
        Material steelBodyMat = CreateLitMaterial(new Color(0.12f, 0.13f, 0.14f), 0.85f, 0.65f); // Вороненая сталь
        Material barrelSteelMat = CreateLitMaterial(new Color(0.09f, 0.10f, 0.11f), 0.90f, 0.70f); // Ствол
        Material dtkMat = CreateLitMaterial(new Color(0.08f, 0.08f, 0.09f), 0.92f, 0.60f); // ДТК-74
        Material plumWoodMat = CreateLitMaterial(new Color(0.24f, 0.12f, 0.11f), 0.10f, 0.42f); // Сливовый полиамид / фанера
        Material bakeliteMagMat = CreateLitMaterial(new Color(0.38f, 0.17f, 0.10f), 0.12f, 0.50f); // Бакелитовый рожок
        Material opticBodyMat = CreateLitMaterial(new Color(0.14f, 0.15f, 0.16f), 0.80f, 0.55f);

        // 1. Ствольная коробка (Receiver)
        CreatePart(akObj.transform, "Receiver", new Vector3(0f, 0f, 0f), new Vector3(0.052f, 0.085f, 0.35f), steelBodyMat);

        // 2. Ребристая крышка ствольной коробки (Ribbed Dust Cover)
        CreatePart(akObj.transform, "Dust_Cover", new Vector3(0f, 0.048f, -0.01f), new Vector3(0.046f, 0.026f, 0.32f), steelBodyMat);
        // Выраженные ребра жесткости крышки
        for (int i = -3; i <= 3; i++)
        {
            CreatePart(akObj.transform, $"Rib_{i}", new Vector3(0f, 0.062f, -0.01f + i * 0.038f), new Vector3(0.048f, 0.008f, 0.012f), steelBodyMat);
        }

        // 3. Рукоятка взведения затвора (Right-side Bolt Handle)
        CreatePart(akObj.transform, "Bolt_Handle", new Vector3(0.036f, 0.028f, 0.05f), new Vector3(0.028f, 0.014f, 0.035f), steelBodyMat);

        // 4. Скоба спуска и спусковой крючок
        CreatePart(akObj.transform, "Trigger_Guard", new Vector3(0f, -0.055f, -0.03f), new Vector3(0.024f, 0.025f, 0.07f), steelBodyMat);
        CreatePart(akObj.transform, "Trigger", new Vector3(0f, -0.048f, -0.025f), new Vector3(0.012f, 0.022f, 0.015f), steelBodyMat);

        // 5. Пистолетная рукоятка (Pistol Grip)
        GameObject grip = CreatePart(akObj.transform, "Pistol_Grip", new Vector3(0f, -0.105f, -0.10f), new Vector3(0.042f, 0.13f, 0.065f), plumWoodMat);
        grip.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

        // 6. Приклад (Buttstock)
        GameObject stock = CreatePart(akObj.transform, "Stock", new Vector3(0f, -0.018f, -0.32f), new Vector3(0.042f, 0.095f, 0.30f), plumWoodMat);
        stock.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
        CreatePart(akObj.transform, "Buttplate", new Vector3(0f, -0.018f, -0.47f), new Vector3(0.044f, 0.10f, 0.018f), steelBodyMat);

        // 7. Изогнутый 30-зарядный бакелитовый магазин (Curved 5.45x39 Magazine)
        GameObject magSeg1 = CreatePart(akObj.transform, "Mag_Seg1", new Vector3(0f, -0.085f, 0.065f), new Vector3(0.038f, 0.09f, 0.082f), bakeliteMagMat);
        magSeg1.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        GameObject magSeg2 = CreatePart(akObj.transform, "Mag_Seg2", new Vector3(0f, -0.165f, 0.088f), new Vector3(0.036f, 0.09f, 0.080f), bakeliteMagMat);
        magSeg2.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        GameObject magSeg3 = CreatePart(akObj.transform, "Mag_Seg3", new Vector3(0f, -0.245f, 0.125f), new Vector3(0.034f, 0.09f, 0.078f), bakeliteMagMat);
        magSeg3.transform.localRotation = Quaternion.Euler(26f, 0f, 0f);

        // 8. Цевье и ствольная накладка (Handguard & Gas Tube Cover)
        CreatePart(akObj.transform, "Handguard_Lower", new Vector3(0f, -0.008f, 0.28f), new Vector3(0.054f, 0.060f, 0.22f), plumWoodMat);
        CreatePart(akObj.transform, "Handguard_Upper", new Vector3(0f, 0.036f, 0.28f), new Vector3(0.046f, 0.038f, 0.20f), plumWoodMat);

        // 9. Газоотвод, стойка мушки и ствол
        CreatePart(akObj.transform, "Gas_Block", new Vector3(0f, 0.035f, 0.42f), new Vector3(0.032f, 0.055f, 0.06f), barrelSteelMat);
        CreatePart(akObj.transform, "Barrel", new Vector3(0f, 0.008f, 0.46f), new Vector3(0.024f, 0.024f, 0.28f), barrelSteelMat);

        // Прицельная планка (целик) и стойка мушки
        CreatePart(akObj.transform, "Rear_Sight_Base", new Vector3(0f, 0.055f, 0.165f), new Vector3(0.032f, 0.025f, 0.06f), steelBodyMat);
        CreatePart(akObj.transform, "Front_Sight_Post", new Vector3(0f, 0.056f, 0.54f), new Vector3(0.026f, 0.065f, 0.035f), barrelSteelMat);

        // 10. Характерный двухкамерный цилиндрический дульный тормоз-компенсатор ДТК-74
        GameObject dtkRoot = new GameObject("DTK74_Muzzle_Brake");
        dtkRoot.transform.SetParent(akObj.transform, false);
        dtkRoot.transform.localPosition = new Vector3(0f, 0.008f, 0.63f);

        // Камера 1: задняя камера расширения (увеличенный диаметр цилиндра)
        GameObject dtkChamber1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dtkChamber1.name = "DTK74_Expansion_Chamber";
        dtkChamber1.transform.SetParent(dtkRoot.transform, false);
        dtkChamber1.transform.localPosition = new Vector3(0f, 0f, -0.024f);
        dtkChamber1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dtkChamber1.transform.localScale = new Vector3(0.038f, 0.022f, 0.038f);
        Object.DestroyImmediate(dtkChamber1.GetComponent<Collider>());
        dtkChamber1.GetComponent<Renderer>().material = dtkMat;

        // Разделительная диафрагма / ребро-кольцо между камерами
        GameObject dtkBaffle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dtkBaffle.name = "DTK74_Baffle_Collar";
        dtkBaffle.transform.SetParent(dtkRoot.transform, false);
        dtkBaffle.transform.localPosition = new Vector3(0f, 0f, 0f);
        dtkBaffle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dtkBaffle.transform.localScale = new Vector3(0.040f, 0.006f, 0.040f);
        Object.DestroyImmediate(dtkBaffle.GetComponent<Collider>());
        dtkBaffle.GetComponent<Renderer>().material = dtkMat;

        // Камера 2: передняя компенсационная камера с боковыми окнами
        GameObject dtkChamber2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dtkChamber2.name = "DTK74_Compensation_Chamber";
        dtkChamber2.transform.SetParent(dtkRoot.transform, false);
        dtkChamber2.transform.localPosition = new Vector3(0f, 0f, 0.024f);
        dtkChamber2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dtkChamber2.transform.localScale = new Vector3(0.036f, 0.020f, 0.036f);
        Object.DestroyImmediate(dtkChamber2.GetComponent<Collider>());
        dtkChamber2.GetComponent<Renderer>().material = dtkMat;

        // Боковые компенсационные вырезы/окна ДТК-74 (левое и правое)
        CreatePart(dtkRoot.transform, "DTK74_Port_L", new Vector3(-0.019f, 0f, 0.024f), new Vector3(0.005f, 0.016f, 0.022f), steelBodyMat);
        CreatePart(dtkRoot.transform, "DTK74_Port_R", new Vector3(0.019f, 0f, 0.024f), new Vector3(0.005f, 0.016f, 0.022f), steelBodyMat);
        // Верхний компенсационный вырез для гашения подброса ствола
        CreatePart(dtkRoot.transform, "DTK74_Top_Port", new Vector3(0f, 0.019f, 0.024f), new Vector3(0.012f, 0.005f, 0.016f), steelBodyMat);

        // 11. Боковая планка «ласточкин хвост» и коллиматорный прицел (Side Mount + Reflex Sight)
        CreatePart(akObj.transform, "Dovetail_Mount", new Vector3(-0.028f, 0.045f, 0.02f), new Vector3(0.012f, 0.09f, 0.07f), opticBodyMat);
        CreatePart(akObj.transform, "Sight_Housing", new Vector3(0f, 0.095f, 0.02f), new Vector3(0.046f, 0.055f, 0.10f), opticBodyMat);
        // Красная прицельная марка
        CreatePart(akObj.transform, "Red_Dot_Reticle", new Vector3(0f, 0.105f, 0.02f), new Vector3(0.010f, 0.010f, 0.004f), CreateUnlitMaterial(new Color(1f, 0.1f, 0.1f, 1f)));

        // 12. Точка вылета пули (Muzzle) и контроллер вспышки
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(akObj.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.008f, 0.68f);

        GameObject flashLightObj = new GameObject("Muzzle_Light");
        flashLightObj.transform.SetParent(muzzle.transform, false);
        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 16f;
        flashLight.color = new Color(1f, 0.88f, 0.55f);
        flashLight.intensity = 5.5f;
        flashLight.enabled = false;

        MuzzleFlashController flashCtrl = muzzle.AddComponent<MuzzleFlashController>();
        flashCtrl.SetupReferences(flashLight, null);

        // 13. Окно экстракции гильз (Ejection Port)
        GameObject ejectionPort = new GameObject("EjectionPort");
        ejectionPort.transform.SetParent(akObj.transform, false);
        ejectionPort.transform.localPosition = new Vector3(0.032f, 0.035f, 0.06f);
        ejectionPort.transform.localRotation = Quaternion.Euler(-15f, 45f, 0f);

        // 14. Зеленый тактический лазер (Laser Sight)
        GameObject laserObj = new GameObject("Laser_Sight");
        laserObj.transform.SetParent(akObj.transform, false);
        laserObj.transform.localPosition = new Vector3(0.034f, -0.01f, 0.38f);

        laserObj.AddComponent<LineRenderer>();
        LaserSight laserSight = laserObj.AddComponent<LaserSight>();

        GameObject laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        laserDot.name = "Laser_Dot";
        laserDot.transform.SetParent(laserObj.transform, false);
        Collider dotCol = laserDot.GetComponent<Collider>();
        if (dotCol != null) Object.DestroyImmediate(dotCol);
        laserDot.transform.localScale = Vector3.one * 0.035f;
        laserDot.GetComponent<Renderer>().material = CreateUnlitMaterial(new Color(0.1f, 1f, 0.25f, 1f));
        laserSight.SetupDot(laserDot.transform);

        // 15. Тактический подствольный фонарь
        GameObject torchObj = new GameObject("Tactical_Flashlight");
        torchObj.transform.SetParent(akObj.transform, false);
        torchObj.transform.localPosition = new Vector3(-0.035f, -0.01f, 0.38f);
        Light torch = torchObj.AddComponent<Light>();
        torch.type = LightType.Spot;
        torch.range = 35f;
        torch.spotAngle = 48f;
        torch.intensity = 3.8f;
        torch.color = new Color(0.95f, 0.96f, 1.0f);
        torch.enabled = false;

        // 16. Скрипт огнестрельного оружия
        RaycastWeapon weapon = akObj.AddComponent<RaycastWeapon>();
        weapon.ConfigureWeaponSpecs(
            RaycastWeapon.WeaponType.AK74,
            "AK-74 TAC",
            "5.45x39 MM",
            magCap: 30,
            current: 30,
            reserve: 120,
            rpm: 650f,
            dmg: 42f,
            mode: RaycastWeapon.FireMode.FullAuto,
            canSwitch: true
        );
        weapon.ConfigureStanceOffsets(
            aimPos: new Vector3(-0.06f, 0.04f, 0.06f),
            aimRot: new Vector3(-1.0f, 1.5f, 2.0f),
            lowReadyPos: new Vector3(0.02f, -0.14f, -0.05f),
            lowReadyRot: new Vector3(25f, -15f, 10f),
            holsterPos: new Vector3(0.04f, -0.32f, -0.06f),
            holsterRot: new Vector3(22f, -12f, 8f)
        );
        weapon.SetupReferences(input, muzzle.transform, ejectionPort.transform, recoil, flashCtrl, torch, null);

        return weapon;
    }

    // =========================================================================
    // МОДЕЛИРОВАНИЕ: ТАКТИЧЕСКИЙ ПИСТОЛЕТ (9x19) С БЛОУБЭКОМ И СЛАЙД-ЛОКОМ
    // =========================================================================
    private static RaycastWeapon BuildDetailedTacticalPistol(Transform parent, PlayerInputHandler input, WeaponRecoilSpring recoil)
    {
        GameObject pistolObj = new GameObject("Weapon_TacticalPistol");
        pistolObj.transform.SetParent(parent, false);
        pistolObj.transform.localPosition = new Vector3(0.02f, 0.03f, 0.10f); // Естественное удержание двумя руками вперед

        // Материалы пистолета
        Material polymerFrameMat = CreateLitMaterial(new Color(0.13f, 0.13f, 0.14f), 0.12f, 0.35f); // Полимерная рамка
        Material steelSlideMat = CreateLitMaterial(new Color(0.20f, 0.21f, 0.23f), 0.88f, 0.72f); // Затворная рама
        Material barrelMat = CreateLitMaterial(new Color(0.28f, 0.29f, 0.31f), 0.90f, 0.80f); // Стальной ствол
        Material tritiumMat = CreateUnlitMaterial(new Color(0.3f, 1f, 0.4f, 1f)); // Светящиеся тритиевые точки

        // 1. Полимерная рамка (Polymer Lower Frame)
        // Рукоятка
        GameObject grip = CreatePart(pistolObj.transform, "Frame_Grip", new Vector3(0f, -0.085f, -0.045f), new Vector3(0.034f, 0.125f, 0.055f), polymerFrameMat);
        grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

        // Текстурированные боковые накладки рукоятки (Textured Grip Panels)
        Material gripTextureMat = CreateLitMaterial(new Color(0.09f, 0.09f, 0.10f), 0.08f, 0.20f);
        CreatePart(grip.transform, "Grip_Panel_L", new Vector3(-0.018f, 0f, 0f), new Vector3(0.004f, 0.095f, 0.044f), gripTextureMat);
        CreatePart(grip.transform, "Grip_Panel_R", new Vector3(0.018f, 0f, 0f), new Vector3(0.004f, 0.095f, 0.044f), gripTextureMat);
        // Передние подпальцевые выемки (Finger Grooves)
        CreatePart(grip.transform, "Finger_Groove_1", new Vector3(0f, 0.022f, 0.028f), new Vector3(0.032f, 0.018f, 0.006f), polymerFrameMat);
        CreatePart(grip.transform, "Finger_Groove_2", new Vector3(0f, -0.016f, 0.028f), new Vector3(0.032f, 0.018f, 0.006f), polymerFrameMat);

        // Хвостовик типа «бобровый хвост» (Beavertail)
        CreatePart(pistolObj.transform, "Beavertail", new Vector3(0f, -0.015f, -0.09f), new Vector3(0.032f, 0.022f, 0.045f), polymerFrameMat);

        // Пятка магазина (Magazine Basepad)
        GameObject basepad = CreatePart(pistolObj.transform, "Mag_Basepad", new Vector3(0f, -0.150f, -0.025f), new Vector3(0.036f, 0.018f, 0.062f), polymerFrameMat);
        basepad.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

        // Спусковая скоба и крючок
        CreatePart(pistolObj.transform, "Trigger_Guard", new Vector3(0f, -0.042f, 0.018f), new Vector3(0.022f, 0.024f, 0.06f), polymerFrameMat);
        CreatePart(pistolObj.transform, "Pistol_Trigger", new Vector3(0f, -0.035f, 0.014f), new Vector3(0.010f, 0.020f, 0.014f), polymerFrameMat);

        // Подствольная планка Пикатинни (Accessory Rail)
        CreatePart(pistolObj.transform, "Picatinny_Rail", new Vector3(0f, -0.012f, 0.055f), new Vector3(0.032f, 0.024f, 0.085f), polymerFrameMat);

        // 2. Подвижная стальная затворная рама (Reciprocating Slide)
        GameObject slideObj = new GameObject("Pistol_Slide");
        slideObj.transform.SetParent(pistolObj.transform, false);
        slideObj.transform.localPosition = new Vector3(0f, 0.024f, 0.012f);
        PistolSlideController slideCtrl = slideObj.AddComponent<PistolSlideController>();

        // Основное тело затвора
        CreatePart(slideObj.transform, "Slide_Body", new Vector3(0f, 0f, 0f), new Vector3(0.034f, 0.038f, 0.20f), steelSlideMat);

        // Насечки взвода на задней и передней части затвора
        CreatePart(slideObj.transform, "Rear_Serrations", new Vector3(0f, 0.002f, -0.065f), new Vector3(0.036f, 0.034f, 0.035f), steelSlideMat);
        CreatePart(slideObj.transform, "Front_Serrations", new Vector3(0f, 0.002f, 0.065f), new Vector3(0.036f, 0.034f, 0.035f), steelSlideMat);

        // Окно экстракции в затворе
        CreatePart(slideObj.transform, "Ejection_Cutout", new Vector3(0.012f, 0.006f, 0.015f), new Vector3(0.016f, 0.024f, 0.045f), barrelMat);

        // Ночные 3-точечные тактические прицелы (Combat 3-Dot Night Sights)
        CreatePart(slideObj.transform, "Rear_Sight_Notch", new Vector3(0f, 0.026f, -0.088f), new Vector3(0.028f, 0.016f, 0.018f), steelSlideMat);
        CreatePart(slideObj.transform, "Rear_Dot_L", new Vector3(-0.009f, 0.028f, -0.086f), new Vector3(0.004f, 0.004f, 0.004f), tritiumMat);
        CreatePart(slideObj.transform, "Rear_Dot_R", new Vector3(0.009f, 0.028f, -0.086f), new Vector3(0.004f, 0.004f, 0.004f), tritiumMat);

        CreatePart(slideObj.transform, "Front_Sight_Post", new Vector3(0f, 0.026f, 0.088f), new Vector3(0.010f, 0.016f, 0.018f), steelSlideMat);
        CreatePart(slideObj.transform, "Front_Dot", new Vector3(0f, 0.028f, 0.086f), new Vector3(0.004f, 0.004f, 0.004f), tritiumMat);

        // 3. Стальной ствол внутри затвора
        CreatePart(pistolObj.transform, "Pistol_Barrel", new Vector3(0f, 0.024f, 0.04f), new Vector3(0.018f, 0.018f, 0.15f), barrelMat);

        // 4. Подствольный тактический фонарь
        GameObject torchObj = new GameObject("Pistol_Flashlight");
        torchObj.transform.SetParent(pistolObj.transform, false);
        torchObj.transform.localPosition = new Vector3(0f, -0.038f, 0.065f);

        // Корпус фонаря
        CreatePart(torchObj.transform, "Light_Housing", new Vector3(0f, 0f, 0f), new Vector3(0.032f, 0.028f, 0.065f), steelSlideMat);
        Light torch = torchObj.AddComponent<Light>();
        torch.type = LightType.Spot;
        torch.range = 30f;
        torch.spotAngle = 55f;
        torch.intensity = 3.4f;
        torch.color = new Color(0.94f, 0.96f, 1.0f);
        torch.enabled = false;

        // 5. Точка вылета пули
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(pistolObj.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.024f, 0.120f);

        GameObject flashLightObj = new GameObject("Muzzle_Light");
        flashLightObj.transform.SetParent(muzzle.transform, false);
        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 10f;
        flashLight.color = new Color(1f, 0.88f, 0.55f);
        flashLight.intensity = 4.2f;
        flashLight.enabled = false;

        MuzzleFlashController flashCtrl = muzzle.AddComponent<MuzzleFlashController>();
        flashCtrl.SetupReferences(flashLight, null);

        // 6. Окно экстракции гильз
        GameObject ejectionPort = new GameObject("EjectionPort");
        ejectionPort.transform.SetParent(pistolObj.transform, false);
        ejectionPort.transform.localPosition = new Vector3(0.024f, 0.030f, 0.025f);
        ejectionPort.transform.localRotation = Quaternion.Euler(-10f, 50f, 0f);

        // 7. Скрипт оружия
        RaycastWeapon weapon = pistolObj.AddComponent<RaycastWeapon>();
        weapon.ConfigureWeaponSpecs(
            RaycastWeapon.WeaponType.Pistol,
            "TACTICAL PISTOL",
            "9x19 MM",
            magCap: 17,
            current: 17,
            reserve: 68,
            rpm: 450f,
            dmg: 34f,
            mode: RaycastWeapon.FireMode.SemiAuto,
            canSwitch: false
        );
        weapon.ConfigureStanceOffsets(
            aimPos: new Vector3(-0.08f, 0.05f, 0.06f),
            aimRot: new Vector3(-0.5f, 1.2f, 1.5f),
            lowReadyPos: new Vector3(0.01f, -0.12f, -0.06f),
            lowReadyRot: new Vector3(28f, -10f, 6f),
            holsterPos: new Vector3(0.03f, -0.28f, -0.05f),
            holsterRot: new Vector3(20f, -10f, 6f)
        );
        weapon.SetupReferences(input, muzzle.transform, ejectionPort.transform, recoil, flashCtrl, torch, slideCtrl);

        return weapon;
    }

    private static GameObject CreatePart(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;

        Collider col = part.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        part.GetComponent<Renderer>().material = mat;
        return part;
    }

    // =========================================================================
    // HUD ОВЕРЛЕЙ БОДИКАМЕРЫ
    // =========================================================================
    private static void CreateBodycamHUD(WeaponInventoryController inventory, PlayerInputHandler input)
    {
        GameObject canvasObj = new GameObject("Bodycam_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        BodycamOverlayUI overlay = canvasObj.AddComponent<BodycamOverlayUI>();

        Font tacticalFont = Font.CreateDynamicFontFromOSFont("Consolas", 24);
        if (tacticalFont == null) tacticalFont = Font.CreateDynamicFontFromOSFont("Arial", 24);

        // 1. REC индикатор (Top-Left)
        GameObject recObj = new GameObject("Text_REC");
        recObj.transform.SetParent(canvasObj.transform, false);
        Text recText = recObj.AddComponent<Text>();
        recText.font = tacticalFont;
        recText.fontSize = 24;
        recText.fontStyle = FontStyle.Bold;
        recText.text = "● REC";
        recText.color = new Color(0.95f, 0.15f, 0.15f, 0.95f);
        RectTransform recRect = recObj.GetComponent<RectTransform>();
        recRect.anchorMin = new Vector2(0f, 1f);
        recRect.anchorMax = new Vector2(0f, 1f);
        recRect.pivot = new Vector2(0f, 1f);
        recRect.anchoredPosition = new Vector2(40f, -40f);
        recRect.sizeDelta = new Vector2(180f, 40f);

        // 2. Таймкод даты и времени (Top-Right)
        GameObject timeObj = new GameObject("Text_Timestamp");
        timeObj.transform.SetParent(canvasObj.transform, false);
        Text timeText = timeObj.AddComponent<Text>();
        timeText.font = tacticalFont;
        timeText.fontSize = 20;
        timeText.fontStyle = FontStyle.Bold;
        timeText.color = Color.white;
        RectTransform timeRect = timeObj.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(1f, 1f);
        timeRect.anchorMax = new Vector2(1f, 1f);
        timeRect.pivot = new Vector2(1f, 1f);
        timeRect.anchoredPosition = new Vector2(-40f, -40f);
        timeRect.sizeDelta = new Vector2(500f, 35f);
        timeText.alignment = TextAnchor.MiddleRight;

        // 3. Батарея (Top-Right под таймкодом)
        GameObject batObj = new GameObject("Text_Battery");
        batObj.transform.SetParent(canvasObj.transform, false);
        Text batText = batObj.AddComponent<Text>();
        batText.font = tacticalFont;
        batText.fontSize = 17;
        batText.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        RectTransform batRect = batObj.GetComponent<RectTransform>();
        batRect.anchorMin = new Vector2(1f, 1f);
        batRect.anchorMax = new Vector2(1f, 1f);
        batRect.pivot = new Vector2(1f, 1f);
        batRect.anchoredPosition = new Vector2(-40f, -75f);
        batRect.sizeDelta = new Vector2(380f, 30f);
        batText.alignment = TextAnchor.MiddleRight;

        // 4. Метаданные оперативника и GPS (Bottom-Left)
        GameObject metaObj = new GameObject("Text_Metadata");
        metaObj.transform.SetParent(canvasObj.transform, false);
        Text metaText = metaObj.AddComponent<Text>();
        metaText.font = tacticalFont;
        metaText.fontSize = 16;
        metaText.lineSpacing = 1.15f;
        metaText.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        RectTransform metaRect = metaObj.GetComponent<RectTransform>();
        metaRect.anchorMin = new Vector2(0f, 0f);
        metaRect.anchorMax = new Vector2(0f, 0f);
        metaRect.pivot = new Vector2(0f, 0f);
        metaRect.anchoredPosition = new Vector2(40f, 35f);
        metaRect.sizeDelta = new Vector2(520f, 95f);

        // 5. Боевой статус оружия и боезапас (Bottom-Right)
        GameObject wepObj = new GameObject("Text_WeaponStatus");
        wepObj.transform.SetParent(canvasObj.transform, false);
        Text wepText = wepObj.AddComponent<Text>();
        wepText.font = tacticalFont;
        wepText.fontSize = 17;
        wepText.lineSpacing = 1.15f;
        wepText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
        wepText.alignment = TextAnchor.LowerRight;
        RectTransform wepRect = wepObj.GetComponent<RectTransform>();
        wepRect.anchorMin = new Vector2(1f, 0f);
        wepRect.anchorMax = new Vector2(1f, 0f);
        wepRect.pivot = new Vector2(1f, 0f);
        wepRect.anchoredPosition = new Vector2(-40f, 35f);
        wepRect.sizeDelta = new Vector2(480f, 95f);

        overlay.SetupUI(timeText, recText, metaText, batText, wepText);
        overlay.SetupReferences(inventory, input);
    }

    private static Material CreateLitMaterial(Color col, float metallic = 0.2f, float smoothness = 0.4f)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        Material m = new Material(s);
        m.color = col;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private static Material CreateUnlitMaterial(Color col)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material m = new Material(s);
        m.color = col;
        return m;
    }
}
#endif
