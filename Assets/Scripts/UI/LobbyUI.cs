// LobbyManager.cs + LobbyUI.cs — логика лобби: кто хост (панель с кодом и кнопкой старта),
// кто клиент (ожидание), кто просто открыл сцену (предложение стать хостом).

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Менеджер лобби (создаётся в сцене Lobby).</summary>
public class LobbyManager : MonoBehaviour
{
    private LobbyUI _ui;
    private string _lobbyCode;
    private string _hostIp;
    private float _refreshTimer;

    private void Start()
    {
        _ui = gameObject.AddComponent<LobbyUI>();
        ComputeCodes();

        // Если мы уже в сети (кто-то открыл сцену через меню «создать лобби») — стартуем хост.
        if (!NetworkLauncher.InSession)
        {
            // Сцену открыли напрямую: предлагаем стать хостом.
            _ui.ShowDirectOpenPanel(OnBecomeHost);
        }
        else if (NetworkLauncher.IsServer)
        {
            _ui.ShowHostPanel(_lobbyCode, _hostIp, OnStartFlight);
        }
        else
        {
            _ui.ShowClientPanel();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ComputeCodes()
    {
        _hostIp = LobbyCodes.GetLocalIpv4();
        _lobbyCode = LobbyCodes.Encode(_hostIp);
        if (string.IsNullOrEmpty(_lobbyCode))
        {
            // Публичный адрес: кода нет, показываем IP.
            _lobbyCode = null;
        }
    }

    private bool OnBecomeHost()
    {
        if (NetworkLauncher.StartLobbyHost())
        {
            _ui.ShowHostPanel(_lobbyCode, _hostIp, OnStartFlight);
            return true;
        }
        _ui.ShowError(NetworkLauncher.LastError ?? "Не удалось открыть лобби (порт занят?)");
        return false;
    }

    private void OnStartFlight()
    {
        // Старт — только владельцем лобби (хостом). Все клиенты перейдут автоматически.
        if (!NetworkLauncher.IsServer) return;
        ProceduralAudio.PlayUiClick();
        if (NetworkLauncher.NM != null)
        {
            NetworkLauncher.NM.SceneManager.LoadScene(GameBootstrap.SceneAirplane, LoadSceneMode.Single);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Выход из лобби.
        if (kb.escapeKey.wasPressedThisFrame)
        {
            NetworkLauncher.LeaveToMenu();
        }

        // Обновляем список игроков раз в секунду.
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 1f;
            var players = new List<string>();
            foreach (var p in NetworkPlayer.All)
            {
                if (p == null) continue;
                players.Add(p.PlayerName.Value.ToString() + (p.Talking.Value ? " ▮" : ""));
            }
            _ui.SetPlayers(players);

            // Если хост улетел без нас — клиент вернётся в меню через OnClientDisconnected.
        }

        // Табло в 3D-мире.
        var holder = UnityEngine.Object.FindObjectOfType<BoardTextHolder>();
        if (holder != null && holder.textMesh != null)
        {
            string code = _lobbyCode ?? (_hostIp ?? "—");
            holder.textMesh.text = $"РЕЙС DP-18 · КОД ЛОББИ: {code} · ЭКИПАЖ: {NetworkPlayer.All.Count}/4";
        }
    }
}

/// <summary>Панели UI лобби.</summary>
public class LobbyUI : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _panel;
    private UiLabel _codeLabel, _ipLabel, _playersLabel, _titleLabel, _errorLabel;
    private System.Action _startAction;

    public void Build()
    {
        _canvas = UIFactory.CreateCanvas(null, "LobbyCanvas", 60);
        var root = _canvas.GetComponent<RectTransform>();

        _panel = UIFactory.CreateCenterPanel(root, "LobbyPanel", 640f, 520f);

        _titleLabel = UIFactory.CreateLabel(_panel, "Title", "ЛОББИ", 40, TextAnchor.UpperCenter,
            Color.white, FontStyle.Bold);
        var tr = _titleLabel.go.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.anchoredPosition = new Vector2(0f, -26f);
        tr.sizeDelta = new Vector2(0f, 60f);

        _codeLabel = UIFactory.CreateLabel(_panel, "Code", "", 56, TextAnchor.MiddleCenter,
            UIFactory.Accent, FontStyle.Bold);
        var cr = _codeLabel.go.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f);
        cr.anchoredPosition = new Vector2(0f, -130f);
        cr.sizeDelta = new Vector2(0f, 70f);

        _ipLabel = UIFactory.CreateLabel(_panel, "Ip", "", 22, TextAnchor.MiddleCenter, UIFactory.TextDim);
        var ir = _ipLabel.go.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0f, 1f); ir.anchorMax = new Vector2(1f, 1f);
        ir.anchoredPosition = new Vector2(0f, -185f);
        ir.sizeDelta = new Vector2(0f, 34f);

        _playersLabel = UIFactory.CreateLabel(_panel, "Players", "", 26, TextAnchor.UpperLeft, UIFactory.TextMain);
        var pr = _playersLabel.go.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0f, 1f); pr.anchorMax = new Vector2(1f, 1f);
        pr.anchoredPosition = new Vector2(0f, -215f);
        pr.sizeDelta = new Vector2(-60f, 170f);

        _errorLabel = UIFactory.CreateLabel(_panel, "Error", "", 22, TextAnchor.MiddleCenter,
            new Color(1f, 0.45f, 0.35f));
        var er = _errorLabel.go.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(0f, 0f); er.anchorMax = new Vector2(1f, 0f);
        er.anchoredPosition = new Vector2(0f, 120f);
        er.sizeDelta = new Vector2(0f, 34f);
    }

    private void EnsureBuilt()
    {
        if (_canvas == null) Build();
        _panel.gameObject.SetActive(true);
    }

    /// <summary>Панель хоста: код + кнопка старта.</summary>
    public void ShowHostPanel(string code, string ip, System.Action onStart)
    {
        EnsureBuilt();
        _startAction = onStart;
        _titleLabel.SetText("ЛОББИ · ВЫ ХОСТ");
        _codeLabel.SetText(code ?? "IP-РЕЖИМ");
        _ipLabel.SetText(string.IsNullOrEmpty(code)
            ? $"Передайте друзьям адрес: {ip}:{LobbyCodes.DefaultPort} (пробросьте порт 7777)"
            : $"Код для друзей: {code} · или адрес {ip}:{LobbyCodes.DefaultPort}");

        ClearButtons();
        MakeButton("Start", "ВЫЛЕТАЕМ! (перевозит всех в самолёт)", new Vector2(0f, -215f), onStart);
        MakeButton("Back", "В МЕНЮ (ESC)", new Vector2(0f, -285f), () => NetworkLauncher.LeaveToMenu());
    }

    /// <summary>Панель клиента: ждём капитана.</summary>
    public void ShowClientPanel()
    {
        EnsureBuilt();
        _titleLabel.SetText("ЛОББИ · ОЖИДАНИЕ КАПИТАНА");
        _codeLabel.SetText("…");
        _ipLabel.SetText("Хост запустит рейс, когда все соберутся. Голосовой чат: V.");
        ClearButtons();
        MakeButton("Back", "В МЕНЮ (ESC)", new Vector2(0f, -240f), () => NetworkLauncher.LeaveToMenu());
    }

    /// <summary>Сцену открыли напрямую — предложить стать хостом.</summary>
    public void ShowDirectOpenPanel(System.Func<bool> onBecomeHost)
    {
        EnsureBuilt();
        _titleLabel.SetText("ЛОББИ");
        _codeLabel.SetText("СОЗДАТЬ РЕЙС?");
        _ipLabel.SetText("Станьте хостом и позовите друзей по коду — или вернитесь в меню.");
        ClearButtons();
        MakeButton("Host", "СТАТЬ ХОСТОМ", new Vector2(0f, -215f), () => onBecomeHost());
        MakeButton("Back", "В МЕНЮ", new Vector2(0f, -285f), () => NetworkLauncher.LeaveToMenu());
    }

    public void SetPlayers(List<string> players)
    {
        if (_playersLabel == null) return;
        string text = "ЭКИПАЖ:\n";
        foreach (var p in players) text += "• " + p + "\n";
        _playersLabel.SetText(text);
    }

    public void ShowError(string text)
    {
        if (_errorLabel != null) _errorLabel.SetText(text);
    }

    // ---- кнопки (пересоздаются при смене панели) ----
    private readonly List<UnityEngine.UI.Button> _buttons = new List<UnityEngine.UI.Button>();

    private void ClearButtons()
    {
        foreach (var b in _buttons) if (b != null) Destroy(b.gameObject);
        _buttons.Clear();
    }

    private void MakeButton(string name, string text, Vector2 pos, System.Action onClick)
    {
        _buttons.Add(UIFactory.CreateButton(_panel, name, text, pos, new Vector2(430f, 58f), 22, () =>
        {
            ProceduralAudio.PlayUiClick();
            onClick();
        }));
    }
}
