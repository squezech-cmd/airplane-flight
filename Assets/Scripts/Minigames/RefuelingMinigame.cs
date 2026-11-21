// RefuelingMinigame.cs — заправка в полёте: событие «летит заправщик», выход на крыло,
// шланг на законцовке, мини-игра удержания стыковки (WASD против ветра), поток топлива.
// Состояние синхронизировано через GameSession.RefuelStage/RefuelProgress; ввод выравнивания
// игрок отправляет ServerRpc'ом, сервер перекачивает топливо.

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Мини-игра заправки в воздухе.</summary>
public class RefuelingMinigame : MonoBehaviour
{
    public static RefuelingMinigame Instance { get; set; }

    // ---------- Серверное ----------
    private float _stageTimer;
    private float _flowAccum;         // накопленный «поток» за такт выравнивания
    private bool _inRing;             // текущее состояние выравнивания от игрока

    // ---------- Локальное (у каждого клиента) ----------
    private Transform _tanker;        // самолёт-заправщик
    private Transform _coupling;      // муфта-цель над крылом
    private HoseInteractable _hose;   // интерактивный шланг
    private float _markerX, _markerY; // маркер мини-игры (-1..1)
    private bool _engaged;            // игрок подключился к муфте

    private Material _ringMat;

    private void Awake()
    {
        Instance = this;
        _ringMat = ProceduralAssets.TexMat("CouplingRing", ProceduralAssets.CircleTexture, Color.white, billboard: true);
    }

    private void Start()
    {
        BuildTanker();
        BuildHose();
    }

    /// <summary>Визуал заправщика (большая «бочка» с крыльями над нами).</summary>
    private void BuildTanker()
    {
        var root = new GameObject("Tanker");
        _tanker = root.transform;
        _tanker.SetParent(transform, false);
        _tanker.position = new Vector3(0f, 40f, -400f); // далеко, прилетит

        var bodyMat = ProceduralAssets.Mat("TankerBody", new Color(0.85f, 0.85f, 0.4f));
        ProceduralAssets.Cylinder(_tanker, "Body", Vector3.zero, new Vector3(90f, 0f, 0f), 1.6f, 14f, bodyMat);
        ProceduralAssets.Box(_tanker, "WingL", new Vector3(-6f, 0f, 0f), Vector3.zero, new Vector3(10f, 0.25f, 2.6f), bodyMat);
        ProceduralAssets.Box(_tanker, "WingR", new Vector3(6f, 0f, 0f), Vector3.zero, new Vector3(10f, 0.25f, 2.6f), bodyMat);
        ProceduralAssets.Box(_tanker, "Tail", new Vector3(0f, 1.6f, -6.6f), Vector3.zero, new Vector3(0.2f, 3f, 2f), bodyMat);
        // Шланг заправщика вниз к нам.
        var hoseMat = ProceduralAssets.Mat("HoseMat", new Color(0.15f, 0.15f, 0.16f));
        ProceduralAssets.Cylinder(_tanker, "Hose", new Vector3(0f, -3.5f, 0f), Vector3.zero, 0.09f, 7f, hoseMat);
        ProceduralAssets.Sphere(_tanker, "Nozzle", new Vector3(0f, -7f, 0f), 0.28f, hoseMat);

        _tanker.gameObject.SetActive(false);
    }

    /// <summary>Муфта-цель над крылом + шланг на крыле.</summary>
    private void BuildHose()
    {
        // Муфта парит над правым крылом.
        var coupling = new GameObject("Coupling");
        _coupling = coupling.transform;
        _coupling.SetParent(transform, false);
        _coupling.position = new Vector3(5.6f, 3.2f, 0.7f);
        var ring = ProceduralAssets.Quad(_coupling, "Ring", Vector3.zero, Vector3.zero,
            new Vector2(1.6f, 1.6f), _ringMat);
        ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _coupling.gameObject.SetActive(false);

        // Шланг (интерактив) на крыле.
        var hoseGo = new GameObject("RefuelHose");
        hoseGo.transform.SetParent(transform, false);
        var refs = AirplaneController.Instance != null ? AirplaneController.Instance.Refs : null;
        hoseGo.transform.position = refs != null && refs.HoseAnchor != null
            ? refs.HoseAnchor.position
            : new Vector3(5.8f, 0.5f, 0.7f);
        _hose = hoseGo.AddComponent<HoseInteractable>();
        _hose.BuildVisual();
        _hose.owner = this;
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null) return;

        // ---------- Серверная логика ----------
        if (NetworkLauncher.IsServer)
        {
            ServerUpdate(session);
        }

        // ---------- Общий визуал ----------
        int stage = session.RefuelStage.Value;
        _tanker.gameObject.SetActive(stage >= 1);
        _coupling.gameObject.SetActive(stage == 2);
        _hose.gameObject.SetActive(stage <= 2);

        if (stage == 1)
        {
            // Заправщик подлетает и встаёт над нами.
            Vector3 target = new Vector3(0f, 9.5f, -2f);
            _tanker.position = Vector3.MoveTowards(_tanker.position, target, 42f * Time.deltaTime);
        }
        else if (stage >= 2)
        {
            _tanker.position = new Vector3(0f, 9.5f, -2f) + new Vector3(
                Mathf.PerlinNoise(Time.time * 0.3f, 8f) - 0.5f, 0f, 0f) * 2f;
        }

        // Муфта «гуляет» (её и надо удерживать).
        if (_coupling.gameObject.activeSelf)
        {
            float turb = session.Turbulence.Value;
            // Пилот держит штурвал ровно — муфту меньше трясёт.
            float pilotSteady = 1f - Mathf.Clamp01(Mathf.Abs(session.PilotRoll.Value) + Mathf.Abs(session.PilotPitch.Value)) * 0.7f;
            float drift = (0.35f + turb) * pilotSteady + 0.12f;
            _coupling.position = new Vector3(5.6f, 3.2f, 0.7f) + new Vector3(
                (Mathf.PerlinNoise(Time.time * 0.9f, 3f) - 0.5f) * 3.2f * drift,
                0f,
                (Mathf.PerlinNoise(Time.time * 0.9f, 7f) - 0.5f) * 2.0f * drift);
        }

        // ---------- Мини-игра у игрока с шлангом ----------
        if (_engaged && stage == 2) MiniGameUpdate(session);
    }

    // ================= СЕРВЕР =================

    private void ServerUpdate(GameSession session)
    {
        // Стадия 0: ждём низкого топлива в полёте.
        if (session.RefuelStage.Value == 0)
        {
            if (session.Phase.Value == (int)GamePhase.Flight &&
                session.Fuel.Value < GameSession.FuelCapacity * 0.25f)
            {
                session.RefuelStage.Value = 1;
                session.RefuelProgress.Value = 0f;
                session.HostAnnounce(AnnouncementType.RefuelStart);
            }
            return;
        }

        // Стадия 1: заправщик на позиции → можно подключаться.
        if (session.RefuelStage.Value == 1)
        {
            _stageTimer += Time.deltaTime;
            if (_stageTimer > 10f)
            {
                _stageTimer = 0f;
                session.RefuelStage.Value = 2;
            }
            return;
        }

        // Стадия 2: идёт заправка, пока игрок удерживает стыковку.
        if (session.RefuelStage.Value == 2)
        {
            _stageTimer += Time.deltaTime;
            if (_inRing)
            {
                const float litersPerSecond = 22f;
                float add = litersPerSecond * Time.deltaTime;
                session.Fuel.Value = Mathf.Min(GameSession.FuelCapacity, session.Fuel.Value + add);
                session.StatRefueledLiters.Value += (int)add;
                session.RefuelProgress.Value = session.Fuel.Value / GameSession.FuelCapacity;

                if (session.Fuel.Value >= GameSession.FuelCapacity * 0.999f)
                {
                    session.RefuelStage.Value = 3;
                    session.HostAnnounce(AnnouncementType.RefuelDone);
                }
            }
            // Таймаут попытки — перезапуск стыковки.
            if (_stageTimer > 90f && session.Fuel.Value < GameSession.FuelCapacity)
            {
                _stageTimer = 0f;
                session.RefuelProgress.Value = 0f;
            }
        }
    }

    /// <summary>Сервер получил состояние выравнивания (из GameSession.SetRefuelAlignmentServerRpc).</summary>
    public void ServerSetAlignment(bool inRing)
    {
        _inRing = inRing;
    }

    /// <summary>Отправить своё состояние выравнивания на сервер.</summary>
    private void ReportAlignment(bool inRing)
    {
        if (GameSession.Instance != null) GameSession.Instance.SetRefuelAlignmentServerRpc(inRing);
    }

    // ================= МИНИ-ИГРА =================

    private void MiniGameUpdate(GameSession session)
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Ветер/турбулентность сносит маркер.
        float turb = session.Turbulence.Value;
        float pilotSteady = 1f - Mathf.Clamp01(Mathf.Abs(session.PilotRoll.Value) + Mathf.Abs(session.PilotPitch.Value)) * 0.7f;
        float drift = (0.5f + turb * 1.4f) * pilotSteady + 0.15f;
        _markerX += (Mathf.PerlinNoise(Time.time * 1.1f, 2f) - 0.5f) * drift * Time.deltaTime * 6f;
        _markerY += (Mathf.PerlinNoise(Time.time * 1.1f, 6f) - 0.5f) * drift * Time.deltaTime * 6f;

        // WASD компенсирует.
        float k = 1.6f * Time.deltaTime;
        if (kb.aKey.isPressed) _markerX -= k;
        if (kb.dKey.isPressed) _markerX += k;
        if (kb.wKey.isPressed) _markerY += k;
        if (kb.sKey.isPressed) _markerY -= k;
        _markerX = Mathf.Clamp(_markerX, -1f, 1f);
        _markerY = Mathf.Clamp(_markerY, -1f, 1f);

        // Выравнивание: маркер в кольце?
        float dist = Mathf.Sqrt(_markerX * _markerX + _markerY * _markerY);
        bool inRing = dist < 0.38f;
        ReportAlignment(inRing);

        if (HUD.Instance != null)
        {
            HUD.Instance.SetRefuelMarker(_markerX, _markerY, inRing, session.RefuelProgress.Value);
        }

        // F — отключиться.
        if (kb.fKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
        {
            Disengage();
        }
    }

    /// <summary>Игрок подключил шланг к муфте (вызывается HoseInteractable при F у муфты).</summary>
    public void Engage(PlayerController player)
    {
        _engaged = true;
        _markerX = 0f;
        _markerY = 0f;
        if (HUD.Instance != null) HUD.Instance.Subtitle("Держите маркер в кольце! WASD — выравнивание, F — отключиться.", 4f);
    }

    public void Disengage()
    {
        _engaged = false;
        ReportAlignment(false);
        if (HUD.Instance != null) HUD.Instance.SetRefuelMarker(0f, 0f, false, -1f);
    }

    public bool IsEngaged => _engaged;
    public Transform Coupling => _coupling;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

/// <summary>Шланг заправки: интерактив на крыле.</summary>
public class HoseInteractable : Interactable
{
    public RefuelingMinigame owner;
    private bool _carriedLocal;

    public void BuildVisual()
    {
        var hoseMat = ProceduralAssets.Mat("HoseMat", new Color(0.15f, 0.15f, 0.16f));
        ProceduralAssets.Cylinder(transform, "Coil", Vector3.zero, new Vector3(90f, 0f, 0f), 0.16f, 0.5f, hoseMat);
        ProceduralAssets.Cylinder(transform, "Stub", new Vector3(0f, 0.05f, 0.3f), new Vector3(60f, 0f, 0f), 0.05f, 0.7f, hoseMat);
    }

    public override string GetPrompt(PlayerController player)
    {
        var session = GameSession.Instance;
        if (session == null) return null;
        if (session.RefuelStage.Value == 2 && !_carriedLocal)
            return "Взять заправочный шланг (E)";
        if (_carriedLocal && session.RefuelStage.Value == 2)
            return owner != null && owner.Coupling != null
                ? "Подключить к муфте: подойдите и нажмите F"
                : "Нести шланг к муфте";
        if (session.RefuelStage.Value == 1) return "Заправщик ещё заходит…";
        return null;
    }

    public override void Interact(PlayerController player)
    {
        _carriedLocal = true; // локально «в руках» (упрощение: шланг один на рейс)
        if (HUD.Instance != null) HUD.Instance.Subtitle("Шланг в руках. Идите к светящемуся кольцу над крылом.", 3.5f);
    }

    public override void SecondaryInteract(PlayerController player)
    {
        // F рядом с муфтой — подключиться.
        var session = GameSession.Instance;
        if (owner == null || session == null || session.RefuelStage.Value != 2 || !_carriedLocal) return;
        if (owner.Coupling != null &&
            Vector3.Distance(player.transform.position, owner.Coupling.position) < 3.5f)
        {
            owner.Engage(player);
        }
        else if (HUD.Instance != null)
        {
            HUD.Instance.Subtitle("Муфта слишком далеко — подойдите к кольцу над крылом.", 2.5f);
        }
    }
}
