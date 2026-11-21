// ProceduralAssets.cs — фабрика всего контента: материалы, текстуры, примитивы, звук.
// Проект не содержит бинарных ассетов (кроме меню-фона): весь мир строится кодом.
// Все материалы выбираются с учётом активного рендер-пайплайна (URP или Built-in fallback).

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Статическая фабрика процедурных ресурсов с кэшем.</summary>
public static class ProceduralAssets
{
    // ---------- Материалы ----------

    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    /// <summary>true, если активен URP (иначе — Built-in, материалы выбираются автоматически).</summary>
    public static bool UsingUrp => GraphicsSettings.currentRenderPipeline != null;

    private static Shader LitShader => Shader.Find(UsingUrp ? "Universal Render Pipeline/Lit" : "Standard");
    private static Shader UnlitShader => Shader.Find(UsingUrp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
    private static Shader UnlitTextureShader => Shader.Find(UsingUrp ? "Universal Render Pipeline/Unlit" : "Unlit/Texture");

    /// <summary>Материал с базовым цветом (кэшируется по имени).</summary>
    public static Material Mat(string name, Color color, bool lit = true, float smoothness = 0.2f, float metallic = 0f)
    {
        string key = $"{name}|{lit}|{smoothness:F2}|{metallic:F2}";
        if (materialCache.TryGetValue(key, out var cached)) return cached;

        var shader = lit ? LitShader : UnlitShader;
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        materialCache[key] = mat;
        return mat;
    }

    /// <summary>Материал-текстура (unlit) — облака, спрайты-биллборды.</summary>
    public static Material TexMat(string name, Texture2D texture, Color tint, bool billboard = false)
    {
        string key = $"{name}|{tint.r:F2}{tint.g:F2}{tint.b:F2}{tint.a:F2}";
        if (materialCache.TryGetValue(key, out var cached)) return cached;

        // Спрайтовый шейдер работает и в URP, и в Built-in и умеет альфу.
        var shader = Shader.Find(billboard ? "Sprites/Default" : UnlitTextureShader);
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.name = name;
        if (texture != null)
        {
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
        }
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        mat.renderQueue = 3000; // прозрачное поверх
        materialCache[key] = mat;
        return mat;
    }

    // ---------- Текстуры ----------

    private static Texture2D _cloudTex;
    /// <summary>Мягкое пятно (облака, свечения).</summary>
    public static Texture2D CloudTexture
    {
        get
        {
            if (_cloudTex != null) return _cloudTex;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Мягкий круг с небольшим шумом по краям — похоже на клочковатое облако.
                    float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.25f;
                    float a = Mathf.Clamp01((1f - d) + n - 0.15f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(cols);
            tex.Apply();
            _cloudTex = tex;
            return tex;
        }
    }

    private static Texture2D _circleTex;
    /// <summary>Резкий круг (индикаторы, кольца мини-игры).</summary>
    public static Texture2D CircleTexture
    {
        get
        {
            if (_circleTex != null) return _circleTex;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool ring = d < 0.92f && d > 0.78f; // кольцо
                    bool dot = d < 0.35f;               // центральная точка
                    cols[y * size + x] = (ring || dot) ? Color.white : Color.clear;
                }
            tex.SetPixels(cols);
            tex.Apply();
            _circleTex = tex;
            return tex;
        }
    }

    private static Texture2D _gridTex;
    /// <summary>Клетчатая текстура земли (поля под самолётом).</summary>
    public static Texture2D GridTexture
    {
        get
        {
            if (_gridTex != null) return _gridTex;
            const int size = 256;
            const int cell = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / cell) + (y / cell)) % 2 == 0;
                    float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.1f;
                    Color c = even
                        ? new Color(0.30f + n, 0.42f + n, 0.22f + n, 1f)
                        : new Color(0.26f + n, 0.38f + n, 0.20f + n, 1f);
                    cols[y * size + x] = c;
                }
            tex.SetPixels(cols);
            tex.Apply();
            _gridTex = tex;
            return tex;
        }
    }

    private static Texture2D _crackTex;
    /// <summary>Трещины на лобовом стекле (оверлей HUD).</summary>
    public static Texture2D CrackTexture
    {
        get
        {
            if (_crackTex != null) return _crackTex;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
            var rng = new System.Random(42);
            for (int crack = 0; crack < 14; crack++)
            {
                int x = rng.Next(size / 4, size * 3 / 4);
                int y = rng.Next(size / 4, size * 3 / 4);
                int len = rng.Next(20, 70);
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2);
                for (int s = 0; s < len; s++)
                {
                    x += (int)Mathf.Round(Mathf.Cos(ang) * 1.5f);
                    y += (int)Mathf.Round(Mathf.Sin(ang) * 1.5f);
                    ang += (float)(rng.NextDouble() - 0.5) * 0.4f;
                    if (x < 0 || y < 0 || x >= size || y >= size) break;
                    for (int ox = -1; ox <= 1; ox++)
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int px = x + ox, py = y + oy;
                            if (px >= 0 && py >= 0 && px < size && py < size)
                                cols[py * size + px] = new Color(1f, 1f, 1f, 0.85f);
                        }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            _crackTex = tex;
            return tex;
        }
    }

    /// <summary>Спрайт из текстуры (для UI Image).</summary>
    public static Sprite MakeSprite(Texture2D tex)
        => Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

    // ---------- Примитивы ----------

    /// <summary>Создать бокс (по умолчанию без коллайдера) с материалом.</summary>
    public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 localEuler,
        Vector3 size, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Создать сферу.</summary>
    public static GameObject Sphere(Transform parent, string name, Vector3 localPos, float radius,
        Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Создать цилиндр (по умолчанию ось Y; поворот задаётся отдельно).</summary>
    public static GameObject Cylinder(Transform parent, string name, Vector3 localPos, Vector3 localEuler,
        float radius, float height, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Создать квад (плоскость 1x1) — биллборды, стекла, декали.</summary>
    public static GameObject Quad(Transform parent, string name, Vector3 localPos, Vector3 localEuler,
        Vector2 size, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // ---------- Шрифт / TMP ----------

    private static Font _builtinFont;
    /// <summary>Встроенный шрифт для legacy Text / TextMesh.</summary>
    public static Font BuiltinFont
    {
        get
        {
            if (_builtinFont != null) return _builtinFont;
            try { _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { _builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            return _builtinFont;
        }
    }

    private static bool _tmpChecked;
    private static bool _tmpReady;
    /// <summary>true, если TMP Essentials импортированы и можно использовать TextMeshProUGUI.</summary>
    public static bool TmpReady
    {
        get
        {
            if (_tmpChecked) return _tmpReady;
            _tmpChecked = true;
            try { _tmpReady = TMP_Settings.defaultFontAsset != null; }
            catch { _tmpReady = false; }
            return _tmpReady;
        }
    }

    // ---------- Палитра ----------

    /// <summary>Палитра «затертая авиация»: белый корпус, оранжевые акценты, серый интерьер.</summary>
    public static class Palette
    {
        public static readonly Color Fuselage = new Color(0.92f, 0.93f, 0.95f);
        public static readonly Color Accent = new Color(0.95f, 0.45f, 0.13f);
        public static readonly Color Metal = new Color(0.55f, 0.58f, 0.62f);
        public static readonly Color DarkMetal = new Color(0.22f, 0.24f, 0.27f);
        public static readonly Color Interior = new Color(0.72f, 0.70f, 0.66f);
        public static readonly Color SeatBlue = new Color(0.16f, 0.25f, 0.45f);
        public static readonly Color FloorDark = new Color(0.35f, 0.33f, 0.30f);
        public static readonly Color Glass = new Color(0.55f, 0.75f, 0.85f, 0.45f);
        public static readonly Color Warning = new Color(0.95f, 0.25f, 0.15f);
        public static readonly Color Calm = new Color(0.25f, 0.75f, 0.35f);
        public static readonly Color PanicYellow = new Color(0.95f, 0.8f, 0.2f);
    }
}
