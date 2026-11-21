// MainMenu.cs — главное меню: гейт 18+, фон, кнопки (соло / кооператив / подключение по коду /
// настройки / выход), ввод 6-символьного кода лобби без InputField (прямой опрос клавиатуры).

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Контроллер главного меню.</summary>
public class MainMenu : MonoBehaviour
{
    public const string Disclaimer = "18+ | Создана в развлекательных целях. Не повторяйте в реальной жизни. Все совпадения случайны.";

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _gatePanel;
    private RectTransform _mainPanel;
    private RectTransform _joinPanel;
    private RectTransform _connectingPanel;
    private UiLabel _joinCodeLabel;
    private UiLabel _errorLabel;
    private UiLabel _connectLabel;
    private string _codeInput = "";

    private System.Random _jokeRng = new System.Random(2026);
    private static readonly string[] MenuJokes =
    {
        "«Худшая авиакомпания мира» ищет экипаж. Вы приняты.",
        "Держите штурвал крепче. Пассажиры — слабже.",
        "Наш девиз: «Долетим — значит долетели».",
        "Топливо? Это буквы на табло. Буквы кончаются.",
        "Птицы — наши друзья. Двигатели — нет.",
    };

    private void Start()
    {
        _canvas = UIFactory.CreateCanvas(null, "MainMenuCanvas", 50);
        _root = _canvas.GetComponent<RectTransform>();

        BuildBackground();
        BuildGate();
        BuildMain();
        BuildJoin();
        BuildConnecting();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void BuildBackground()
    {
        // Фоновая картинка из Resources (сгенерированный арт), поверх — затемнение.
        var bgTex = Resources.Load<Texture2D>("Textures/menu_bg");
        if (bgTex != null)
        {
            var img = UIFactory.CreateImage(_root, "Bg", ProceduralAssets.MakeSprite(bgTex), Vector2.zero,
                new Vector2(4000f, 2500f), Color.white);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }
        UIFactory.CreatePanel(_root, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.45f));
    }

    // ================= ГЕЙТ 18+ =================

    private void BuildGate()
    {
        _gatePanel = UIFactory.CreateCenterPanel(_root, "AgeGate", 760f, 420f);
        var title = UIFactory.CreateLabel(_gatePanel, "Title", "ВНИМАНИЕ: 18+", 46, TextAnchor.MiddleCenter,
            UIFactory.Accent, FontStyle.Bold);
        title.go.GetComponent<RectTransform>().offsetMin = new Vector2(40f, -80f);
        title.go.GetComponent<RectTransform>().offsetMax = new Vector2(-40f, -30f);

        var text = UIFactory.CreateLabel(_gatePanel, "Text",
            "Игра содержит чёрный юмор, сцены выбрасывания пассажиров за борт,\n" +
            "драки, алкогольные отсылки и полное пренебрежение авиабезопасностью.\n\n" +
            "Создана в развлекательных целях. Не повторяйте в реальной жизни.\nВсе совпадения случайны.",
            24, TextAnchor.UpperCenter, UIFactory.TextMain);
        text.go.GetComponent<RectTransform>().offsetMin = new Vector2(50f, 110f);
        text.go.GetComponent<RectTransform>().offsetMax = new Vector2(-50f, -90f);

        UIFactory.CreateButton(_gatePanel, "Yes", "МНЕ 18 ЛЕТ ИЛИ БОЛЬШЕ — ЛЕТИМ", new Vector2(0f, -115f),
            new Vector2(480f, 66f), 26, () =>
        {
            GameSettings.Current.ageConfirmed = true;
            GameSettings.Save();
            ProceduralAudio.PlayUiClick();
            _gatePanel.gameObject.SetActive(false);
        });

        UIFactory.CreateButton(_gatePanel, "No", "Выйти", new Vector2(0f, -195f),
            new Vector2(480f, 56f), 22, () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }, new Color(0.35f, 0.15f, 0.12f));

        _gatePanel.gameObject.SetActive(!GameSettings.Current.ageConfirmed);
    }

    // ================= ГЛАВНОЕ МЕНЮ =================

    private void BuildMain()
    {
        _mainPanel = new GameObject("MainPanel", typeof(RectTransform)).GetComponent<RectTransform>();
        _mainPanel.SetParent(_root, false);
        _mainPanel.anchorMin = _mainPanel.anchorMax = Vector2.one;
        _mainPanel.offsetMin = _mainPanel.offsetMax = Vector2.zero;

        var title = UIFactory.CreateLabel(_mainPanel, "Title", "DEAR PASSENGERS", 74,
            TextAnchor.UpperCenter, Color.white, FontStyle.Bold);
        title.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -140f);
        title.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -50f);

        var subtitle = UIFactory.CreateLabel(_mainPanel, "Subtitle",
            "неофициальное фан-переиздание · кооперативный хаос «худшей авиакомпании мира»",
            22, TextAnchor.UpperCenter, new Color(1f, 0.8f, 0.55f));
        subtitle.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -180f);
        subtitle.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -140f);

        var joke = UIFactory.CreateLabel(_mainPanel, "Joke",
            MenuJokes[_jokeRng.Next(MenuJokes.Length)], 22, TextAnchor.UpperCenter, UIFactory.TextDim);
        joke.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -230f);
        joke.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -190f);

        UIFactory.CreateButton(_mainPanel, "Solo", "ОДИНОЧНЫЙ РЕЙС", new Vector2(0f, 90f),
            new Vector2(420f, 62f), 26, () =>
        {
            ProceduralAudio.PlayUiClick();
            if (!NetworkLauncher.StartSolo()) ShowError(NetworkLauncher.LastError);
        });

        UIFactory.CreateButton(_mainPanel, "Host", "КООПЕРАТИВ — СОЗДАТЬ ЛОББИ", new Vector2(0f, 16f),
            new Vector2(420f, 62f), 26, () =>
        {
            ProceduralAudio.PlayUiClick();
            SceneManager.LoadScene(GameBootstrap.SceneLobby, LoadSceneMode.Single);
        });

        UIFactory.CreateButton(_mainPanel, "Join", "ПОДКЛЮЧИТЬСЯ ПО КОДУ", new Vector2(0f, -58f),
            new Vector2(420f, 62f), 26, () =>
        {
            ProceduralAudio.PlayUiClick();
            _codeInput = "";
            _joinPanel.gameObject.SetActive(true);
        });

        UIFactory.CreateButton(_mainPanel, "Settings", "НАСТРОЙКИ", new Vector2(0f, -132f),
            new Vector2(420f, 62f), 26, () =>
        {
            ProceduralAudio.PlayUiClick();
            SettingsMenu.Open(_root, null);
        });

        UIFactory.CreateButton(_mainPanel, "Quit", "ВЫХОД", new Vector2(0f, -206f),
            new Vector2(420f, 62f), 26, () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }, new Color(0.3f, 0.14f, 0.1f));

        var footer = UIFactory.CreateLabel(_mainPanel, "Disclaimer", Disclaimer, 20,
            TextAnchor.LowerCenter, UIFactory.TextDim);
        footer.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 10f);
        footer.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 40f);

        var version = UIFactory.CreateLabel(_mainPanel, "Version", "v1.0.0 · Unity 2022.3 · Netcode for GameObjects",
            18, TextAnchor.LowerRight, UIFactory.TextDim);
        version.go.GetComponent<RectTransform>().offsetMin = new Vector2(-420f, 8f);
        version.go.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, 30f);

        _errorLabel = UIFactory.CreateLabel(_mainPanel, "Error", "", 22, TextAnchor.MiddleCenter,
            new Color(1f, 0.45f, 0.35f));
        _errorLabel.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -290f);
        _errorLabel.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -250f);
    }

    private void ShowError(string text)
    {
        if (_errorLabel != null && !string.IsNullOrEmpty(text)) _errorLabel.SetText(text);
    }

    // ================= ПАНЕЛЬ ПОДКЛЮЧЕНИЯ =================

    private void BuildJoin()
    {
        _joinPanel = UIFactory.CreateCenterPanel(_root, "JoinPanel", 700f, 460f);

        var title = UIFactory.CreateLabel(_joinPanel, "Title", "ПОДКЛЮЧЕНИЕ К РЕЙСУ", 36,
            TextAnchor.UpperCenter, Color.white, FontStyle.Bold);
        title.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -70f);
        title.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -20f);

        var hint = UIFactory.CreateLabel(_joinPanel, "Hint",
            "Введите 6-символьный код лобби (или IP:порт хоста с точками) и нажмите Enter.\n" +
            "Код даёт хост в лобби. Например: ABCDEF или 192.168.1.5:7777",
            22, TextAnchor.UpperCenter, UIFactory.TextDim);
        hint.go.GetComponent<RectTransform>().offsetMin = new Vector2(30f, -130f);
        hint.go.GetComponent<RectTransform>().offsetMax = new Vector2(-30f, -80f);

        _joinCodeLabel = UIFactory.CreateLabel(_joinPanel, "Code", "_ _ _ _ _ _", 52,
            TextAnchor.MiddleCenter, UIFactory.Accent, FontStyle.Bold);
        _joinCodeLabel.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -220f);
        _joinCodeLabel.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -140f);

        var err = UIFactory.CreateLabel(_joinPanel, "Err", "", 22, TextAnchor.MiddleCenter,
            new Color(1f, 0.45f, 0.35f));
        err.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -260f);
        err.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -226f);

        UIFactory.CreateButton(_joinPanel, "Cancel", "НАЗАД (ESC)", new Vector2(0f, -185f),
            new Vector2(320f, 56f), 24, () => _joinPanel.gameObject.SetActive(false));

        _joinErrLabel = err;
        _joinPanel.gameObject.SetActive(false);
    }

    private UiLabel _joinErrLabel;

    // ================= ПАНЕЛЬ «ПОДКЛЮЧЕНИЕ…» =================

    private void BuildConnecting()
    {
        _connectingPanel = UIFactory.CreateCenterPanel(_root, "ConnectingPanel", 600f, 300f);
        _connectLabel = UIFactory.CreateLabel(_connectingPanel, "Text", "ПОДКЛЮЧЕНИЕ…", 40,
            TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        _connectLabel.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -40f);
        _connectLabel.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 40f);

        UIFactory.CreateButton(_connectingPanel, "Cancel", "ОТМЕНИТЬ", new Vector2(0f, -95f),
            new Vector2(320f, 56f), 24, () =>
        {
            NetworkLauncher.CancelConnect();
            _connectingPanel.gameObject.SetActive(false);
        });
        _connectingPanel.gameObject.SetActive(false);
    }

    // ================= ОБНОВЛЕНИЕ =================

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Ввод кода лобби.
        if (_joinPanel.gameObject.activeSelf)
        {
            if (kb.escapeKey.wasPressedThisFrame)
            {
                _joinPanel.gameObject.SetActive(false);
                return;
            }
            foreach (var key in kb.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                string c = key.displayName;
                if (c != null && c.Length == 1)
                {
                    char ch = char.ToUpperInvariant(c[0]);
                    if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == ':')
                    {
                        if (_codeInput.Length < 21) _codeInput += ch;
                    }
                }
            }
            if (kb.backspaceKey.wasPressedThisFrame && _codeInput.Length > 0)
            {
                _codeInput = _codeInput.Substring(0, _codeInput.Length - 1);
            }
            _joinCodeLabel.SetText(_codeInput.Length > 0 ? _codeInput : "_ _ _ _ _ _");

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_codeInput.Length >= 6)
                {
                    if (NetworkLauncher.Join(_codeInput))
                    {
                        _joinPanel.gameObject.SetActive(false);
                        _connectingPanel.gameObject.SetActive(true);
                    }
                    else
                    {
                        _joinErrLabel.SetText(NetworkLauncher.LastError ?? "Не удалось");
                    }
                }
                else
                {
                    _joinErrLabel.SetText("Код — 6 символов.");
                }
            }
        }

        // Ожидание подключения.
        if (_connectingPanel.gameObject.activeSelf)
        {
            if (NetworkLauncher.InSession && NetworkLauncher.NM != null && NetworkLauncher.NM.IsConnectedClient)
            {
                // Сцена синхронизируется сама; на всякий случай скрываем панель.
                _connectingPanel.gameObject.SetActive(false);
            }
            else if (!NetworkLauncher.IsConnecting)
            {
                _connectingPanel.gameObject.SetActive(false);
                ShowError(NetworkLauncher.LastError ?? "Подключение не удалось.");
            }
            else
            {
                _connectLabel.SetText($"ПОДКЛЮЧЕНИЕ… {Mathf.FloorToInt(Time.unscaledTime) % 4}. ");
            }
        }
    }
}
