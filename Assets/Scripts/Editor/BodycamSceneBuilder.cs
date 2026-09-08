#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Автоматический генератор полноценной сцены тактического шутера Bodycam в Unity Editor.
/// Создает:
/// - Тактический полигон с укрытиями, стенами и интерактивными мишенями
/// - Игрока с нагрудной камерой (Bodycam Rig)
/// - Детализированную тактическую винтовку с лазером, вспышкой и прицелом
/// - Разделенную 3-уровневую систему физики (Deadzone Aiming -> Sway -> Recoil Springs)
/// - HUD оверлей бодикамеры с таймкодом и REC
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

        // 1. Создаем новую чистую сцену
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Создаем окружение (Свет, Стены, Укрытия, Мишени)
        CreateEnvironment();

        // 3. Создаем Игрока, кинематическую цепь камеры и оружие
        GameObject playerRoot = CreatePlayerWithWeapon();

        // 4. Создаем HUD оверлей бодикамеры
        CreateBodycamHUD();

        // 5. Сохраняем сцену и открываем ее
        string scenePath = "Assets/Scenes/BodycamPrototype.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(scenePath);

        // Регистрируем в Build Settings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        Debug.Log("<color=green><b>[Bodycam] Сцена успешно собрана и открыта: Assets/Scenes/BodycamPrototype.unity! Нажмите Play (Ctrl+P)!</b></color>");
    }

    private static void CreateEnvironment()
    {
        // 1. Солнце / Направленный свет
        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        light.color = new Color(0.96f, 0.94f, 0.90f);
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        // 2. Пол полигона
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Arena";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(8f, 1f, 8f); // 80x80 м

        Material floorMat = CreateLitMaterial(new Color(0.32f, 0.34f, 0.38f));
        floor.GetComponent<Renderer>().material = floorMat;

        // 3. Периметр полигона (Стены)
        CreateWall(new Vector3(0f, 2.5f, 40f), new Vector3(80f, 5f, 1.5f), new Color(0.25f, 0.27f, 0.3f));
        CreateWall(new Vector3(0f, 2.5f, -40f), new Vector3(80f, 5f, 1.5f), new Color(0.25f, 0.27f, 0.3f));
        CreateWall(new Vector3(40f, 2.5f, 0f), new Vector3(1.5f, 5f, 80f), new Color(0.25f, 0.27f, 0.3f));
        CreateWall(new Vector3(-40f, 2.5f, 0f), new Vector3(1.5f, 5f, 80f), new Color(0.25f, 0.27f, 0.3f));

        // 4. Тактические укрытия перед игроком
        CreateWall(new Vector3(0f, 1.2f, 7f), new Vector3(3.5f, 2.4f, 0.8f), new Color(0.42f, 0.44f, 0.48f));
        CreateWall(new Vector3(-5.5f, 1.5f, 12f), new Vector3(1.2f, 3.0f, 4.5f), new Color(0.38f, 0.40f, 0.44f));
        CreateWall(new Vector3(5.5f, 1.3f, 13f), new Vector3(4.0f, 2.6f, 1.2f), new Color(0.38f, 0.40f, 0.44f));
        CreateWall(new Vector3(-2f, 1.0f, 18f), new Vector3(5.0f, 2.0f, 0.8f), new Color(0.45f, 0.47f, 0.50f));

        // 5. Интерактивные физические мишени (реагируют на попадание пуль)
        for (int i = -3; i <= 3; i++)
        {
            CreatePhysicalTarget(new Vector3(i * 3.2f, 1.3f, 24f));
        }
    }

    private static void CreateWall(Vector3 pos, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall_Cover";
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = CreateLitMaterial(color);
    }

    private static void CreatePhysicalTarget(Vector3 pos)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        target.name = "Shooting_Target";
        target.transform.position = pos;
        target.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);

        // Яркий красный тактический цвет мишени
        target.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.85f, 0.22f, 0.18f));

        Rigidbody rb = target.AddComponent<Rigidbody>();
        rb.mass = 45f;
    }

    private static GameObject CreatePlayerWithWeapon()
    {
        // 1. Корневой объект игрока
        GameObject player = new GameObject("Player_Root");
        player.transform.position = new Vector3(0f, 0f, 0f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        BodycamPlayerController movement = player.AddComponent<BodycamPlayerController>();

        // 2. Chest Mount Pivot (виртуальное крепление камеры на бронежилете ~1.42м)
        GameObject chestMount = new GameObject("Chest_Mount_Pivot");
        chestMount.transform.SetParent(player.transform, false);
        chestMount.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        movement.SetChestMountPivot(chestMount.transform);

        // 3. Camera Rig (подвес камеры, инерция, Pitch и боббинг Лиссажу)
        GameObject cameraRig = new GameObject("Camera_Rig");
        cameraRig.transform.SetParent(player.transform, false);
        cameraRig.transform.position = chestMount.transform.position;

        BodycamCameraRig camRigComp = cameraRig.AddComponent<BodycamCameraRig>();
        camRigComp.SetupReferences(chestMount.transform, movement, input);

        // 4. Main Camera
        GameObject mainCam = new GameObject("Main Camera");
        mainCam.transform.SetParent(cameraRig.transform, false);
        Camera cam = mainCam.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 92f;
        cam.nearClipPlane = 0.03f;
        mainCam.AddComponent<AudioListener>();

        // 5. Weapon Rig Pivot (свободный прицел Deadzone Aiming)
        GameObject weaponPivot = new GameObject("Weapon_Rig_Pivot");
        weaponPivot.transform.SetParent(cameraRig.transform, false);
        // Позиционируем оружие в поле зрения нагрудной камеры
        weaponPivot.transform.localPosition = new Vector3(0.16f, -0.16f, 0.40f);
        weaponPivot.transform.localRotation = Quaternion.Euler(1.5f, -2.5f, 0f);

        DeadzoneAimController aimController = player.AddComponent<DeadzoneAimController>();
        aimController.SetupReferences(input, movement, weaponPivot.transform);

        // 6. Уровень Sway: инерция веса оружия за мышью и шаги
        GameObject weaponSwayObj = new GameObject("Weapon_Sway");
        weaponSwayObj.transform.SetParent(weaponPivot.transform, false);
        ProceduralWeaponSway sway = weaponSwayObj.AddComponent<ProceduralWeaponSway>();
        sway.SetupReferences(input, movement);

        // 7. Уровень Recoil Springs: гармонические пружины отдачи
        GameObject weaponRecoilObj = new GameObject("Weapon_Recoil");
        weaponRecoilObj.transform.SetParent(weaponSwayObj.transform, false);
        WeaponRecoilSpring recoil = weaponRecoilObj.AddComponent<WeaponRecoilSpring>();

        // 8. Сборка детальной тактической винтовки
        GameObject weaponModel = BuildTacticalRifle(weaponRecoilObj.transform);

        // 9. Точка вылета пули (Muzzle) и контроллер вспышки
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(weaponModel.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.58f);

        // Точечный свет выстрела
        GameObject flashLightObj = new GameObject("Muzzle_Light");
        flashLightObj.transform.SetParent(muzzle.transform, false);
        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 14f;
        flashLight.color = new Color(1f, 0.88f, 0.55f);
        flashLight.intensity = 5.0f;
        flashLight.enabled = false;

        MuzzleFlashController flashCtrl = muzzle.AddComponent<MuzzleFlashController>();
        flashCtrl.SetupReferences(flashLight, null);

        // Скрипт оружия (баллистика Raycast)
        RaycastWeapon weapon = weaponModel.AddComponent<RaycastWeapon>();
        weapon.SetupReferences(input, muzzle.transform, recoil, flashCtrl);

        // 10. Тактический зеленый лазерный целеуказатель
        GameObject laserObj = new GameObject("Laser_Sight");
        laserObj.transform.SetParent(muzzle.transform, false);
        laserObj.transform.localPosition = new Vector3(0.035f, -0.01f, 0f);

        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        LaserSight laserSight = laserObj.AddComponent<LaserSight>();

        GameObject laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        laserDot.name = "Laser_Dot";
        Collider dotCol = laserDot.GetComponent<Collider>();
        if (dotCol != null) Object.DestroyImmediate(dotCol);
        laserDot.transform.localScale = Vector3.one * 0.035f;

        Material dotMat = CreateUnlitMaterial(new Color(0.1f, 1f, 0.25f, 1f));
        laserDot.GetComponent<Renderer>().material = dotMat;

        laserSight.SetupDot(laserDot.transform);

        return player;
    }

    /// <summary>
    /// Собирает визуально различимую, контрастную тактическую винтовку из примитивов.
    /// </summary>
    private static GameObject BuildTacticalRifle(Transform parent)
    {
        GameObject rifle = new GameObject("Weapon_Model");
        rifle.transform.SetParent(parent, false);

        Material gunBodyMat = CreateLitMaterial(new Color(0.14f, 0.15f, 0.17f));
        Material gripMat = CreateLitMaterial(new Color(0.08f, 0.08f, 0.09f));
        Material magMat = CreateLitMaterial(new Color(0.22f, 0.20f, 0.18f));
        Material barrelMat = CreateLitMaterial(new Color(0.10f, 0.11f, 0.12f));
        Material sightMat = CreateLitMaterial(new Color(0.18f, 0.19f, 0.22f));

        // 1. Ствольная коробка (Receiver)
        CreateGunPart(rifle.transform, "Receiver", new Vector3(0f, 0f, 0f), new Vector3(0.065f, 0.11f, 0.38f), gunBodyMat);

        // 2. Тактическая пистолетная рукоять (Pistol Grip)
        GameObject grip = CreateGunPart(rifle.transform, "Grip", new Vector3(0f, -0.10f, -0.08f), new Vector3(0.045f, 0.13f, 0.065f), gripMat);
        grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

        // 3. Тактический магазин (Magazine)
        GameObject mag = CreateGunPart(rifle.transform, "Magazine", new Vector3(0f, -0.13f, 0.07f), new Vector3(0.042f, 0.22f, 0.085f), magMat);
        mag.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

        // 4. Цевье (Handguard)
        CreateGunPart(rifle.transform, "Handguard", new Vector3(0f, 0.005f, 0.32f), new Vector3(0.055f, 0.075f, 0.30f), gunBodyMat);

        // 5. Ствол и дульный тормоз (Barrel)
        CreateGunPart(rifle.transform, "Barrel", new Vector3(0f, 0.015f, 0.50f), new Vector3(0.024f, 0.024f, 0.16f), barrelMat);

        // 6. Коллиматорный прицел (Reflex Sight)
        CreateGunPart(rifle.transform, "Sight_Base", new Vector3(0f, 0.075f, 0.04f), new Vector3(0.05f, 0.06f, 0.11f), sightMat);
        
        // Светящаяся прицельная марка коллиматора
        GameObject reticle = CreateGunPart(rifle.transform, "Reticle_Dot", new Vector3(0f, 0.085f, 0.04f), new Vector3(0.015f, 0.015f, 0.005f), CreateUnlitMaterial(Color.red));

        return rifle;
    }

    private static GameObject CreateGunPart(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
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

    private static void CreateBodycamHUD()
    {
        GameObject canvasObj = new GameObject("Bodycam_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        BodycamOverlayUI overlay = canvasObj.AddComponent<BodycamOverlayUI>();

        // Шрифт Consolas / Arial (гарантированно есть на Windows, выглядит как настоящий военный оверлей)
        Font tacticalFont = Font.CreateDynamicFontFromOSFont("Consolas", 24);
        if (tacticalFont == null) tacticalFont = Font.CreateDynamicFontFromOSFont("Arial", 24);

        // 1. REC индикатор
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

        // 2. Таймкод даты и времени
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
        timeRect.sizeDelta = new Vector2(500f, 40f);
        timeText.alignment = TextAnchor.MiddleRight;

        // 3. Метаданные оперативника
        GameObject metaObj = new GameObject("Text_Metadata");
        metaObj.transform.SetParent(canvasObj.transform, false);
        Text metaText = metaObj.AddComponent<Text>();
        metaText.font = tacticalFont;
        metaText.fontSize = 17;
        metaText.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        RectTransform metaRect = metaObj.GetComponent<RectTransform>();
        metaRect.anchorMin = new Vector2(0f, 0f);
        metaRect.anchorMax = new Vector2(0f, 0f);
        metaRect.pivot = new Vector2(0f, 0f);
        metaRect.anchoredPosition = new Vector2(40f, 40f);
        metaRect.sizeDelta = new Vector2(450f, 80f);

        // 4. Статус батареи
        GameObject batObj = new GameObject("Text_Battery");
        batObj.transform.SetParent(canvasObj.transform, false);
        Text batText = batObj.AddComponent<Text>();
        batText.font = tacticalFont;
        batText.fontSize = 18;
        batText.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        RectTransform batRect = batObj.GetComponent<RectTransform>();
        batRect.anchorMin = new Vector2(1f, 0f);
        batRect.anchorMax = new Vector2(1f, 0f);
        batRect.pivot = new Vector2(1f, 0f);
        batRect.anchoredPosition = new Vector2(-40f, 40f);
        batRect.sizeDelta = new Vector2(280f, 40f);
        batText.alignment = TextAnchor.MiddleRight;

        overlay.SetupUI(timeText, recText, metaText, batText);
    }

    private static Material CreateLitMaterial(Color col)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        Material m = new Material(s);
        m.color = col;
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
