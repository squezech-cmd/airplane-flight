// AirplaneController.cs — «мозг» самолёта. Строит его процедурно на каждом пире,
// симулирует полёт только на хосте (авторитетно) и пишет состояние в GameSession (NetworkVariable).
// Модель аркадная: тяга/сопротивление/подъёмная/сваливание + топливо + пожары + посадка/крушение.

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Контроллер самолёта: симуляция (сервер) + визуал (все).</summary>
public class AirplaneController : MonoBehaviour
{
    public static AirplaneController Instance { get; set; }

    /// <summary>true — сцена стоянки (посадка пассажиров), false — полётная сцена.</summary>
    public bool boardingScene;

    public AirplaneRefs Refs { get; private set; }

    // ---------- Параметры модели (настройка баланса) ----------
    private const float MaxAccel = 4.6f;        // м/с² на полном газу при двух целых двигателях
    private const float DragK = 0.00045f;       // квадратичное сопротивление
    private const float PitchRate = 40f;        // град/с на полном отклонении
    private const float RollRate = 55f;
    private const float YawRate = 14f;
    private const float StallSpeed = 36f;       // м/с (~130 км/ч)
    private const float ClimbFactor = 0.38f;    // вертикальная скорость от тангажа
    private const float FuelBurnBase = 0.05f;   // л/с на холостых
    private const float FuelBurnThrottle = 0.28f; // л/с на полном газу

    // ---------- Локальная симуляция (только сервер) ----------
    private float _speed;        // м/с
    private float _alt;          // м
    private float _heading;      // град
    private float _pitch;        // град
    private float _roll;         // град
    private float _vertSpeed;    // м/с
    private float _turbNoiseSeed = 7.3f;
    private float _netVarTimer;
    private float _announceTimer;
    private bool _welcomeDone;

    // ---------- Визуал (все пиры) ----------
    private float _smokeTimer;
    private readonly List<SmokePuff> _puffs = new List<SmokePuff>();

    private class SmokePuff
    {
        public Transform t;
        public float life;
    }

    private void Awake()
    {
        Instance = this;
        Refs = AirplaneBuilder.Build(transform, boardingScene);
        transform.position = Vector3.zero; // самолёт всегда «в начале координат»

        // Точка спавна игроков — у входной двери.
        NetworkLauncher.SpawnPoint = new Vector3(-0.5f, 0.05f, 3.2f);
        NetworkLauncher.SpawnYaw = 180f;
    }

    private void Start()
    {
        // Хост: создаём сетевую сессию рейса, если её ещё нет (первый вход в игру).
        if (NetworkLauncher.IsServer && GameSession.Instance == null)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/GameSession");
            if (prefab != null && prefab.GetComponent<NetworkObject>() != null)
            {
                var go = Instantiate(prefab);
                go.name = "GameSession";
                go.GetComponent<NetworkObject>().Spawn(false); // переживает смену сцены
            }
            else
            {
                Debug.LogError("[AirplaneController] Префаб GameSession не готов. Запустите Assets → Dear Passengers → Настроить проект.");
            }
        }

        // Хост вошёл сразу в Flight-сцену (или продолжает): начинаем взлёт.
        if (NetworkLauncher.IsServer && !boardingScene &&
            GameSession.Instance != null &&
            GameSession.Instance.Phase.Value == (int)GamePhase.Boarding)
        {
            BeginTakeoff();
        }
    }

    /// <summary>Начать взлёт (сервер). Вызывается после подтверждения на стоянке.</summary>
    public void BeginTakeoff()
    {
        var session = GameSession.Instance;
        if (session == null || !NetworkLauncher.IsServer) return;
        if (!boardingScene)
        {
            session.Phase.Value = (int)GamePhase.Takeoff;
            _speed = 0f;
            _alt = 0f;
            _pitch = 0f;
            _roll = 0f;
            _heading = 0f;
        }
        else
        {
            // Стыходка: сначала все переходят в Flight-сцену, там начнётся Takeoff.
            if (NetworkLauncher.NM != null)
            {
                NetworkLauncher.NM.SceneManager.LoadScene(GameBootstrap.SceneFlight, LoadSceneMode.Single);
            }
        }
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null) return;

        if (NetworkLauncher.IsServer)
        {
            Simulate(session);
        }

        // ---------- Визуал у всех ----------
        float throttle = session.Throttle.Value;
        float propSpeed = 140f + throttle * 1500f;
        if (Refs.PropL != null) Refs.PropL.Rotate(0f, 0f, propSpeed * Time.deltaTime, Space.Self);
        if (Refs.PropR != null) Refs.PropR.Rotate(0f, 0f, propSpeed * Time.deltaTime, Space.Self);

        // Вибрация внешнего контура от турбулентности.
        float turb = session.Turbulence.Value;
        if (Refs.Exterior != null)
        {
            float shake = turb * 0.02f + (session.Engine1Fire.Value || session.Engine2Fire.Value ? 0.008f : 0f);
            Refs.Exterior.localPosition = new Vector3(
                (Mathf.PerlinNoise(Time.time * 9f, 0f) - 0.5f) * shake,
                (Mathf.PerlinNoise(Time.time * 9f, 3f) - 0.5f) * shake,
                0f);
        }

        // Дым от горящих двигателей.
        UpdateSmoke(session);

        // Напоминание пилоту о сваливании.
        if (session.Phase.Value == (int)GamePhase.Flight && session.SpeedKmh.Value / 3.6f < StallSpeed + 3f)
        {
            _announceTimer -= Time.deltaTime;
            if (_announceTimer <= 0f)
            {
                _announceTimer = 12f;
                if (NetworkLauncher.IsServer) session.HostAnnounce(AnnouncementType.Stall);
            }
        }
    }

    // ================= СЕРВЕРНАЯ СИМУЛЯЦИЯ =================

    private void Simulate(GameSession session)
    {
        float dt = Time.deltaTime;
        int phase = session.Phase.Value;

        // Взял ли кто-то штурвал.
        ulong pilot = session.PilotClientId.Value;
        bool hasPilot = pilot != GameSession.NoPilot;
        float inPitch = hasPilot ? session.PilotPitch.Value : 0f;
        float inRoll = hasPilot ? session.PilotRoll.Value : 0f;
        float inYaw = hasPilot ? session.PilotYaw.Value : 0f;
        float throttle = hasPilot ? session.PilotThrottle.Value : (phase == (int)GamePhase.Takeoff ? 1f : 0.35f);

        // Эффективность двигателей.
        float e1 = session.Engine1Health.Value * 0.01f;
        float e2 = session.Engine2Health.Value * 0.01f;
        float powerScale = (e1 + e2) * 0.5f; // 0..1
        if (session.Fuel.Value <= 0f) powerScale = 0f; // топливо кончилось

        if (phase == (int)GamePhase.Boarding)
        {
            // На стоянке самолёт стоит: движки на малом газу.
            session.Throttle.Value = 0.05f;
            session.SpeedKmh.Value = 0f;
            session.AltitudeM.Value = 0f;
            return;
        }

        // ---------- Управление ----------
        if (phase == (int)GamePhase.Takeoff)
        {
            // Взлёт полу-скриптованный: тяга 100%, на 170 км/ч поднимаем нос.
            _speed += MaxAccel * powerScale * dt;
            float kmh = _speed * 3.6f;
            _pitch = kmh > 170f ? Mathf.MoveTowards(_pitch, 10f, 8f * dt) : Mathf.MoveTowards(_pitch, 0f, 8f * dt);
            _roll = Mathf.MoveTowards(_roll, 0f, 30f * dt);
            _heading += inYaw * YawRate * dt * 0.5f;

            // Набор высоты.
            _vertSpeed = _speed * Mathf.Sin(_pitch * Mathf.Deg2Rad) * ClimbFactor;
            _alt = Mathf.Max(0f, _alt + _vertSpeed * dt);

            if (_alt >= 2500f)
            {
                session.Phase.Value = (int)GamePhase.Flight;
                session.HostAnnounce(AnnouncementType.Welcome);
                _welcomeDone = true;
            }
        }
        else if (phase == (int)GamePhase.Flight || phase == (int)GamePhase.Landing)
        {
            // ---------- Полёт ----------
            if (phase == (int)GamePhase.Flight)
            {
                _pitch += inPitch * PitchRate * dt;
                _pitch = Mathf.Clamp(_pitch, -22f, 24f);
                _roll += inRoll * RollRate * dt;
                _roll = Mathf.Clamp(_roll, -55f, 55f);
                _heading += inYaw * YawRate * dt;
                if (!hasPilot) { _pitch = Mathf.MoveTowards(_pitch, 0f, 6f * dt); _roll = Mathf.MoveTowards(_roll, 0f, 12f * dt); }
            }
            else
            {
                // Заход на посадку: плавно снижаемся к полосе.
                _pitch = Mathf.MoveTowards(_pitch, -4f, 3f * dt);
                _roll = Mathf.MoveTowards(_roll, 0f, 20f * dt);
                throttle = Mathf.Clamp(throttle, 0.15f, 0.4f);
            }

            // Крен «тянет» нос по курсу (координированный разворот).
            _heading += _roll * 0.08f * dt;
            if (_heading < 0f) _heading += 360f;
            if (_heading >= 360f) _heading -= 360f;

            // Скорость.
            float drag = DragK * _speed * _speed;
            _speed += (MaxAccel * powerScale * throttle - drag) * dt;
            _speed = Mathf.Max(0f, _speed);

            // Сваливание.
            if (_speed < StallSpeed && _alt > 1f)
            {
                _pitch = Mathf.MoveTowards(_pitch, -14f, 20f * dt); // нос падает
            }

            // Вертикальная скорость.
            _vertSpeed = _speed * Mathf.Sin(_pitch * Mathf.Deg2Rad) * ClimbFactor;
            _speed -= Mathf.Max(0f, _vertSpeed) * 0.06f * dt * 10f; // набор высоты «съедает» скорость

            _alt += _vertSpeed * dt;
            _alt = Mathf.Max(0f, _alt);

            // ---------- Топливо ----------
            if (session.Fuel.Value > 0f)
            {
                float burn = (FuelBurnBase + FuelBurnThrottle * throttle) * dt;
                session.Fuel.Value = Mathf.Max(0f, session.Fuel.Value - burn);
            }

            // ---------- Турбулентность (шумовые зоны по дистанции) ----------
            float dist = session.DistanceToDestM.Value;
            float zone = Mathf.PerlinNoise(dist * 0.0022f, _turbNoiseSeed);
            float turb = zone > 0.55f ? Mathf.Clamp01((zone - 0.55f) / 0.3f) : 0f;
            if (phase == (int)GamePhase.Landing) turb *= 0.4f;
            session.Turbulence.Value = turb;
            if (turb > 0.4f)
            {
                // Порывы болтает.
                _pitch += (Mathf.PerlinNoise(Time.time * 3.1f, 11f) - 0.5f) * turb * 6f * dt;
                _roll += (Mathf.PerlinNoise(Time.time * 2.7f, 31f) - 0.5f) * turb * 14f * dt;
                _alt += (Mathf.PerlinNoise(Time.time * 1.3f, 71f) - 0.5f) * turb * 9f * dt;
                _announceTimer -= dt;
                if (_announceTimer <= 0f && turb > 0.65f)
                {
                    _announceTimer = 45f;
                    session.HostAnnounce(AnnouncementType.Turbulence);
                }
            }

            // ---------- Дистанция и фазы ----------
            if (phase == (int)GamePhase.Flight)
            {
                int newDist = Mathf.Max(0, (int)(dist - _speed * dt));
                session.DistanceToDestM.Value = newDist;
                if (newDist < 2500)
                {
                    session.Phase.Value = (int)GamePhase.Landing;
                    session.HostAnnounce(AnnouncementType.Landing);
                }

                // Крушение: врезались в землю в полётной фазе.
                if (_alt <= 0.5f)
                {
                    Crash(session);
                    return;
                }
            }
            else if (phase == (int)GamePhase.Landing)
            {
                session.DistanceToDestM.Value = Mathf.Max(0, (int)(dist - _speed * dt));
                if (_alt <= 0.6f)
                {
                    _alt = 0f;
                    _speed = Mathf.MoveTowards(_speed, 0f, 8f * dt);
                    session.Phase.Value = (int)GamePhase.Landed;
                    session.HostAnnounce(AnnouncementType.Landing);
                }
            }
        }
        else if (phase == (int)GamePhase.Landed || phase == (int)GamePhase.Crashed)
        {
            // Выруливаем/замираем.
            _speed = Mathf.MoveTowards(_speed, 0f, 6f * dt);
            _alt = 0f;
        }

        // ---------- Публикация состояния (10 Гц) ----------
        _netVarTimer += dt;
        if (_netVarTimer >= 0.1f)
        {
            _netVarTimer = 0f;
            session.SpeedKmh.Value = _speed * 3.6f;
            session.AltitudeM.Value = _alt;
            session.HeadingDeg.Value = _heading;
            session.PitchDeg.Value = _pitch;
            session.RollDeg.Value = _roll;
            session.Throttle.Value = throttle;
        }
    }

    /// <summary>Птица в двигатель: урон и возможный пожар.</summary>
    public void ApplyBirdHit(int engineIndex, float damage)
    {
        var session = GameSession.Instance;
        if (session == null || !NetworkLauncher.IsServer) return;
        if (engineIndex == 0)
        {
            session.Engine1Health.Value = Mathf.Max(0, session.Engine1Health.Value - (int)damage);
            if (session.Engine1Health.Value < 30 && !session.Engine1Fire.Value)
            {
                session.Engine1Fire.Value = true;
                session.HostAnnounce(AnnouncementType.EngineFire);
            }
        }
        else
        {
            session.Engine2Health.Value = Mathf.Max(0, session.Engine2Health.Value - (int)damage);
            if (session.Engine2Health.Value < 30 && !session.Engine2Fire.Value)
            {
                session.Engine2Fire.Value = true;
                session.HostAnnounce(AnnouncementType.EngineFire);
            }
        }
    }

    /// <summary>Птица в лобовое стекло.</summary>
    public void ApplyWindshieldHit()
    {
        var session = GameSession.Instance;
        if (session == null || !NetworkLauncher.IsServer) return;
        session.WindshieldCracks.Value = Mathf.Min(5, session.WindshieldCracks.Value + 1);
    }

    private void Crash(GameSession session)
    {
        session.Phase.Value = (int)GamePhase.Crashed;
        session.AltitudeM.Value = 0f;
        session.HostAnnounce(AnnouncementType.Crash);
        session.HostSfx(transform.position, 3);
    }

    // ================= ДЫМ/ОГОНЬ (визуал) =================

    private void UpdateSmoke(GameSession session)
    {
        _smokeTimer -= Time.deltaTime;
        bool fireL = session.Engine1Fire.Value;
        bool fireR = session.Engine2Fire.Value;
        if (_smokeTimer <= 0f && (fireL || fireR))
        {
            _smokeTimer = 0.16f;
            if (fireL && Refs.SmokeL != null) SpawnPuff(Refs.SmokeL.position, new Color(0.15f, 0.15f, 0.16f, 0.8f));
            if (fireR && Refs.SmokeR != null) SpawnPuff(Refs.SmokeR.position, new Color(0.15f, 0.15f, 0.16f, 0.8f));
            // Языки пламени.
            if (fireL && Refs.SmokeL != null && Random.value < 0.5f)
                SpawnPuff(Refs.SmokeL.position, new Color(1f, 0.45f, 0.1f, 0.9f), small: true);
            if (fireR && Refs.SmokeR != null && Random.value < 0.5f)
                SpawnPuff(Refs.SmokeR.position, new Color(1f, 0.45f, 0.1f, 0.9f), small: true);
        }

        for (int i = _puffs.Count - 1; i >= 0; i--)
        {
            var p = _puffs[i];
            p.life += Time.deltaTime;
            if (p.life > 1.3f)
            {
                Destroy(p.t.gameObject);
                _puffs.RemoveAt(i);
                continue;
            }
            p.t.position += new Vector3(-0.3f, 1.8f, 0.6f) * Time.deltaTime; // дым уносится назад
            float k = 1f - p.life / 1.3f;
            p.t.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.4f, p.life) * (p.t.name.StartsWith("Flame") ? 0.45f : 1f);
            if (p.t.TryGetComponent<MeshRenderer>(out var mr))
            {
                var m = mr.material;
                var c = m.color;
                c.a = k * 0.85f;
                m.color = c;
            }
        }
    }

    private void SpawnPuff(Vector3 pos, Color color, bool small = false)
    {
        var mat = ProceduralAssets.TexMat(small ? "Flame" : "Smoke", ProceduralAssets.CloudTexture, color, billboard: true);
        var go = ProceduralAssets.Quad(transform, small ? "FlamePuff" : "SmokePuff", pos, Vector3.zero,
            new Vector2(1f, 1f), mat);
        go.transform.rotation = Quaternion.LookRotation(Camera.main != null
            ? (Camera.main.transform.position - pos).normalized : Vector3.back);
        _puffs.Add(new SmokePuff { t = go.transform });
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
