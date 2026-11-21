// ProceduralAudio.cs — весь звук игры синтезируется кодом (AudioClip.Create + SetData):
// гул двигателей, ветер, тревога, удары, крики, радио-писки. Никаких .wav файлов в проекте.

using UnityEngine;

/// <summary>Синтез звуков + управление зацикленным эмбиентом сцены.</summary>
public class ProceduralAudio : MonoBehaviour
{
    // ---------- Клипы (ленивые синглтоны) ----------

    private static AudioClip _engine;
    /// <summary>Гул винтового двигателя (2 сек, зацикленный).</summary>
    public static AudioClip Engine
    {
        get
        {
            if (_engine != null) return _engine;
            const int sampleRate = 22050;
            const int length = sampleRate * 2;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                // Гармоники вала винта + лёгкая рыскань поршней + шум.
                float v = 0.42f * Mathf.Sin(2f * Mathf.PI * 26f * t)
                        + 0.25f * Mathf.Sin(2f * Mathf.PI * 52f * t)
                        + 0.14f * Mathf.Sin(2f * Mathf.PI * 78f * t)
                        + 0.10f * Mathf.Sin(2f * Mathf.PI * 104f * t + Mathf.Sin(t * 3f));
                v += (Mathf.PerlinNoise(t * 40f, 0.5f) - 0.5f) * 0.12f;
                data[i] = v * 0.8f;
            }
            _engine = AudioClip.Create("EngineLoop", length, 1, sampleRate, false);
            _engine.SetData(data, 0);
            return _engine;
        }
    }

    private static AudioClip _wind;
    /// <summary>Ветер (розовый шум, 3 сек).</summary>
    public static AudioClip Wind
    {
        get
        {
            if (_wind != null) return _wind;
            const int sampleRate = 22050;
            const int length = sampleRate * 3;
            var data = new float[length];
            float last = 0f;
            for (int i = 0; i < length; i++)
            {
                // Низкочастотный фильтрованный шум + медленная модуляция «порывов».
                float t = i / (float)sampleRate;
                float noise = Mathf.PerlinNoise(t * 23f, 12.3f) - 0.5f;
                last = Mathf.Lerp(last, noise, 0.08f);
                float gust = 0.5f + 0.5f * Mathf.Sin(t * 0.9f + Mathf.Sin(t * 0.37f) * 2f);
                data[i] = Mathf.Clamp(last * (1.6f + gust), -1f, 1f) * 0.7f;
            }
            _wind = AudioClip.Create("WindLoop", length, 1, sampleRate, false);
            _wind.SetData(data, 0);
            return _wind;
        }
    }

    private static AudioClip _alarm;
    /// <summary>Тревожный сигнал (квадрат, три гудка).</summary>
    public static AudioClip Alarm
    {
        get
        {
            if (_alarm != null) return _alarm;
            const int sampleRate = 22050;
            const int length = sampleRate;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                int seg = (int)(t * 6f) % 2; // чередование 0/1
                float gate = seg == 0 ? 1f : 0f;
                float v = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 880f * t)) * 0.35f * gate;
                data[i] = v;
            }
            _alarm = AudioClip.Create("Alarm", length, 1, sampleRate, false);
            _alarm.SetData(data, 0);
            return _alarm;
        }
    }

    private static AudioClip _thunk;
    /// <summary>Глухой удар (птица в двигатель).</summary>
    public static AudioClip Thunk
    {
        get
        {
            if (_thunk != null) return _thunk;
            const int sampleRate = 22050;
            const int length = sampleRate / 3;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 22f);
                float v = (Mathf.PerlinNoise(t * 300f, 3f) - 0.5f) * 2f * env
                        + Mathf.Sin(2f * Mathf.PI * 65f * t) * env * 0.8f;
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            _thunk = AudioClip.Create("Thunk", length, 1, sampleRate, false);
            _thunk.SetData(data, 0);
            return _thunk;
        }
    }

    private static AudioClip _punch;
    /// <summary>Удар кулаком.</summary>
    public static AudioClip Punch
    {
        get
        {
            if (_punch != null) return _punch;
            const int sampleRate = 22050;
            const int length = sampleRate / 5;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 40f);
                float v = Mathf.Sin(2f * Mathf.PI * 120f * t) * env + (Mathf.PerlinNoise(t * 500f, 9f) - 0.5f) * env;
                data[i] = Mathf.Clamp(v, -1f, 1f) * 0.9f;
            }
            _punch = AudioClip.Create("Punch", length, 1, sampleRate, false);
            _punch.SetData(data, 0);
            return _punch;
        }
    }

    private static AudioClip _scream;
    /// <summary>Комедийный крик падающего пассажира.</summary>
    public static AudioClip Scream
    {
        get
        {
            if (_scream != null) return _scream;
            const int sampleRate = 22050;
            const int length = sampleRate;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                // Нисходящий свип с вибрато.
                float f = Mathf.Lerp(900f, 320f, t) * (1f + 0.03f * Mathf.Sin(t * 55f));
                float env = Mathf.Clamp01(t * 30f) * Mathf.Exp(-t * 2.2f);
                float v = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f
                         + Mathf.Sin(2f * Mathf.PI * f * 2.01f * t) * 0.25f
                         + Mathf.Sin(2f * Mathf.PI * f * 3.02f * t) * 0.1f) * env;
                data[i] = Mathf.Clamp(v, -1f, 1f) * 0.85f;
            }
            _scream = AudioClip.Create("Scream", length, 1, sampleRate, false);
            _scream.SetData(data, 0);
            return _scream;
        }
    }

    private static AudioClip _beep;
    /// <summary>Радио-писк капитана.</summary>
    public static AudioClip Beep
    {
        get
        {
            if (_beep != null) return _beep;
            const int sampleRate = 22050;
            const int length = sampleRate / 10;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Clamp01(t * 200f) * Mathf.Exp(-t * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1050f * t) * env * 0.5f;
            }
            _beep = AudioClip.Create("Beep", length, 1, sampleRate, false);
            _beep.SetData(data, 0);
            return _beep;
        }
    }

    private static AudioClip _click;
    /// <summary>Клик интерфейса.</summary>
    public static AudioClip Click
    {
        get
        {
            if (_click != null) return _click;
            const int sampleRate = 22050;
            const int length = sampleRate / 40;
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                data[i] = (Mathf.PerlinNoise(t * 900f, 2f) - 0.5f) * Mathf.Exp(-t * 260f) * 0.6f;
            }
            _click = AudioClip.Create("Click", length, 1, sampleRate, false);
            _click.SetData(data, 0);
            return _click;
        }
    }

    // ---------- Эмбиент сцены ----------

    private AudioSource _engineSourceL;
    private AudioSource _engineSourceR;
    private AudioSource _windSource;

    private void Awake()
    {
        _engineSourceL = gameObject.AddComponent<AudioSource>();
        _engineSourceL.clip = Engine;
        _engineSourceL.loop = true;
        _engineSourceL.volume = 0f;
        _engineSourceL.spatialBlend = 0f;
        _engineSourceL.Play();

        _engineSourceR = gameObject.AddComponent<AudioSource>();
        _engineSourceR.clip = Engine;
        _engineSourceR.loop = true;
        _engineSourceR.volume = 0f;
        _engineSourceR.spatialBlend = 0f;
        _engineSourceR.Play();

        _windSource = gameObject.AddComponent<AudioSource>();
        _windSource.clip = Wind;
        _windSource.loop = true;
        _windSource.volume = 0f;
        _windSource.spatialBlend = 0f;
        _windSource.Play();
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null)
        {
            if (_engineSourceL != null) _engineSourceL.volume = Mathf.MoveTowards(_engineSourceL.volume, 0f, Time.deltaTime);
            if (_engineSourceR != null) _engineSourceR.volume = Mathf.MoveTowards(_engineSourceR.volume, 0f, Time.deltaTime);
            if (_windSource != null) _windSource.volume = Mathf.MoveTowards(_windSource.volume, 0f, Time.deltaTime);
            return;
        }

        // Тяга двигателей → громкость и высота тона. Повреждённый двигатель — неровный, тише.
        float throttle = session.Throttle.Value;
        float healthL = session.Engine1Health.Value / 100f;
        float healthR = session.Engine2Health.Value / 100f;
        float baseVol = Mathf.Lerp(0.05f, 0.4f, throttle);
        float pitch = Mathf.Lerp(0.6f, 1.5f, throttle);

        _engineSourceL.volume = baseVol * Mathf.Lerp(0.35f, 1f, healthL);
        _engineSourceL.pitch = pitch * Mathf.Lerp(0.85f, 1f, healthL) * (0.98f + Mathf.PerlinNoise(Time.time * 7f, 1f) * 0.04f);

        _engineSourceR.volume = baseVol * Mathf.Lerp(0.35f, 1f, healthR);
        _engineSourceR.pitch = pitch * Mathf.Lerp(0.85f, 1f, healthR) * (0.98f + Mathf.PerlinNoise(Time.time * 7f, 5f) * 0.04f);

        // Ветер: сильнее на высоте и при выходе на крыло (учитываем PlayerController).
        float windBase = Mathf.Lerp(0.02f, 0.12f, session.SpeedKmh.Value / 400f);
        bool onWing = PlayerController.Local != null && PlayerController.Local.isOnWing;
        _windSource.volume = onWing ? 0.55f : windBase;
    }

    /// <summary>Проиграть клип в точке мира (3D).</summary>
    public static void PlayAt(Vector3 position, AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        var go = new GameObject("OneShotAudio");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume * GameSettings.Current.sfxVolume;
        src.spatialBlend = 1f;
        src.minDistance = 3f;
        src.maxDistance = 80f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    /// <summary>Проиграть 2D-клик интерфейса.</summary>
    public static void PlayUiClick()
    {
        var src = new GameObject("UiClick").AddComponent<AudioSource>();
        src.clip = Click;
        src.volume = 0.7f * GameSettings.Current.sfxVolume;
        src.spatialBlend = 0f;
        src.Play();
        Object.Destroy(src.gameObject, 0.3f);
    }
}
