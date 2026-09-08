#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Автоматический генератор полноценной сцены шутера Bodycam в Unity Editor.
/// Позволяет в один клик через меню собрать готовую игровую сцену с ареной,
/// настроенным игроком, оружием, лазером, физикой и оверлеем бодикамеры.
/// </summary>
public static class BodycamSceneBuilder
{
    [MenuItem("Tools/Bodycam/Setup Complete Prototype Scene", priority = 10)]
    public static void BuildCompleteScene()
    {
        // 1. Создаем новую чистую сцену
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Создаем окружение (Свет, Пол, Препятствия, Мишени)
        CreateEnvironment();

        // 3. Создаем Игрока и всю кинематическую цепь
        GameObject playerRoot = CreatePlayer();

        // 4. Создаем HUD оверлей бодикамеры
        CreateBodycamHUD();

        // 5. Сохраняем сцену
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BodycamPrototype.unity");
        Debug.Log("<color=green><b>[Bodycam] Сцена успешно собрана и сохранена: Assets/Scenes/BodycamPrototype.unity!</b></color>");
    }

    private static void CreateEnvironment()
    {
        // Солнце / Свет
        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(0.95f, 0.95f, 1.0f);
        sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        // Пол
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(8f, 1f, 8f);

        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = new Color(0.2f, 0.22f, 0.25f);
        floor.GetComponent<Renderer>().material = floorMat;

        // Препятствия и укрытия (Тактический полигон)
        CreateCoverBox(new Vector3(0f, 1f, 8f), new Vector3(3f, 2f, 1f), new Color(0.35f, 0.38f, 0.4f));
        CreateCoverBox(new Vector3(-6f, 1.5f, 12f), new Vector3(1.5f, 3f, 4f), new Color(0.3f, 0.32f, 0.35f));
        CreateCoverBox(new Vector3(6f, 1.2f, 14f), new Vector3(4f, 2.4f, 1.5f), new Color(0.3f, 0.32f, 0.35f));

        // Физические мишени для проверки выстрелов
        for (int i = -2; i <= 2; i++)
        {
            CreatePhysicalTarget(new Vector3(i * 3.5f, 1.2f, 20f));
        }
    }

    private static void CreateCoverBox(Vector3 pos, Vector3 scale, Color col)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Cover_Wall";
        box.transform.position = pos;
        box.transform.localScale = scale;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        box.GetComponent<Renderer>().material = mat;
    }

    private static void CreatePhysicalTarget(Vector3 pos)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        target.name = "Shooting_Target";
        target.transform.position = pos;
        target.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.85f, 0.25f, 0.2f);
        target.GetComponent<Renderer>().material = mat;

        Rigidbody rb = target.AddComponent<Rigidbody>();
        rb.mass = 45f;
    }

    private static GameObject CreatePlayer()
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

        // 2. Chest Mount Pivot (крепление на груди)
        GameObject chestMount = new GameObject("Chest_Mount_Pivot");
        chestMount.transform.SetParent(player.transform, false);
        chestMount.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        movement.SetChestMountPivot(chestMount.transform);

        // 3. Camera Rig
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
        cam.fieldOfView = 95f;
        cam.nearClipPlane = 0.05f;
        mainCam.AddComponent<AudioListener>();

        // 5. Weapon Rig Pivot
        GameObject weaponPivot = new GameObject("Weapon_Rig_Pivot");
        weaponPivot.transform.SetParent(cameraRig.transform, false);
        weaponPivot.transform.localPosition = new Vector3(0.18f, -0.22f, 0.35f);

        DeadzoneAimController aimController = player.AddComponent<DeadzoneAimController>();
        aimController.SetupReferences(input, movement, weaponPivot.transform, cameraRig.transform);

        // 6. Weapon Spring Lag & Sway
        GameObject weaponSpring = new GameObject("Weapon_Spring_Lag");
        weaponSpring.transform.SetParent(weaponPivot.transform, false);

        ProceduralWeaponSway sway = weaponSpring.AddComponent<ProceduralWeaponSway>();
        sway.SetupReferences(input, movement);

        WeaponRecoilSpring recoil = weaponSpring.AddComponent<WeaponRecoilSpring>();

        // 7. Процедурный визуал тактической винтовки
        GameObject gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gunBody.name = "Weapon_Model";
        gunBody.transform.SetParent(weaponSpring.transform, false);
        gunBody.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        gunBody.transform.localScale = new Vector3(0.06f, 0.09f, 0.55f);
        
        Collider gunCol = gunBody.GetComponent<Collider>();
        if (gunCol != null) Object.DestroyImmediate(gunCol);

        Material gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gunMat.color = new Color(0.12f, 0.12f, 0.14f);
        gunBody.GetComponent<Renderer>().material = gunMat;

        // 8. Срез ствола (Muzzle)
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(gunBody.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.55f);

        // Свет вспышки
        GameObject flashLightObj = new GameObject("Muzzle_Light");
        flashLightObj.transform.SetParent(muzzle.transform, false);
        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 10f;
        flashLight.color = new Color(1f, 0.85f, 0.5f);
        flashLight.intensity = 4.5f;
        flashLight.enabled = false;

        MuzzleFlashController flashCtrl = muzzle.AddComponent<MuzzleFlashController>();
        flashCtrl.SetupReferences(flashLight, null);

        // Скрипт оружия
        RaycastWeapon weapon = gunBody.AddComponent<RaycastWeapon>();
        weapon.SetupReferences(input, muzzle.transform, recoil, flashCtrl);

        // 9. Лазерный целеуказатель
        GameObject laserObj = new GameObject("Laser_Sight");
        laserObj.transform.SetParent(muzzle.transform, false);
        laserObj.transform.localPosition = new Vector3(0.04f, -0.02f, 0f);

        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        LaserSight laserSight = laserObj.AddComponent<LaserSight>();

        GameObject laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        laserDot.name = "Laser_Dot";
        Collider dotCol = laserDot.GetComponent<Collider>();
        if (dotCol != null) Object.DestroyImmediate(dotCol);
        laserDot.transform.localScale = Vector3.one * 0.035f;

        Material dotMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        dotMat.color = new Color(0.1f, 1f, 0.2f, 1f);
        laserDot.GetComponent<Renderer>().material = dotMat;

        laserSight.SetupDot(laserDot.transform);

        return player;
    }

    private static void CreateBodycamHUD()
    {
        GameObject canvasObj = new GameObject("Bodycam_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        BodycamOverlayUI overlay = canvasObj.AddComponent<BodycamOverlayUI>();

        // Шрифты и цвета
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. REC индикатор
        GameObject recObj = new GameObject("Text_REC");
        recObj.transform.SetParent(canvasObj.transform, false);
        Text recText = recObj.AddComponent<Text>();
        recText.font = defaultFont;
        recText.fontSize = 26;
        recText.fontStyle = FontStyle.Bold;
        recText.text = "● REC";
        recText.color = new Color(0.9f, 0.15f, 0.15f, 0.95f);
        RectTransform recRect = recObj.GetComponent<RectTransform>();
        recRect.anchorMin = new Vector2(0f, 1f);
        recRect.anchorMax = new Vector2(0f, 1f);
        recRect.pivot = new Vector2(0f, 1f);
        recRect.anchoredPosition = new Vector2(40f, -40f);
        recRect.sizeDelta = new Vector2(160f, 40f);

        // 2. Таймкод даты и времени
        GameObject timeObj = new GameObject("Text_Timestamp");
        timeObj.transform.SetParent(canvasObj.transform, false);
        Text timeText = timeObj.AddComponent<Text>();
        timeText.font = defaultFont;
        timeText.fontSize = 22;
        timeText.color = Color.white;
        RectTransform timeRect = timeObj.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(1f, 1f);
        timeRect.anchorMax = new Vector2(1f, 1f);
        timeRect.pivot = new Vector2(1f, 1f);
        timeRect.anchoredPosition = new Vector2(-40f, -40f);
        timeRect.sizeDelta = new Vector2(450f, 40f);
        timeText.alignment = TextAnchor.MiddleRight;

        // 3. Метаданные оперативника
        GameObject metaObj = new GameObject("Text_Metadata");
        metaObj.transform.SetParent(canvasObj.transform, false);
        Text metaText = metaObj.AddComponent<Text>();
        metaText.font = defaultFont;
        metaText.fontSize = 18;
        metaText.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        RectTransform metaRect = metaObj.GetComponent<RectTransform>();
        metaRect.anchorMin = new Vector2(0f, 0f);
        metaRect.anchorMax = new Vector2(0f, 0f);
        metaRect.pivot = new Vector2(0f, 0f);
        metaRect.anchoredPosition = new Vector2(40f, 40f);
        metaRect.sizeDelta = new Vector2(400f, 80f);

        // 4. Статус батареи
        GameObject batObj = new GameObject("Text_Battery");
        batObj.transform.SetParent(canvasObj.transform, false);
        Text batText = batObj.AddComponent<Text>();
        batText.font = defaultFont;
        batText.fontSize = 18;
        batText.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        RectTransform batRect = batObj.GetComponent<RectTransform>();
        batRect.anchorMin = new Vector2(1f, 0f);
        batRect.anchorMax = new Vector2(1f, 0f);
        batRect.pivot = new Vector2(1f, 0f);
        batRect.anchoredPosition = new Vector2(-40f, 40f);
        batRect.sizeDelta = new Vector2(250f, 40f);
        batText.alignment = TextAnchor.MiddleRight;

        overlay.SetupUI(timeText, recText, metaText, batText);
    }
}
#endif
