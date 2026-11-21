// BirdSpawner.cs + Bird — стаи птиц. Симуляция детерминирована seed'ом: все клиенты
// видят одну и ту же стаю без трафика на каждую птицу. Хост проверяет столкновения
// с двигателями и лобовым стеклом.

using System.Collections.Generic;
using UnityEngine;

/// <summary>Стая птиц (спавнер + симуляция на каждом пире).</summary>
public class BirdSpawner : MonoBehaviour
{
    public static BirdSpawner Instance { get; set; }

    private class Bird
    {
        public Transform t;
        public Vector3 velocity;
        public float phase;
        public bool alive = true;
    }

    private readonly List<Bird> _birds = new List<Bird>();
    private float _spawnTimer = 14f;
    private int _seedCounter;
    private Material _birdMat;
    private Material _featherMat;

    private void Awake()
    {
        Instance = this;
        _birdMat = ProceduralAssets.Mat("Bird", new Color(0.12f, 0.12f, 0.14f));
        _featherMat = ProceduralAssets.TexMat("Feather", ProceduralAssets.CloudTexture,
            new Color(0.95f, 0.95f, 0.92f, 0.95f), billboard: true);
    }

    private void OnEnable()
    {
        var session = GameSession.Instance;
        if (session != null) session.FlockSpawned += OnFlockSpawned;
    }

    private void Start()
    {
        // GameSession может появиться позже — подписываемся при первом Update тоже.
        StartCoroutine(SubscribeWhenReady());
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        while (GameSession.Instance == null) yield return new WaitForSeconds(0.5f);
        GameSession.Instance.FlockSpawned -= OnFlockSpawned; // избегаем дублей
        GameSession.Instance.FlockSpawned += OnFlockSpawned;
    }

    private void OnDisable()
    {
        if (GameSession.Instance != null) GameSession.Instance.FlockSpawned -= OnFlockSpawned;
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null) return;

        // Хост решает, когда заспавнить стаю.
        if (NetworkLauncher.IsServer && session.Phase.Value == (int)GamePhase.Flight)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = Random.Range(26f, 55f);
                _seedCounter++;
                int seed = (int)(Time.timeAsDouble * 1000f) ^ (_seedCounter * 7919);
                session.HostSpawnFlock(seed);
            }
        }

        SimulateFlock(session);
    }

    /// <summary>Заспавнить стаю (все пиры, детерминированно по seed).</summary>
    private void OnFlockSpawned(int seed)
    {
        var rng = new System.Random(seed);
        int count = 5 + rng.Next(0, 9);
        for (int i = 0; i < count; i++)
        {
            var b = new Bird();
            var go = new GameObject("Bird");
            // Стая рождается далеко впереди-сверху или снизу, курсом на самолёт.
            float side = rng.Next(0, 2) == 0 ? -1f : 1f;
            Vector3 pos = new Vector3(
                side * (10f + (float)rng.NextDouble() * 30f),
                -6f + (float)rng.NextDouble() * 16f,
                300f + (float)rng.NextDouble() * 80f);
            go.transform.position = pos;
            b.t = go.transform;
            b.velocity = new Vector3(-side * 0.4f, 0f, -26f) + new Vector3(0f, 0f, (float)rng.NextDouble() * -6f);
            b.phase = (float)rng.NextDouble() * Mathf.PI * 2f;

            // Визуал: тельце + два машущих крыла.
            ProceduralAssets.Sphere(go.transform, "Body", Vector3.zero, 0.16f, _birdMat);
            var wingL = ProceduralAssets.Box(go.transform, "WingL", new Vector3(-0.3f, 0.05f, 0f), Vector3.zero,
                new Vector3(0.55f, 0.03f, 0.22f), _birdMat);
            var wingR = ProceduralAssets.Box(go.transform, "WingR", new Vector3(0.3f, 0.05f, 0f), Vector3.zero,
                new Vector3(0.55f, 0.03f, 0.22f), _birdMat);
            _birds.Add(b);
        }
    }

    /// <summary>Движение стаи + (только хост) проверки столкновений.</summary>
    private void SimulateFlock(GameSession session)
    {
        var airplane = AirplaneController.Instance;
        bool host = NetworkLauncher.IsServer;

        for (int i = _birds.Count - 1; i >= 0; i--)
        {
            var b = _birds[i];
            if (b == null || b.t == null) { _birds.RemoveAt(i); continue; }

            float dt = Time.deltaTime;
            // Плавное подруливание к самолёту (машут крыльями).
            b.phase += dt * 9f;
            Vector3 toPlane = -b.t.position;
            toPlane.z = 0f;
            b.velocity += Vector3.ClampMagnitude(toPlane * 0.35f * dt, 3f * dt);
            b.t.position += b.velocity * dt;
            b.t.rotation = Quaternion.LookRotation(b.velocity);

            // Взмахи крыльев.
            float flap = Mathf.Sin(b.phase) * 40f;
            var wl = b.t.Find("WingL");
            var wr = b.t.Find("WingR");
            if (wl != null) wl.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (wr != null) wr.localRotation = Quaternion.Euler(0f, 0f, -flap);

            // Пролетели мимо — убираем.
            if (b.t.position.z < -60f || Mathf.Abs(b.t.position.x) > 120f || Mathf.Abs(b.t.position.y) > 90f)
            {
                Destroy(b.t.gameObject);
                _birds.RemoveAt(i);
                continue;
            }

            // Столкновения — только хост (двигатели/лобовое).
            if (host && airplane != null && airplane.Refs != null)
            {
                Vector3 p = b.t.position;
                var refs = airplane.Refs;
                float engineR = 1.4f;
                bool hitEngineL = refs.EngineL != null &&
                    (p - refs.EngineL.position).sqrMagnitude < engineR * engineR;
                bool hitEngineR = refs.EngineR != null &&
                    (p - refs.EngineR.position).sqrMagnitude < engineR * engineR;
                bool hitWindshield = (p - new Vector3(0f, 0.55f, 6.6f)).sqrMagnitude < 1.1f * 1.1f;

                if (hitEngineL || hitEngineR || hitWindshield)
                {
                    session.StatBirdsEaten.Value++;
                    if (hitEngineL) airplane.ApplyBirdHit(0, Random.Range(30f, 60f));
                    if (hitEngineR) airplane.ApplyBirdHit(1, Random.Range(30f, 60f));
                    if (hitWindshield) airplane.ApplyWindshieldHit();
                    session.HostSfx(p, 0);
                    session.HostAnnounce(AnnouncementType.BirdStrike);
                    if (PassengerManager.Instance != null)
                    {
                        PassengerManager.Instance.AddStressAll(15f, "ЧТО ЭТО БЫЛО?!");
                    }
                    SpawnFeathers(p);
                    Destroy(b.t.gameObject);
                    _birds.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>Взрыв перьев (локальный визуал).</summary>
    private void SpawnFeathers(Vector3 pos)
    {
        for (int i = 0; i < 7; i++)
        {
            var f = ProceduralAssets.Quad(transform, "Feather", pos + Random.insideUnitSphere * 0.4f,
                Vector3.zero, new Vector2(0.35f, 0.35f), _featherMat);
            f.transform.rotation = Random.rotation;
            Destroy(f, 1.4f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
