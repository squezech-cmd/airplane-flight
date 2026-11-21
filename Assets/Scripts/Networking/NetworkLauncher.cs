// NetworkLauncher.cs — создание и настройка NetworkManager во время выполнения (runtime-bootstrap):
// UnityTransport, PlayerPrefab, подключение дополнительных сетевых префабов, approval,
// запуск соло/хостом/клиентом, аккуратный выход в меню.

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Управление сетевым lifecycle. NetworkManager создаётся кодом один раз за сессию.</summary>
public static class NetworkLauncher
{
    /// <summary>Текущий NetworkManager (может быть null — вне сети).</summary>
    public static NetworkManager NM { get; private set; }

    /// <summary>Последняя ошибка подключения (для UI).</summary>
    public static string LastError { get; private set; }

    /// <summary>Идёт ли сейчас попытка подключения клиентом.</summary>
    public static bool IsConnecting { get; private set; }

    /// <summary>Точка спавна игроков в текущей сцене (ставится построителем сцены).</summary>
    public static Vector3? SpawnPoint { get; set; }
    public static float SpawnYaw { get; set; }

    public static bool IsServer => NM != null && NM.IsListening && NM.IsServer;
    public static bool IsClient => NM != null && NM.IsListening && NM.IsClient;
    public static bool InSession => NM != null && NM.IsListening;

    /// <summary>Создать NetworkManager (однократно).</summary>
    public static NetworkManager EnsureNetworkManager()
    {
        if (NM != null) return NM;

        var playerPrefab = LoadNetworkPrefab("Player");
        if (playerPrefab == null)
        {
            LastError = "Префаб Player не найден в Resources/Prefabs. Откройте проект в Unity один раз (мастер настройки добавит компоненты) и пересоберите.";
            Debug.LogError("[NetworkLauncher] " + LastError);
            return null;
        }

        var go = new GameObject("NetworkManager");
        Object.DontDestroyOnLoad(go);
        var nm = go.AddComponent<NetworkManager>();
        var ut = go.AddComponent<UnityTransport>();

        nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = ut,
            PlayerPrefab = playerPrefab,
            EnableSceneManagement = true,
            ConnectionApproval = true,
            TickRate = 30,
        };

        // Дополнительные сетевые префабы.
        nm.AddNetworkPrefab(playerPrefab); // идемпотентно (дубликаты игнорируются)
        foreach (var name in new[] { "PassengerNPC", "GameSession" })
        {
            var p = LoadNetworkPrefab(name);
            if (p != null) nm.AddNetworkPrefab(p);
        }

        nm.ConnectionApprovalCallback = ApproveConnection;

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;

        NM = nm;
        LastError = null;
        return nm;
    }

    private static GameObject LoadNetworkPrefab(string name)
    {
        var prefab = Resources.Load<GameObject>($"Prefabs/{name}");
        if (prefab == null)
        {
            Debug.LogError($"[NetworkLauncher] Префаб Resources/Prefabs/{name} не найден.");
            return null;
        }
        if (prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"[NetworkLauncher] В префабе {name} нет NetworkObject! Запустите ProjectSetupWizard (Assets → Dear Passengers → Настроить проект).");
            return null;
        }
        return prefab;
    }

    private static void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Принимаем до 8 подключений (игровая цель — экипаж до 4).
        bool approved = NM.ConnectedClients.Count <= 8;
        response.Approved = approved;
        response.CreatePlayerObject = true;
        if (approved)
        {
            response.Position = SpawnPoint ?? Vector3.zero;
            response.Rotation = Quaternion.Euler(0f, SpawnYaw, 0f);
        }
    }

    private static void OnClientConnected(ulong clientId)
    {
        if (IsConnecting && NM != null && clientId == NM.LocalClientId)
        {
            IsConnecting = false; // клиент успешно подключился — сцена синхронизируется сама
        }
    }

    private static void OnClientDisconnected(ulong clientId)
    {
        if (NM == null) return;
        // Наш локальный клиент отвалился (хост закрыл лобби/таймаут) — возвращаемся в меню.
        if (!NM.IsServer && clientId == NM.LocalClientId)
        {
            LastError = "Соединение с рейсом потеряно.";
            LeaveToMenu();
        }
    }

    // ---------- Режимы запуска ----------

    /// <summary>Одиночный рейс: хост без внешних клиентов, сразу сцена стоянки.</summary>
    public static bool StartSolo()
    {
        var nm = EnsureNetworkManager();
        if (nm == null) return false;
        LastError = null;
        if (!nm.StartHost())
        {
            LastError = "Не удалось запустить локальный сервер (порт занят?).";
            return false;
        }
        nm.SceneManager.LoadScene(GameBootstrap.SceneAirplane, LoadSceneMode.Single);
        return true;
    }

    /// <summary>Создать кооп-лобби (вызывается уже в сцене Lobby).</summary>
    public static bool StartLobbyHost()
    {
        var nm = EnsureNetworkManager();
        if (nm == null) return false;
        LastError = null;
        var ut = (UnityTransport)nm.NetworkConfig.NetworkTransport;
        ut.SetConnectionData("0.0.0.0", LobbyCodes.DefaultPort);
        if (!nm.StartHost())
        {
            LastError = $"Не удалось открыть лобби (порт {LobbyCodes.DefaultPort} занят?).";
            return false;
        }
        return true;
    }

    /// <summary>Подключиться по коду или IP:port. true — попытка запущена.</summary>
    public static bool Join(string codeOrAddress)
    {
        var nm = EnsureNetworkManager();
        if (nm == null) return false;
        if (!LobbyCodes.TryParse(codeOrAddress, out string address, out ushort port))
        {
            LastError = "Неверный код лобби (6 символов) или адрес (например 192.168.1.5:7777).";
            return false;
        }
        LastError = null;
        var ut = (UnityTransport)nm.NetworkConfig.NetworkTransport;
        ut.SetConnectionData(address, port);
        IsConnecting = true;
        if (!nm.StartClient())
        {
            LastError = "Не удалось запустить клиент.";
            IsConnecting = false;
            return false;
        }
        return true;
    }

    /// <summary>Отменить попытку подключения (кнопка «Отмена»).</summary>
    public static void CancelConnect()
    {
        if (IsConnecting && NM != null)
        {
            NM.Shutdown();
            Object.Destroy(NM.gameObject);
            NM = null;
            IsConnecting = false;
        }
    }

    /// <summary>Выйти в главное меню (и разорвать сессию).</summary>
    public static void LeaveToMenu()
    {
        IsConnecting = false;
        if (NM != null)
        {
            NM.Shutdown();
            Object.Destroy(NM.gameObject);
            NM = null;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SceneManager.GetActiveScene().name != GameBootstrap.SceneMainMenu)
        {
            SceneManager.LoadScene(GameBootstrap.SceneMainMenu, LoadSceneMode.Single);
        }
    }
}
