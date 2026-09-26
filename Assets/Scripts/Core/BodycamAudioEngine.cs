using UnityEngine;

/// <summary>
/// Процедурный синтезатор и звуковой движок для тактического бодикама.
/// Генерирует в реальном времени процедурные аудиоклипы через AudioClip.Create,
/// обеспечивая реалистичное звучание выстрелов АК-74, пистолета, перезарядки,
/// отскока гильз, шагов и звона стальных мишеней без внешних wav/mp3 файлов.
/// </summary>
public class BodycamAudioEngine : MonoBehaviour
{
    public static BodycamAudioEngine Instance { get; private set; }

    private const int SampleRate = 44100;

    // Синтезированные клипы
    private AudioClip clipAkShot;
    private AudioClip clipPistolShot;
    private AudioClip clipDryFire;
    private AudioClip clipMagOut;
    private AudioClip clipMagIn;
    private AudioClip clipBoltRack;
    private AudioClip clipSlideRelease;
    private AudioClip clipCasingClink;
    private AudioClip clipFootstepWalk;
    private AudioClip clipFootstepRun;
    private AudioClip clipSteelTargetGong;
    private AudioClip clipFlashlightClick;
    private AudioClip clipSelectorClick;

    // Пул AudioSource для воспроизведения
    private AudioSource[] audioSourcePool;
    private int currentPoolIndex;
    private const int PoolSize = 16;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeAudioClips();
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        audioSourcePool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject srcObj = new GameObject($"AudioSource_Pool_{i}");
            srcObj.transform.SetParent(transform, false);
            AudioSource src = srcObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0.5f; // Полу-3D для эффекта нагрудного микрофона
            src.minDistance = 1f;
            src.maxDistance = 60f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSourcePool[i] = src;
        }
    }

    private void InitializeAudioClips()
    {
        clipAkShot = GenerateAkShotClip();
        clipPistolShot = GeneratePistolShotClip();
        clipDryFire = GenerateDryFireClip();
        clipMagOut = GenerateMagOutClip();
        clipMagIn = GenerateMagInClip();
        clipBoltRack = GenerateBoltRackClip();
        clipSlideRelease = GenerateSlideReleaseClip();
        clipCasingClink = GenerateCasingClinkClip();
        clipFootstepWalk = GenerateFootstepClip(false);
        clipFootstepRun = GenerateFootstepClip(true);
        clipSteelTargetGong = GenerateSteelTargetGongClip();
        clipFlashlightClick = GenerateFlashlightClickClip();
        clipSelectorClick = GenerateSelectorClickClip();
    }

    // -------------------------------------------------------------
    // Синтез Выстрела АК-74 (5.45x39: резкий хлопок, бас, перегруз микрофона)
    // -------------------------------------------------------------
    private AudioClip GenerateAkShotClip()
    {
        float duration = 0.55f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;

            // 1. Начальный гиперзвуковой щелчок ударной волны (первые 4 мс)
            float crack = 0f;
            if (t < 0.005f)
            {
                crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 900f);
            }

            // 2. Взрыв пороховых газов (широкополосный шум с затуханием)
            float blastNoise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 14f);

            // 3. Низкочастотный импульс давления (ударная волна 64 Гц с понижением тона)
            float lowFreq = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(68f, 38f, t) * t) * Mathf.Exp(-t * 16f) * 1.4f;

            // 4. Эхо дульного тормоза ДТК-74 (резонанс ~420 Гц)
            float dtkResonance = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 22f) * 0.35f;

            float rawSignal = (crack * 1.5f + blastNoise * 0.9f + lowFreq * 1.2f + dtkResonance) * 1.5f;

            // 5. Нелинейное ограничение / клиппинг сенсора бодикамеры (Soft Saturation)
            samples[i] = Mathf.Clamp(Mathf.Sin(rawSignal * 1.4f), -0.98f, 0.98f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_AK74_Shot", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Выстрела Пистолета (9х19: звонкий хлесткий хлопок)
    // -------------------------------------------------------------
    private AudioClip GeneratePistolShotClip()
    {
        float duration = 0.38f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;

            // 1. Хлесткий щелчок затвора и пули 9мм
            float whipCrack = 0f;
            if (t < 0.004f)
            {
                whipCrack = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 1200f);
            }

            // 2. Пороховой шум более короткий и высокий по тембру
            float blastNoise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 26f);

            // 3. Компактный низкочастотный толчок (110 Гц)
            float bassThud = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 28f) * 1.1f;

            float rawSignal = (whipCrack * 1.2f + blastNoise * 0.95f + bassThud * 1.0f) * 1.4f;

            // Сатурация
            samples[i] = Mathf.Clamp(Mathf.Sin(rawSignal * 1.3f), -0.96f, 0.96f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_Pistol_Shot", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Холостого Спуска (Dry Fire Click)
    // -------------------------------------------------------------
    private AudioClip GenerateDryFireClip()
    {
        float duration = 0.06f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float metallicPing = Mathf.Sin(2f * Mathf.PI * 2800f * t) * Mathf.Exp(-t * 160f);
            float clickImpulse = (Random.value * 2f - 1f) * Mathf.Exp(-t * 350f) * 0.7f;
            samples[i] = (metallicPing * 0.6f + clickImpulse * 0.8f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_DryFire", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Извлечения Магазина (Mag Out)
    // -------------------------------------------------------------
    private AudioClip GenerateMagOutClip()
    {
        float duration = 0.22f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            // Щелчок защелки магазина (0.01s)
            float latchClick = 0f;
            if (t < 0.03f)
            {
                latchClick = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 220f);
            }
            // Трение направляющих шахты магазина
            float friction = (Random.value * 2f - 1f) * Mathf.Exp(-(t - 0.05f) * 18f) * 0.25f;
            if (t < 0.05f) friction = 0f;

            samples[i] = (latchClick * 0.8f + friction * 0.5f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_MagOut", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Вставки Магазина (Mag In)
    // -------------------------------------------------------------
    private AudioClip GenerateMagInClip()
    {
        float duration = 0.25f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            // Удар посадки магазина в защелку
            float impact = Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-t * 45f) * 0.9f;
            float steelSnap = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t * 120f) * 0.7f;
            float bodyThud = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 30f) * 0.6f;

            samples[i] = Mathf.Clamp((impact + steelSnap + bodyThud) * 0.7f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_MagIn", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Передергивания Затвора (Bolt Rack / Charging Handle)
    // -------------------------------------------------------------
    private AudioClip GenerateBoltRackClip()
    {
        float duration = 0.45f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;

            // 1. Отведение назад: металлический скрежет возвратной пружины
            float pull = 0f;
            if (t >= 0.02f && t < 0.18f)
            {
                float pt = t - 0.02f;
                pull = Mathf.Sin(2f * Mathf.PI * 850f * pt) * Mathf.Exp(-pt * 30f) * 0.6f;
                pull += (Random.value * 2f - 1f) * Mathf.Exp(-pt * 35f) * 0.3f;
            }

            // 2. Спуск и удар затвора о казенник (Slam into battery) в районе 0.24s
            float slam = 0f;
            if (t >= 0.24f)
            {
                float st = t - 0.24f;
                float clank1 = Mathf.Sin(2f * Mathf.PI * 1650f * st) * Mathf.Exp(-st * 60f) * 0.9f;
                float clank2 = Mathf.Sin(2f * Mathf.PI * 520f * st) * Mathf.Exp(-st * 40f) * 0.8f;
                float noiseSnap = (Random.value * 2f - 1f) * Mathf.Exp(-st * 180f) * 0.5f;
                slam = (clank1 + clank2 + noiseSnap);
            }

            samples[i] = Mathf.Clamp(pull + slam, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_BoltRack", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Сброса Затворной Задержки Пистолета (Slide Release)
    // -------------------------------------------------------------
    private AudioClip GenerateSlideReleaseClip()
    {
        float duration = 0.22f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float releaseClick = Mathf.Sin(2f * Mathf.PI * 2600f * t) * Mathf.Exp(-t * 180f) * 0.7f;
            float slamThud = Mathf.Sin(2f * Mathf.PI * 650f * t) * Mathf.Exp(-t * 60f) * 0.8f;
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 100f) * 0.4f;

            samples[i] = Mathf.Clamp((releaseClick + slamThud + noise) * 0.8f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_SlideRelease", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Звона Отскока Латунной Гильзы (Brass Casing Clink)
    // -------------------------------------------------------------
    private AudioClip GenerateCasingClinkClip()
    {
        float duration = 0.16f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            // Звонкий резонанс латуни (~3750 Гц и 4480 Гц)
            float tone1 = Mathf.Sin(2f * Mathf.PI * 3750f * t) * Mathf.Exp(-t * 42f);
            float tone2 = Mathf.Sin(2f * Mathf.PI * 4480f * t) * Mathf.Exp(-t * 55f) * 0.5f;
            float transient = (Random.value * 2f - 1f) * Mathf.Exp(-t * 300f) * 0.3f;

            samples[i] = (tone1 + tone2 + transient) * 0.75f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_CasingClink", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Шага (Footstep Thud)
    // -------------------------------------------------------------
    private AudioClip GenerateFootstepClip(bool isRunning)
    {
        float duration = isRunning ? 0.16f : 0.14f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        float baseFreq = isRunning ? 75f : 90f;
        float decay = isRunning ? 22f : 28f;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(baseFreq, 45f, t) * t) * Mathf.Exp(-t * decay);
            float crunch = (Random.value * 2f - 1f) * Mathf.Exp(-t * (decay * 1.5f)) * 0.35f;

            samples[i] = (thud * 0.85f + crunch * 0.35f);
        }

        AudioClip clip = AudioClip.Create(isRunning ? "Synthesized_Footstep_Run" : "Synthesized_Footstep_Walk", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Удара и Звона Стального Поппера (Steel Target Gong)
    // Моделирование 4 гармоник колебания стальной пластины толщиной 12мм
    // -------------------------------------------------------------
    private AudioClip GenerateSteelTargetGongClip()
    {
        float duration = 1.35f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;

            // Начальный металлический щелчок пробития пулей (первые 3 мс)
            float hitSnap = 0f;
            if (t < 0.005f)
            {
                hitSnap = (Random.value * 2f - 1f) * Mathf.Exp(-t * 700f);
            }

            // Модальный синтез стального колокола:
            // f1 = 388 Hz (основной тон)
            float m1 = Mathf.Sin(2f * Mathf.PI * 388f * t) * Mathf.Exp(-t * 3.8f);
            // f2 = 824 Hz (первый обертон)
            float m2 = Mathf.Sin(2f * Mathf.PI * 824f * t) * Mathf.Exp(-t * 5.2f) * 0.65f;
            // f3 = 1630 Hz (высокий звон)
            float m3 = Mathf.Sin(2f * Mathf.PI * 1630f * t) * Mathf.Exp(-t * 8.5f) * 0.45f;
            // f4 = 3080 Hz (вибрация края мишени)
            float m4 = Mathf.Sin(2f * Mathf.PI * 3080f * t) * Mathf.Exp(-t * 14f) * 0.25f;

            float combined = hitSnap * 0.8f + (m1 + m2 + m3 + m4) * 0.65f;
            samples[i] = Mathf.Clamp(combined, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_SteelTarget_Gong", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Клика Фонарика (Tactical Flashlight Click)
    // -------------------------------------------------------------
    private AudioClip GenerateFlashlightClickClip()
    {
        float duration = 0.04f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float click = Mathf.Sin(2f * Mathf.PI * 3200f * t) * Mathf.Exp(-t * 300f);
            samples[i] = click * 0.6f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_FlashlightClick", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Синтез Переключателя Огня (Selector Switch Click)
    // -------------------------------------------------------------
    private AudioClip GenerateSelectorClickClip()
    {
        float duration = 0.08f;
        int totalSamples = Mathf.FloorToInt(SampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float metalClick = Mathf.Sin(2f * Mathf.PI * 1450f * t) * Mathf.Exp(-t * 110f);
            float latch = (Random.value * 2f - 1f) * Mathf.Exp(-t * 160f) * 0.4f;
            samples[i] = (metalClick * 0.7f + latch * 0.4f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_SelectorClick", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // -------------------------------------------------------------
    // Публичное API воспроизведения звуков
    // -------------------------------------------------------------
    private AudioSource GetAvailableSource()
    {
        if (audioSourcePool == null || audioSourcePool.Length == 0) return null;
        AudioSource src = audioSourcePool[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Length;
        return src;
    }

    public static void PlayAkShot(Vector3 pos)
    {
        if (Instance == null || Instance.clipAkShot == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.97f, 1.03f);
        src.PlayOneShot(Instance.clipAkShot, 1.0f);
    }

    public static void PlayPistolShot(Vector3 pos)
    {
        if (Instance == null || Instance.clipPistolShot == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.98f, 1.04f);
        src.PlayOneShot(Instance.clipPistolShot, 0.95f);
    }

    public static void PlayDryFire(Vector3 pos)
    {
        if (Instance == null || Instance.clipDryFire == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.95f, 1.05f);
        src.PlayOneShot(Instance.clipDryFire, 0.7f);
    }

    public static void PlayMagOut(Vector3 pos)
    {
        if (Instance == null || Instance.clipMagOut == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.96f, 1.04f);
        src.PlayOneShot(Instance.clipMagOut, 0.8f);
    }

    public static void PlayMagIn(Vector3 pos)
    {
        if (Instance == null || Instance.clipMagIn == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.96f, 1.04f);
        src.PlayOneShot(Instance.clipMagIn, 0.85f);
    }

    public static void PlayBoltRack(Vector3 pos)
    {
        if (Instance == null || Instance.clipBoltRack == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.97f, 1.03f);
        src.PlayOneShot(Instance.clipBoltRack, 0.9f);
    }

    public static void PlaySlideRelease(Vector3 pos)
    {
        if (Instance == null || Instance.clipSlideRelease == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.97f, 1.03f);
        src.PlayOneShot(Instance.clipSlideRelease, 0.85f);
    }

    public static void PlayCasingClink(Vector3 pos, float impactSpeed = 1f)
    {
        if (Instance == null || Instance.clipCasingClink == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.92f, 1.15f); // Натуральная вариация тона латуни
        float volume = Mathf.Clamp01(impactSpeed * 0.25f + 0.2f);
        src.PlayOneShot(Instance.clipCasingClink, volume);
    }

    public static void PlayFootstep(Vector3 pos, bool isRunning)
    {
        if (Instance == null) return;
        AudioClip clip = isRunning ? Instance.clipFootstepRun : Instance.clipFootstepWalk;
        if (clip == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.94f, 1.06f);
        src.PlayOneShot(clip, isRunning ? 0.65f : 0.45f);
    }

    public static void PlaySteelTargetGong(Vector3 pos)
    {
        if (Instance == null || Instance.clipSteelTargetGong == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.97f, 1.03f);
        src.PlayOneShot(Instance.clipSteelTargetGong, 1.0f);
    }

    public static void PlayFlashlightClick(Vector3 pos)
    {
        if (Instance == null || Instance.clipFlashlightClick == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.95f, 1.05f);
        src.PlayOneShot(Instance.clipFlashlightClick, 0.6f);
    }

    public static void PlaySelectorClick(Vector3 pos)
    {
        if (Instance == null || Instance.clipSelectorClick == null) return;
        AudioSource src = Instance.GetAvailableSource();
        if (src == null) return;
        src.transform.position = pos;
        src.pitch = Random.Range(0.95f, 1.05f);
        src.PlayOneShot(Instance.clipSelectorClick, 0.7f);
    }
}
