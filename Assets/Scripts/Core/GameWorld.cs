// GameWorld.cs — процедурный мир вокруг самолёта: небо, солнце, облака, земля, горы, аэропорт.
// Архитектура «мировой прокрутки»: самолёт неподвижен в начале координат (внутри него стабильная
// физика для ходьбы), а мир движется мимо — это делает кооп и ходьбу по салону надёжными.

using System.Collections.Generic;
using UnityEngine;

/// <summary>Мир вокруг самолёта. Обновляется из сетевого состояния GameSession.</summary>
public class GameWorld : MonoBehaviour
{
    public static GameWorld Instance { get; private set; }

    /// <summary>true — сцена стоянки (самолёт на перроне, мир статичен).</summary>
    public bool boardingScene = false;

    private Transform _tiltRoot;      // наклоняется тангажом/креном (горизонт «кренится»)
    private Transform _scrollRoot;    // прокручивается назад со скоростью самолёта
    private Transform _groundRoot;    // опускается на текущую высоту
    private Transform _cloudRoot;     // облака (физическая прокрутка с рециклом)
    private Transform _airportRoot;   // аэропорт назначения (появляется при заходе на посадку)

    private Renderer _groundRenderer; // земля: прокрутка текстурой
    private Light _sun;
    private readonly List<Transform> _clouds = new List<Transform>();
    private readonly List<Transform> _mountains = new List<Transform>();

    // Параметры качества (настраиваются по GameSettings)
    private float _farPlane = 900f;
    private float _shadowDistance = 120f;

    private void Awake()
    {
        Instance = this;
        ApplyQuality();
        BuildSky();
        BuildWorld();
    }

    private void ApplyQuality()
    {
        int q = Mathf.Clamp(GameSettings.Current.qualityLevel, 0, 2);
        _farPlane = q == 0 ? 450f : q == 1 ? 900f : 1600f;
        _shadowDistance = q == 0 ? 0f : q == 1 ? 80f : 160f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartColor = new Color(0.62f, 0.66f, 0.74f);
        RenderSettings.fogColor = RenderSettings.fogStartColor;
    }

    /// <summary>Настроить туман и дальность для конкретной камеры игрока.</summary>
    public void ConfigureCamera(Camera cam)
    {
        cam.farClipPlane = _farPlane;
        RenderSettings.fogStartDistance = _farPlane * 0.35f;
        RenderSettings.fogEndDistance = _farPlane * 0.95f;
    }

    private void BuildSky()
    {
        // Солнце
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(transform, false);
        sunGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        _sun = sunGo.AddComponent<Light>();
        _sun.type = LightType.Directional;
        _sun.color = new Color(1f, 0.96f, 0.88f);
        _sun.intensity = 1.15f;
        _sun.shadows = _shadowDistance > 0f ? LightShadows.Soft : LightShadows.None;
        _sun.shadowDistance = Mathf.Max(40f, _shadowDistance);
        RenderSettings.sun = _sun;

        // Процедурное небо (встроенный шейдер работает и в URP, и в Built-in).
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetColor("_SkyTint", new Color(0.45f, 0.55f, 0.75f));
            sky.SetFloat("_AtmosphereThickness", 0.95f);
            sky.SetFloat("_Exposure", 1.05f);
            RenderSettings.skybox = sky;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.44f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.24f);
    }

    private void BuildWorld()
    {
        _tiltRoot = new GameObject("TiltRoot").transform;
        _tiltRoot.SetParent(transform, false);
        _scrollRoot = new GameObject("ScrollRoot").transform;
        _scrollRoot.SetParent(_tiltRoot, false);
        _groundRoot = new GameObject("GroundRoot").transform;
        _groundRoot.SetParent(_scrollRoot, false);
        _cloudRoot = new GameObject("CloudRoot").transform;
        _cloudRoot.SetParent(_tiltRoot, false);

        // --- Земля: одна огромная плоскость с прокручиваемой текстурой полей.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        Destroy(ground.GetComponent<Collider>());
        ground.transform.SetParent(_groundRoot, false);
        ground.transform.localScale = new Vector3(400f, 1f, 400f); // 4000x4000 м
        var groundMat = new Material(Shader.Find(ProceduralAssets.UsingUrp
            ? "Universal Render Pipeline/Lit" : "Standard"));
        groundMat.name = "Ground";
        if (groundMat.HasProperty("_BaseMap")) groundMat.SetTexture("_BaseMap", ProceduralAssets.GridTexture);
        if (groundMat.HasProperty("_MainTex")) groundMat.SetTexture("_MainTex", ProceduralAssets.GridTexture);
        groundMat.mainTextureScale = new Vector2(400f, 400f); // один тайл текстуры = 10 м
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
        _groundRenderer = ground.GetComponent<MeshRenderer>();

        // --- Горы и леса: редкие конусы, рециклятся по Z.
        var mountainMat = ProceduralAssets.Mat("Mountain", new Color(0.38f, 0.42f, 0.36f));
        var forestMat = ProceduralAssets.Mat("Forest", new Color(0.16f, 0.34f, 0.16f));
        var rng = new System.Random(1234);
        for (int i = 0; i < 26; i++)
        {
            bool big = i % 3 == 0;
            var m = ProceduralAssets.Cylinder(_groundRoot, "Mountain", Vector3.zero, Vector3.zero,
                big ? 90f + rng.Next(0, 70) : 30f + rng.Next(0, 30),
                big ? 120f + rng.Next(0, 90) : 40f + rng.Next(0, 40), mountainMat);
            m.transform.localPosition = new Vector3(
                (rng.Next(0, 2) == 0 ? -1 : 1) * (350f + rng.Next(0, 900)),
                big ? 0.5f : 0.2f,
                rng.Next(-1800, 1800));
            // Плоские «столовые горы» — стилизованный рельеф.
            _mountains.Add(m.transform);
        }
        for (int i = 0; i < 40; i++)
        {
            var f = ProceduralAssets.Cylinder(_groundRoot, "Forest", Vector3.zero, Vector3.zero,
                8f + rng.Next(0, 6), 14f + rng.Next(0, 10), forestMat);
            f.transform.localPosition = new Vector3(
                (rng.Next(0, 2) == 0 ? -1 : 1) * (220f + rng.Next(0, 700)),
                0.1f, rng.Next(-1800, 1800));
            _mountains.Add(f.transform);
        }

        // --- Облака: биллборды-квады с мягкой текстурой, рециклятся.
        var cloudMat = ProceduralAssets.TexMat("Cloud", ProceduralAssets.CloudTexture,
            new Color(1f, 1f, 1f, 0.85f), billboard: true);
        var crng = new System.Random(77);
        for (int i = 0; i < 34; i++)
        {
            float scale = 60f + crng.Next(0, 120);
            var c = ProceduralAssets.Quad(_cloudRoot, "Cloud", Vector3.zero, Vector3.zero,
                new Vector2(scale, scale * 0.45f), cloudMat);
            c.transform.localPosition = new Vector3(
                (crng.Next(0, 2) == 0 ? -1 : 1) * crng.Next(60, 700),
                -60f + crng.Next(-4, 60),
                crng.Next(-1500, 400));
            _clouds.Add(c.transform);
        }

        // --- Аэропорт назначения: появляется в конце полёта (движется к нам).
        _airportRoot = new GameObject("Airport").transform;
        _airportRoot.SetParent(_scrollRoot, false);
        BuildAirport(_airportRoot);
        _airportRoot.gameObject.SetActive(false);

        if (boardingScene)
        {
            // На стоянке: перрон, терминал и буксировщик — мир стоит.
            _groundRoot.localPosition = new Vector3(0f, -2.6f, 0f);
            BuildApron();
        }
        else
        {
            _groundRoot.localPosition = new Vector3(0f, -300f, 0f); // дефолт до прихода GameSession
        }
    }

    /// <summary>Аэропорт: полоса, огни, вышка, здание.</summary>
    private void BuildAirport(Transform root)
    {
        var asphalt = ProceduralAssets.Mat("Asphalt", new Color(0.16f, 0.17f, 0.18f));
        var stripe = ProceduralAssets.Mat("Stripe", new Color(0.9f, 0.9f, 0.85f));
        var tower = ProceduralAssets.Mat("Tower", new Color(0.75f, 0.72f, 0.66f));

        var runway = ProceduralAssets.Box(root, "Runway", new Vector3(0, 0.2f, 0), Vector3.zero,
            new Vector3(40f, 0.4f, 600f), asphalt);
        for (int i = 0; i < 20; i++)
        {
            ProceduralAssets.Box(root, "Marking", new Vector3(0, 0.45f, -280f + i * 30f), Vector3.zero,
                new Vector3(1.2f, 0.1f, 14f), stripe);
        }
        // Огни ВПП
        var lightMat = ProceduralAssets.Mat("RunwayLight", new Color(1f, 0.85f, 0.3f), lit: false);
        for (int i = 0; i < 24; i++)
        {
            ProceduralAssets.Sphere(root, "RLight", new Vector3(-21f, 0.7f, -290f + i * 25f), 0.5f, lightMat);
            ProceduralAssets.Sphere(root, "RLight", new Vector3(21f, 0.7f, -290f + i * 25f), 0.5f, lightMat);
        }
        ProceduralAssets.Box(root, "Terminal", new Vector3(60f, 6f, 0), Vector3.zero,
            new Vector3(30f, 12f, 120f), tower);
        ProceduralAssets.Cylinder(root, "ControlTower", new Vector3(60f, 22f, 90f), Vector3.zero, 3f, 24f, tower);
    }

    /// <summary>Перрон в сцене посадки.</summary>
    private void BuildApron()
    {
        var apron = ProceduralAssets.Box(_groundRoot, "Apron", new Vector3(0f, 0.05f, 0f), Vector3.zero,
            new Vector3(160f, 0.2f, 220f), ProceduralAssets.Mat("Apron", new Color(0.32f, 0.33f, 0.35f)));
        apron.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        var building = ProceduralAssets.Box(_groundRoot, "TerminalBuilding", new Vector3(-55f, 7f, -10f),
            Vector3.zero, new Vector3(24f, 14f, 140f),
            ProceduralAssets.Mat("Terminal", new Color(0.62f, 0.64f, 0.66f)));
        // Окна терминала — светящаяся полоса.
        ProceduralAssets.Box(building.transform, "Windows", new Vector3(12.6f, 2f, 0f), Vector3.zero,
            new Vector3(0.2f, 4f, 120f), ProceduralAssets.Mat("Windows", new Color(0.85f, 0.9f, 1f), lit: false));
        // Разметка стоянки.
        ProceduralAssets.Box(_groundRoot, "StandLine", new Vector3(0f, 0.1f, 8f), Vector3.zero,
            new Vector3(1f, 0.05f, 160f), ProceduralAssets.Mat("Line", new Color(0.9f, 0.85f, 0.2f)));
        // Трап у носа самолёта.
        var stairMat = ProceduralAssets.Mat("Stairs", ProceduralAssets.Palette.Metal);
        var stairs = ProceduralAssets.Box(_groundRoot, "Stairs", new Vector3(-2.6f, 0.9f, 9.5f), Vector3.zero,
            new Vector3(1.6f, 0.25f, 3.6f), stairMat);
        ProceduralAssets.Box(stairs.transform, "Rail", new Vector3(0f, 0.9f, 0f), Vector3.zero,
            new Vector3(0.12f, 1.8f, 3.6f), stairMat);
        // Буксировщик.
        var tug = ProceduralAssets.Box(_groundRoot, "Tug", new Vector3(4.5f, 0.7f, 14f), new Vector3(0, 25f, 0),
            new Vector3(1.8f, 1.2f, 3.2f), ProceduralAssets.Mat("Tug", ProceduralAssets.Palette.Accent));
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null) return;

        float speed = session.SpeedKmh.Value / 3.6f;      // м/с
        float heading = session.HeadingDeg.Value;
        float pitch = session.PitchDeg.Value;
        float roll = session.RollDeg.Value;
        float altitude = session.AltitudeM.Value;
        int phase = session.Phase.Value;

        if (boardingScene)
        {
            // На стоянке мир статичен — только лёгкое покачивание ничего не двигаем.
            return;
        }

        // Направление полёта в плоскости мира (курс 0 = на север = -Z... условно +Z вперёд носа).
        float rad = heading * Mathf.Deg2Rad;
        var forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        // 1) Прокрутка мира назад со скоростью самолёта.
        _scrollRoot.position -= forward * (speed * Time.deltaTime);

        // 2) Земля держит дистанцию = высоте полёта.
        _groundRoot.localPosition = new Vector3(0f, -altitude, 0f);
        // Плоскость земли огромная и центрирована на scrollRoot — двигаем текстуру, чтобы
        // бесконечно «ехать» по полям (сам quad привязан к прокрутке).
        if (_groundRenderer != null)
        {
            var mat = _groundRenderer.sharedMaterial;
            // Плоскость 4000 м с тайлом 10 м → UV-сдвиг = пройденные метры / 10.
            Vector2 off = mat.mainTextureOffset;
            off += new Vector2(forward.x, forward.z) * (speed * Time.deltaTime / 10f);
            mat.mainTextureOffset = off;
        }

        // 3) Наклон мира = тангаж и крен (горизонт кренится вокруг нас).
        _tiltRoot.localRotation = Quaternion.Euler(pitch, 0f, -roll);

        // 4) Рецикл облаков и гор: всё, что улетело за хвост, переносится вперёд.
        WrapChildren(_clouds, 1400f, forward);
        WrapChildren(_mountains, 1900f, forward);

        // 5) Аэропорт назначения появляется при заходе на посадку.
        bool landing = phase == (int)GamePhase.Landing || phase == (int)GamePhase.Landed;
        if (landing && !_airportRoot.gameObject.activeSelf)
        {
            _airportRoot.gameObject.SetActive(true);
            _airportRoot.position = _scrollRoot.position + forward * 2200f + new Vector3(0, -altitude, 0);
        }
        if (_airportRoot.gameObject.activeSelf)
        {
            // Аэропорт «наезжает» вместе с прокруткой; на касании фиксируем полосу под самолётом.
            float ahead = Vector3.Dot(_airportRoot.position - _scrollRoot.position, forward);
            if (ahead < 20f)
            {
                _airportRoot.position = _scrollRoot.position + forward * 20f;
            }
        }
    }

    /// <summary>Переносит объекты, ушедшие за хвост, вперёд по направлению полёта.</summary>
    private void WrapChildren(List<Transform> children, float wrapDistance, Vector3 forward)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var t = children[i];
            Vector3 rel = t.position - _scrollRoot.position;
            float along = Vector3.Dot(rel, forward);
            if (along < -wrapDistance * 0.5f)
            {
                t.position += forward * wrapDistance;
            }
        }
    }
}
