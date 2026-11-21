// GameBootstrap.cs — точка входа игры. Сцены-«раковины» не содержат серифованных ссылок:
// весь контент строится здесь по имени загруженной сцены.
// Работает и в билде, и в редакторе; подписка на смену сцен даёт корректную перестройку мира.

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Маршрутизатор сцен: строит содержимое каждой сцены при её загрузке.</summary>
public static class GameBootstrap
{
    public const string SceneMainMenu = "MainMenu";
    public const string SceneLobby = "Lobby";
    public const string SceneAirplane = "Airplane";   // стоянка/посадка пассажиров
    public const string SceneFlight = "Flight";       // основной полёт

    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        GameSettings.Load();
        Application.targetFrameRate = -1;               // без ограничения FPS
        QualitySettings.vSyncCount = 1;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        // Обрабатываем сцену, которая уже открыта на момент старта процесса.
        HandleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return; // используем только Single-сцены
        HandleScene(scene.name);
    }

    /// <summary>Очистка локальных статических ссылок перед новой сценой.</summary>
    private static void ResetSceneStatics()
    {
        HUD.Instance = null;
        RadioAnnouncer.Instance = null;
        VoiceChatManager.Instance = null;
        GameWorld.Instance = null;
        AirplaneController.Instance = null;
        PassengerManager.Instance = null;
        BirdSpawner.Instance = null;
        RefuelingMinigame.Instance = null;
        PassengerNPC.ClearAll();
    }

    private static void HandleScene(string sceneName)
    {
        ResetSceneStatics();
        Debug.Log($"[GameBootstrap] Сцена: {sceneName}");

        switch (sceneName)
        {
            case SceneMainMenu:
            {
                var host = new GameObject("MainMenuController");
                host.AddComponent<MainMenu>();
                break;
            }
            case SceneLobby:
            {
                var world = new GameObject("LobbyWorld");
                world.AddComponent<LobbyWorld>();
                var mgr = new GameObject("LobbyManager");
                mgr.AddComponent<LobbyManager>();
                break;
            }
            case SceneAirplane:
            {
                BuildGameplayScene(boarding: true);
                break;
            }
            case SceneFlight:
            {
                BuildGameplayScene(boarding: false);
                break;
            }
            default:
                Debug.LogWarning($"[GameBootstrap] Неизвестная сцена '{sceneName}' — пусто.");
                break;
        }
    }

    /// <summary>Строит игровую сцену (стоянка или полёт): мир, самолёт, HUD, звук, менеджеры.</summary>
    private static void BuildGameplayScene(bool boarding)
    {
        // Мир (небо, облака, земля) — на каждом пире локально.
        var worldGo = new GameObject("GameWorld");
        var world = worldGo.AddComponent<GameWorld>();
        world.boardingScene = boarding;

        // Самолёт + контроллер полёта.
        var planeGo = new GameObject("Airplane");
        var controller = planeGo.AddComponent<AirplaneController>();
        controller.boardingScene = boarding;

        // HUD, радио и голосовой чат — локальные для каждого игрока.
        new GameObject("HUD").AddComponent<HUD>();
        new GameObject("RadioAnnouncer").AddComponent<RadioAnnouncer>();
        new GameObject("VoiceChatManager").AddComponent<VoiceChatManager>();

        // Серверные менеджеры (пассажиры, птицы, заправка) — симуляция только на хосте,
        // но компоненты создаются у всех, чтобы видеть визуальные части.
        var passMgr = new GameObject("PassengerManager").AddComponent<PassengerManager>();
        passMgr.boardingScene = boarding;

        new GameObject("BirdSpawner").AddComponent<BirdSpawner>();
        new GameObject("RefuelingMinigame").AddComponent<RefuelingMinigame>();
    }
}
