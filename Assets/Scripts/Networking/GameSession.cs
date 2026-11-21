// GameSession.cs — единый сетевой объект рейса: состояние полёта (NetworkVariable) для всех
// клиентов + серверные RPC (пилотирование, выбор груза, взлёт) и клиентские события (объявления,
// стаи птиц, звуки). Хост создаёт его при входе в сцену Airplane; живёт до конца рейса.

using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Фазы рейса.</summary>
public enum GamePhase
{
    Boarding = 0, // стоянка, посадка пассажиров
    Takeoff = 1,  // разбег и набор высоты
    Flight = 2,   // крейсерский полёт (основной геймплей)
    Landing = 3,  // заход на посадку
    Landed = 4,   // успешная посадка
    Crashed = 5,  // крушение
}

/// <summary>Сетевое состояние рейса (один объект на весь матч).</summary>
public class GameSession : NetworkBehaviour
{
    public static GameSession Instance { get; private set; }

    /// <summary>Никто не пилотирует.</summary>
    public const ulong NoPilot = ulong.MaxValue;

    // ---------- Состояние полёта (пишет сервер, читают все) ----------
    public readonly NetworkVariable<float> SpeedKmh = new(0f);
    public readonly NetworkVariable<float> AltitudeM = new(0f);
    public readonly NetworkVariable<float> HeadingDeg = new(0f);
    public readonly NetworkVariable<float> PitchDeg = new(0f);
    public readonly NetworkVariable<float> RollDeg = new(0f);
    public readonly NetworkVariable<float> Fuel = new(900f);          // литры, бак 1200
    public readonly NetworkVariable<float> Mood = new(80f);           // настроение салона 0..100
    public readonly NetworkVariable<float> Throttle = new(0f);        // тяга 0..1
    public readonly NetworkVariable<float> Turbulence = new(0f);      // 0..1
    public readonly NetworkVariable<int> Phase = new((int)GamePhase.Boarding);
    public readonly NetworkVariable<int> DistanceToDestM = new(60000); // до аэропорта назначения
    public readonly NetworkVariable<int> Engine1Health = new(100);
    public readonly NetworkVariable<int> Engine2Health = new(100);
    public readonly NetworkVariable<bool> Engine1Fire = new(false);
    public readonly NetworkVariable<bool> Engine2Fire = new(false);
    public readonly NetworkVariable<int> WindshieldCracks = new(0);
    public readonly NetworkVariable<int> CargoId = new(0);            // 0..2
    public readonly NetworkVariable<bool> DoorOpen = new(false);
    public readonly NetworkVariable<int> RefuelStage = new(0);        // 0 нет,1 летит,2 идёт заправка,3 готово
    public readonly NetworkVariable<float> RefuelProgress = new(0f);  // 0..1 доля заполнения за попытку

    // Ввод пилота (сервер пишет из RPC, клиенты читают для анимации штурвала)
    public readonly NetworkVariable<float> PilotPitch = new(0f);
    public readonly NetworkVariable<float> PilotRoll = new(0f);
    public readonly NetworkVariable<float> PilotYaw = new(0f);
    public readonly NetworkVariable<float> PilotThrottle = new(0f);
    public readonly NetworkVariable<ulong> PilotClientId = new(NoPilot);

    // ---------- Статистика ----------
    public readonly NetworkVariable<int> StatPaxTotal = new(0);
    public readonly NetworkVariable<int> StatPaxCalm = new(0);
    public readonly NetworkVariable<int> StatPaxPanic = new(0);
    public readonly NetworkVariable<int> StatEjected = new(0);
    public readonly NetworkVariable<int> StatBirdsEaten = new(0);
    public readonly NetworkVariable<int> StatRefueledLiters = new(0);

    /// <summary>Максимальная ёмкость бака, л.</summary>
    public const float FuelCapacity = 1200f;

    /// <summary>Грузы рейса: название, множитель оплаты, «проблемность».</summary>
    public static readonly (string name, int pay, float trouble)[] CargoOptions = new[]
    {
        ("Бандероли бабушкам", 800, 0.15f),
        ("Стеклянные вазы (хрупко!)", 2200, 0.55f),
        ("Контрабандный самогонный аппарат", 4200, 0.95f),
    };

    /// <summary>Локальное событие: заспавнилась стая (seed) — симулируют все клиенты.</summary>
    public event Action<int> FlockSpawned;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        // Клиенты, подключившиеся позже, получают актуальные значения автоматически.
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    // ================= RPC: запросы клиентов =================

    /// <summary>Занять/освободить штурвал.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestPilotServerRpc(bool enter, ServerRpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        if (!enter)
        {
            if (PilotClientId.Value == caller) PilotClientId.Value = NoPilot;
            return;
        }
        if (PilotClientId.Value != NoPilot && PilotClientId.Value != caller) return; // место занято
        PilotClientId.Value = caller;
    }

    /// <summary>Ввод пилота (клиент → сервер, ~20 Гц): тангаж, крен, рыскание, тяга 0..1.</summary>
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void SetPilotInputServerRpc(float pitch, float roll, float yaw, float throttle)
    {
        PilotPitch.Value = Mathf.Clamp(pitch, -1f, 1f);
        PilotRoll.Value = Mathf.Clamp(roll, -1f, 1f);
        PilotYaw.Value = Mathf.Clamp(yaw, -1f, 1f);
        PilotThrottle.Value = Mathf.Clamp(throttle, 0f, 1f);
    }

    /// <summary>Выбор груза до взлёта (только хост/владелец).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChooseCargoServerRpc(int cargoId)
    {
        if (Phase.Value != (int)GamePhase.Boarding) return;
        CargoId.Value = Mathf.Clamp(cargoId, 0, CargoOptions.Length - 1);
    }

    /// <summary>Состояние выравнивания шланга от заправщика (мини-игра).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetRefuelAlignmentServerRpc(bool inRing)
    {
        if (RefuelingMinigame.Instance != null) RefuelingMinigame.Instance.ServerSetAlignment(inRing);
    }

    /// <summary>Открыть/закрыть главную дверь (серверная переменная).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        DoorOpen.Value = !DoorOpen.Value;
        // Открытая дверь в полёте нервирует салон.
        if (DoorOpen.Value && Phase.Value == (int)GamePhase.Flight)
        {
            if (PassengerManager.Instance != null)
            {
                PassengerManager.Instance.AddStressAll(8f, "Дверь открыта?!");
            }
        }
    }

    /// <summary>Подтверждение взлёта (кнопка Y у владельца лобби на стоянке).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ConfirmTakeoffServerRpc()
    {
        if (!IsServer) return;
        if (Phase.Value != (int)GamePhase.Boarding) return;
        var airplane = AirplaneController.Instance;
        if (airplane != null) airplane.BeginTakeoff();
    }

    // ================= RPC: события сервера =================

    /// <summary>Объявление капитана для всех (включая хост).</summary>
    public void HostAnnounce(AnnouncementType type)
    {
        AnnounceClientRpc((int)type);
        if (RadioAnnouncer.Instance != null) RadioAnnouncer.Instance.Enqueue(type);
    }

    [ClientRpc]
    private void AnnounceClientRpc(int type)
    {
        if (RadioAnnouncer.Instance != null) RadioAnnouncer.Instance.Enqueue((AnnouncementType)type);
    }

    /// <summary>Заспавнить стаю птиц с общим seed (детерминированная симуляция на всех пирах).</summary>
    public void HostSpawnFlock(int seed)
    {
        FlockClientRpc(seed);
        FlockSpawned?.Invoke(seed);
    }

    [ClientRpc]
    private void FlockClientRpc(int seed)
    {
        FlockSpawned?.Invoke(seed);
    }

    /// <summary>Звуковой 3D-эффект для всех.</summary>
    public void HostSfx(Vector3 pos, int kind)
    {
        SfxClientRpc(pos, kind);
        PlaySfxLocal(pos, kind);
    }

    [ClientRpc]
    private void SfxClientRpc(Vector3 pos, int kind) => PlaySfxLocal(pos, kind);

    private static void PlaySfxLocal(Vector3 pos, int kind)
    {
        switch (kind)
        {
            case 0: ProceduralAudio.PlayAt(pos, ProceduralAudio.Thunk, 1f); break;   // удар птицы
            case 1: ProceduralAudio.PlayAt(pos, ProceduralAudio.Punch, 0.9f); break; // удар кулаком
            case 2: ProceduralAudio.PlayAt(pos, ProceduralAudio.Scream, 0.9f); break;// крик пассажира
            case 3: ProceduralAudio.PlayAt(pos, ProceduralAudio.Alarm, 0.7f); break; // тревога
        }
    }

    // ================= Хелперы =================

    /// <summary>Текст груза для HUD.</summary>
    public string CargoName => CargoOptions[Mathf.Clamp(CargoId.Value, 0, CargoOptions.Length - 1)].name;

    /// <summary>Имя игрока по clientId (для индикатора пилота и статистики).</summary>
    public static FixedString64Bytes NameFor(ulong clientId)
    {
        foreach (var p in NetworkPlayer.All)
        {
            if (p != null && p.OwnerClientId == clientId) return p.PlayerName.Value;
        }
        return new FixedString64Bytes("?");
    }
}
