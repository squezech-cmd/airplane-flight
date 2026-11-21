// HUD.cs — игровой интерфейс: полётные данные, топливо, двигатели, настроение салона,
// предупреждения, субтитры капитана, список экипажа с индикаторами голоса, подсказки
// интерактов, полоска удержания E, трещины лобового стекла, мини-игра заправки, пауза, результаты.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Игровой HUD (создаётся в сценах Airplane/Flight).</summary>
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; set; }

    private Canvas _canvas;
    private UiLabel _speedLabel, _altLabel, _headingLabel, _distLabel;
    private UiLabel _phaseLabel, _warningLabel;
    private UiLabel _promptLabel, _subtitleLabel, _holdLabel;
    private UiLabel _crewLabel, _moodLabel, _cargoLabel;
    private Image _fuelFill, _moodFill, _holdFill, _crackImage;
    private Image _engineL, _engineR;
    private Image _refuelMarker;
    private RectTransform _refuelRing;
    private Image _refuelProgressFill;

    private RectTransform _pausePanel, _resultsPanel;
    private UiLabel _resultsLabel, _resultsStats;
    private float _subtitleUntil, _warningUntil;

    /// <summary>Пауза открыта?</summary>
    public bool IsPaused { get; private set; }

    private string _promptCache;
    private int _lastPhase = -1;

    private void Start()
    {
        Instance = this;
        _canvas = UIFactory.CreateCanvas(null, "HudCanvas", 80);
        var root = _canvas.GetComponent<RectTransform>();
        BuildFlightInfo(root);
        BuildMood(root);
        BuildFuel(root);
        BuildCenter(root);
        BuildCrew(root);
        BuildOverlays(root);
        BuildPause(root);
    }

    // ================= ПОСТРОЕНИЕ =================

    private void BuildFlightInfo(Transform root)
    {
        var panel = UIFactory.CreatePanel(root, "FlightInfo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -190f), new Vector2(300f, -16f), UIFactory.PanelColor);

        _phaseLabel = UIFactory.CreateLabel(panel, "Phase", "ПОДГОТОВКА К РЕЙСУ", 24, TextAnchor.UpperLeft,
            UIFactory.Accent, FontStyle.Bold);
        _speedLabel = UIFactory.CreateLabel(panel, "Speed", "Скорость: 0 км/ч", 22, TextAnchor.UpperLeft, UIFactory.TextMain);
        _altLabel = UIFactory.CreateLabel(panel, "Alt", "Высота: 0 м", 22, TextAnchor.UpperLeft, UIFactory.TextMain);
        _headingLabel = UIFactory.CreateLabel(panel, "Hdg", "Курс: 0°", 22, TextAnchor.UpperLeft, UIFactory.TextMain);
        _distLabel = UIFactory.CreateLabel(panel, "Dist", "До аэропорта: -- км", 22, TextAnchor.UpperLeft, UIFactory.TextMain);
        _cargoLabel = UIFactory.CreateLabel(panel, "Cargo", "Груз: —", 20, TextAnchor.UpperLeft, UIFactory.TextDim);

        LayoutLabels(panel, 8f, -6f, 34f, _phaseLabel, _speedLabel, _altLabel, _headingLabel, _distLabel, _cargoLabel);
    }

    private static void LayoutLabels(RectTransform panel, float x, float top, float step, params UiLabel[] labels)
    {
        float y = top;
        foreach (var l in labels)
        {
            var r = l.go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(-2f * x, step + 4f);
            y -= step;
        }
    }

    private void BuildMood(Transform root)
    {
        var panel = UIFactory.CreatePanel(root, "MoodPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-260f, -70f), new Vector2(260f, -16f), UIFactory.PanelColor);
        _moodLabel = UIFactory.CreateLabel(panel, "Label", "НАСТРОЕНИЕ САЛОНА", 18, TextAnchor.UpperLeft,
            UIFactory.TextDim);
        var lr = _moodLabel.go.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 1f); lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = new Vector2(10f, 28f); lr.offsetMax = new Vector2(-10f, -6f);

        var (fill, _) = UIFactory.CreateBar(panel, "MoodBar", new Vector2(0f, 4f), new Vector2(-20f, 18f),
            new Color(0.3f, 0.85f, 0.4f));
        var fr = fill.rectTransform; // Bar создаёт fill на весь размер
        _moodFill = fill;
    }

    private void BuildFuel(Transform root)
    {
        var panel = UIFactory.CreatePanel(root, "FuelPanel", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-300f, 16f), new Vector2(-16f, 150f), UIFactory.PanelColor);

        var fuelLabel = UIFactory.CreateLabel(panel, "Label", "ТОПЛИВО", 18, TextAnchor.UpperLeft, UIFactory.TextDim);
        var flr = fuelLabel.go.GetComponent<RectTransform>();
        flr.anchorMin = new Vector2(0f, 1f); flr.anchorMax = new Vector2(1f, 1f);
        flr.offsetMin = new Vector2(10f, 28f); flr.offsetMax = new Vector2(-10f, -6f);

        var (fill, rect) = UIFactory.CreateBar(panel, "FuelBar", new Vector2(0f, 4f), new Vector2(-20f, 18f),
            new Color(0.95f, 0.7f, 0.2f));
        _fuelFill = fill;
        // Bar центрируется — переставим в левый нижний угол панели.
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(0f, 22f);
        rect.sizeDelta = new Vector2(-20f, 18f);
        rect.pivot = new Vector2(0.5f, 0f);

        // Двигатели: два индикатора.
        _engineL = UIFactory.CreateImage(panel, "EngineL", null, new Vector2(-55f, 52f), new Vector2(60f, 26f),
            new Color(0.3f, 0.85f, 0.4f));
        _engineR = UIFactory.CreateImage(panel, "EngineR", null, new Vector2(55f, 52f), new Vector2(60f, 26f),
            new Color(0.3f, 0.85f, 0.4f));
    }

    private void BuildCenter(Transform root)
    {
        // Прицел.
        UIFactory.CreateImage(root, "Crosshair", ProceduralAssets.MakeSprite(ProceduralAssets.CircleTexture),
            Vector2.zero, new Vector2(10f, 10f), new Color(1f, 1f, 1f, 0.6f));

        // Предупреждение (центр-верх).
        _warningLabel = UIFactory.CreateLabel(root, "Warning", "", 34, TextAnchor.MiddleCenter,
            new Color(1f, 0.3f, 0.2f), FontStyle.Bold);
        var wr = _warningLabel.go.GetComponent<RectTransform>();
        wr.anchorMin = new Vector2(0.5f, 1f); wr.anchorMax = new Vector2(0.5f, 1f);
        wr.anchoredPosition = new Vector2(0f, -110f);
        wr.sizeDelta = new Vector2(900f, 46f);

        // Подсказка интеракта (центр-низ).
        _promptLabel = UIFactory.CreateLabel(root, "Prompt", "", 24, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        var pr = _promptLabel.go.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = new Vector2(0f, 170f);
        pr.sizeDelta = new Vector2(1000f, 40f);

        // Полоска удержания E.
        _holdLabel = UIFactory.CreateLabel(root, "HoldLabel", "", 20, TextAnchor.MiddleCenter, UIFactory.TextDim);
        var hr = _holdLabel.go.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0.5f, 0.5f); hr.anchorMax = new Vector2(0.5f, 0.5f);
        hr.anchoredPosition = new Vector2(0f, 130f);
        hr.sizeDelta = new Vector2(600f, 26f);

        // Субтитры капитана.
        _subtitleLabel = UIFactory.CreateLabel(root, "Subtitle", "", 26, TextAnchor.LowerCenter,
            new Color(1f, 0.95f, 0.8f));
        var sr = _subtitleLabel.go.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.5f, 0f); sr.anchorMax = new Vector2(0.5f, 0f);
        sr.anchoredPosition = new Vector2(0f, 110f);
        sr.sizeDelta = new Vector2(1200f, 60f);
    }

    private void BuildCrew(Transform root)
    {
        var panel = UIFactory.CreatePanel(root, "CrewPanel", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-280f, -200f), new Vector2(-16f, -16f), UIFactory.PanelColor);
        _crewLabel = UIFactory.CreateLabel(panel, "Crew", "ЭКИПАЖ", 20, TextAnchor.UpperLeft,
            UIFactory.TextDim);
        var cr = _crewLabel.go.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
        cr.offsetMin = new Vector2(10f, 8f); cr.offsetMax = new Vector2(-10f, -6f);
    }

    private void BuildOverlays(Transform root)
    {
        // Трещины лобового стекла.
        _crackImage = UIFactory.CreateImage(root, "Cracks", ProceduralAssets.MakeSprite(ProceduralAssets.CrackTexture),
            Vector2.zero, new Vector2(2200f, 1300f), new Color(1f, 1f, 1f, 0f));

        // Кольцо + маркер мини-игры заправки.
        _refuelRing = new GameObject("RefuelRing", typeof(RectTransform)).GetComponent<RectTransform>();
        _refuelRing.SetParent(root, false);
        _refuelRing.anchorMin = _refuelRing.anchorMax = new Vector2(0.5f, 0.5f);
        _refuelRing.anchoredPosition = new Vector2(0f, 150f);
        _refuelRing.sizeDelta = new Vector2(420f, 420f);
        _refuelRing.gameObject.SetActive(false);

        UIFactory.CreateImage(_refuelRing, "Ring", ProceduralAssets.MakeSprite(ProceduralAssets.CircleTexture),
            Vector2.zero, new Vector2(190f, 190f), new Color(1f, 0.85f, 0.3f, 0.9f));
        _refuelMarker = UIFactory.CreateImage(_refuelRing, "Marker", ProceduralAssets.MakeSprite(ProceduralAssets.CircleTexture),
            Vector2.zero, new Vector2(46f, 46f), new Color(0.3f, 1f, 0.5f));
        var prog = UIFactory.CreateBar(_refuelRing, "Progress", new Vector2(0f, -250f), new Vector2(400f, 26f),
            new Color(0.3f, 0.9f, 1f));
        _refuelProgressFill = prog.fill;

        // Результаты.
        _resultsPanel = UIFactory.CreateCenterPanel(root, "Results", 900f, 620f);
        _resultsLabel = UIFactory.CreateLabel(_resultsPanel, "Title", "", 54, TextAnchor.UpperCenter,
            Color.white, FontStyle.Bold);
        var rl = _resultsLabel.go.GetComponent<RectTransform>();
        rl.anchorMin = new Vector2(0f, 1f); rl.anchorMax = new Vector2(1f, 1f);
        rl.anchoredPosition = new Vector2(0f, -50f);
        rl.sizeDelta = new Vector2(0f, 70f);
        _resultsStats = UIFactory.CreateLabel(_resultsPanel, "Stats", "", 26, TextAnchor.UpperCenter, UIFactory.TextMain);
        var rs = _resultsStats.go.GetComponent<RectTransform>();
        rs.anchorMin = new Vector2(0f, 1f); rs.anchorMax = new Vector2(1f, 1f);
        rs.anchoredPosition = new Vector2(0f, -140f);
        rs.sizeDelta = new Vector2(-80f, 320f);
        UIFactory.CreateButton(_resultsPanel, "ToMenu", "В ГЛАВНОЕ МЕНЮ", new Vector2(0f, -250f),
            new Vector2(420f, 62f), 26, () => NetworkLauncher.LeaveToMenu());
        _resultsPanel.gameObject.SetActive(false);
    }

    private void BuildPause(Transform root)
    {
        _pausePanel = UIFactory.CreateCenterPanel(root, "Pause", 560f, 520f);
        var title = UIFactory.CreateLabel(_pausePanel, "Title", "ПАУЗА", 48, TextAnchor.UpperCenter,
            Color.white, FontStyle.Bold);
        var tr = title.go.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.anchoredPosition = new Vector2(0f, -40f);
        tr.sizeDelta = new Vector2(0f, 66f);

        UIFactory.CreateButton(_pausePanel, "Resume", "ПРОДОЛЖИТЬ (ESC)", new Vector2(0f, 90f),
            new Vector2(380f, 60f), 24, () => TogglePause());
        UIFactory.CreateButton(_pausePanel, "Settings", "НАСТРОЙКИ", new Vector2(0f, 16f),
            new Vector2(380f, 60f), 24, () => SettingsMenu.Open(_pausePanel, null));
        UIFactory.CreateButton(_pausePanel, "Leave", "ПОКИНУТЬ РЕЙС", new Vector2(0f, -58f),
            new Vector2(380f, 60f), 24, () => NetworkLauncher.LeaveToMenu(), new Color(0.5f, 0.2f, 0.15f));

        var hint = UIFactory.CreateLabel(_pausePanel, "Hint",
            "E — взаимодействие · F — вода/шланг · ЛКМ — удар/выброс · V — голос\n" +
            "Штурвал: мышь, W/S — тяга, A/D — руль, T — выровнять, R — отпустить",
            20, TextAnchor.LowerCenter, UIFactory.TextDim);
        var h = hint.go.GetComponent<RectTransform>();
        h.anchorMin = new Vector2(0f, 0f); h.anchorMax = new Vector2(1f, 0f);
        h.anchoredPosition = new Vector2(0f, 14f);
        h.sizeDelta = new Vector2(-30f, 70f);

        _pausePanel.gameObject.SetActive(false);
    }

    // ================= ПУБЛИЧНОЕ API =================

    /// <summary>Подсказка интеракта (каждый кадр).</summary>
    public void SetPrompt(string text)
    {
        if (text == _promptCache) return;
        _promptCache = text;
        _promptLabel.SetText(text ?? "");
    }

    /// <summary>Полоска удержания E (0..1, 0 — скрыть).</summary>
    public void SetHoldProgress(float k)
    {
        if (k <= 0.01f) _holdLabel.SetText("");
        else _holdLabel.SetText($"Схватить… {Mathf.FloorToInt(k * 100)}%");
    }

    /// <summary>Субтитры капитана.</summary>
    public void Subtitle(string text, float seconds)
    {
        _subtitleLabel.SetText(text);
        _subtitleUntil = Time.unscaledTime + seconds;
    }

    /// <summary>Мигающее предупреждение.</summary>
    public void Warning(string text, float seconds)
    {
        _warningLabel.SetText(text);
        _warningUntil = Time.unscaledTime + seconds;
    }

    /// <summary>Мини-игра заправки: маркер (-1..1), в кольце?, прогресс (0..1, <0 скрыть).</summary>
    public void SetRefuelMarker(float mx, float my, bool inRing, float progress)
    {
        _refuelRing.gameObject.SetActive(progress >= 0f);
        if (progress < 0f) return;
        var r = _refuelMarker.rectTransform;
        r.anchoredPosition = new Vector2(mx * 165f, my * 165f);
        _refuelMarker.color = inRing ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.35f, 0.3f);
        // Прогресс: слева направо.
        var f = _refuelProgressFill.rectTransform;
        f.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        f.anchorMin = new Vector2(0f, 0f);
        f.offsetMin = new Vector2(2f, 2f);
        f.offsetMax = new Vector2(-2f, -2f);
    }

    /// <summary>Пауза. Возвращает новое состояние.</summary>
    public bool TogglePause()
    {
        IsPaused = !IsPaused;
        if (_pausePanel != null) _pausePanel.gameObject.SetActive(IsPaused);
        Time.timeScale = IsPaused ? 0f : 1f;
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
        return IsPaused;
    }

    // ================= ОБНОВЛЕНИЕ =================

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null) return;

        // Полётные данные.
        _speedLabel.SetText($"Скорость: {Mathf.RoundToInt(session.SpeedKmh.Value)} км/ч");
        _altLabel.SetText($"Высота: {Mathf.RoundToInt(session.AltitudeM.Value)} м");
        _headingLabel.SetText($"Курс: {Mathf.RoundToInt(session.HeadingDeg.Value)}°");
        _distLabel.SetText($"До аэропорта: {session.DistanceToDestM.Value / 1000f:F1} км");
        _cargoLabel.SetText($"Груз: {session.CargoName}");

        // Фазы.
        int phase = session.Phase.Value;
        if (phase != _lastPhase)
        {
            _lastPhase = phase;
            _phaseLabel.SetText(PhaseName(phase));
            if (phase == (int)GamePhase.Landed || phase == (int)GamePhase.Crashed)
            {
                ShowResults(session, phase == (int)GamePhase.Landed);
            }
            if (phase == (int)GamePhase.Takeoff) Subtitle("Взлётаем! Пристегните… всё, что можно пристегнуть.", 5f);
        }

        // Топливо.
        float fuelK = session.Fuel.Value / GameSession.FuelCapacity;
        _fuelFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fuelK), 1f);
        _fuelFill.color = fuelK < 0.25f ? new Color(1f, 0.25f, 0.15f) : new Color(0.95f, 0.7f, 0.2f);

        // Двигатели.
        _engineL.color = session.Engine1Fire.Value ? new Color(1f, 0.2f, 0.1f)
            : Color.Lerp(new Color(0.9f, 0.5f, 0.2f), new Color(0.3f, 0.85f, 0.4f), session.Engine1Health.Value / 100f);
        _engineR.color = session.Engine2Fire.Value ? new Color(1f, 0.2f, 0.1f)
            : Color.Lerp(new Color(0.9f, 0.5f, 0.2f), new Color(0.3f, 0.85f, 0.4f), session.Engine2Health.Value / 100f);

        // Настроение.
        float mood = session.Mood.Value / 100f;
        _moodFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(mood), 1f);
        _moodFill.color = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.3f, 0.85f, 0.4f), mood);
        _moodLabel.SetText($"НАСТРОЕНИЕ САЛОНА: {Mathf.RoundToInt(session.Mood.Value)}%");

        // Экипаж.
        string crew = "ЭКИПАЖ\n";
        foreach (var p in NetworkPlayer.All)
        {
            if (p == null) continue;
            string voice = p.Talking.Value ? " ▮ГОВОРИТ" : "";
            string pilot = session.PilotClientId.Value == p.OwnerClientId ? " [ПИЛОТ]" : "";
            crew += $"• {p.PlayerName.Value}{pilot}{voice}\n";
        }
        _crewLabel.SetText(crew);

        // Предупреждения.
        bool stall = session.Phase.Value == (int)GamePhase.Flight && session.SpeedKmh.Value < 145f;
        bool fire = session.Engine1Fire.Value || session.Engine2Fire.Value;
        bool lowFuel = session.Fuel.Value < GameSession.FuelCapacity * 0.25f;
        if (stall) Warning("СВАЛИВАНИЕ! НАБЕРИТЕ СКОРОСТЬ!", 0.4f);
        else if (fire) Warning("ПОЖАР ДВИГАТЕЛЯ!", 0.4f);
        else if (lowFuel && session.RefuelStage.Value < 1) Warning("ТОПЛИВО НА ИСХОДЕ!", 0.4f);

        if (Time.unscaledTime > _warningUntil)
        {
            _warningLabel.SetText("");
        }

        // Субтитры гаснут.
        if (Time.unscaledTime > _subtitleUntil && _subtitleLabel != null)
        {
            _subtitleLabel.SetText("");
        }

        // Трещины.
        int cracks = session.WindshieldCracks.Value;
        _crackImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(cracks * 0.22f));
    }

    private static string PhaseName(int phase) => phase switch
    {
        (int)GamePhase.Boarding => "ПОСАДКА ПАССАЖИРОВ (Y — ВЗЛЁТ, хост)",
        (int)GamePhase.Takeoff => "ВЗЛЁТ",
        (int)GamePhase.Flight => "ПОЛЁТ",
        (int)GamePhase.Landing => "ЗАХОД НА ПОСАДКУ",
        (int)GamePhase.Landed => "ПОСАДКА ВЫПОЛНЕНА",
        (int)GamePhase.Crashed => "КРУШЕНИЕ",
        _ => "…",
    };

    /// <summary>Экран результатов.</summary>
    private void ShowResults(GameSession session, bool success)
    {
        if (_resultsPanel == null) return;
        _resultsPanel.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var cargo = GameSession.CargoOptions[Mathf.Clamp(session.CargoId.Value, 0, 2)];
        int delivered = session.StatPaxCalm.Value;
        int ejected = session.StatEjected.Value;
        int payout = success
            ? cargo.pay + delivered * 200 - ejected * 150 + (session.Fuel.Value > 100f ? 300 : 0)
            : 0;

        _resultsLabel.SetText(success ? "РЕЙС ВЫПОЛНЕН!" : "КРУШЕНИЕ…");
        _resultsLabel.SetColor(success ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.35f, 0.25f));
        _resultsStats.SetText(
            $"Пассажиров доставлено спокойно: {delivered}\n" +
            $"Пассажиров выброшено за борт: {ejected}\n" +
            $"Пассажиров в панике при встрече с землёй: {session.StatPaxPanic.Value}\n" +
            $"Птиц съедено двигателями: {session.StatBirdsEaten.Value}\n" +
            $"Литров заправлено в воздухе: {session.StatRefueledLiters.Value}\n" +
            $"Груз: {cargo.name}\n\n" +
            (success
                ? $"Оплата рейса: {payout} у.е.\nЭкипаж справился. Почти."
                : "Оплата: 0 у.е.\nБилетов больше не продавайте."));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}
