// PassengerManager.cs — серверный менеджер салона: посадка пассажиров, глобальный стресс,
// настроение рейса, удары игроков, выброс пассажиров, статистика, «проблемный» груз.

using Unity.Netcode;
using UnityEngine;

/// <summary>Менеджер пассажиров (создаётся в каждой игровой сцене).</summary>
public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance { get; set; }

    /// <summary>true — сцена стоянки.</summary>
    public bool boardingScene;

    /// <summary>Сколько пассажиров летит этим рейсом.</summary>
    public const int PassengerCount = 12;

    private float _boardTimer = 2f;
    private int _spawnedCount;
    private float _moodTimer;
    private bool _allSpawned;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!NetworkLauncher.IsServer) return;
        var session = GameSession.Instance;
        if (session == null) return;

        // ---------- Посадка (сцена стоянки) ----------
        if (!_allSpawned)
        {
            _boardTimer -= Time.deltaTime;
            if (_boardTimer <= 0f && AirplaneController.Instance != null && AirplaneController.Instance.Refs != null)
            {
                _boardTimer = 1.6f; // пассажиры заходят друг за другом
                if (SpawnPassenger(session)) _spawnedCount++;
                if (_spawnedCount >= PassengerCount) _allSpawned = true;
            }
        }

        // ---------- Настроение салона (1 Гц) ----------
        _moodTimer -= Time.deltaTime;
        if (_moodTimer <= 0f)
        {
            _moodTimer = 1f;
            float mood = ComputeMood();
            session.Mood.Value = Mathf.Lerp(session.Mood.Value, mood, 0.2f);

            // Статистика.
            int calm = 0, panic = 0;
            foreach (var npc in PassengerNPC.All)
            {
                if (npc == null) continue;
                if (npc.State == PassengerState.Calm || npc.State == PassengerState.Calmed) calm++;
                else if (npc.State == PassengerState.Panic || npc.State == PassengerState.Aggression) panic++;
            }
            session.StatPaxTotal.Value = PassengerNPC.All.Count;
            session.StatPaxCalm.Value = calm;
            session.StatPaxPanic.Value = panic;
        }

        // ---------- Проблемный груз ----------
        HandleCargoTrouble(session);
    }

    /// <summary>Заспавнить одного пассажира (сервер).</summary>
    private bool SpawnPassenger(GameSession session)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/PassengerNPC");
        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("[PassengerManager] Префаб PassengerNPC не готов (нужен мастер настройки проекта).");
            _allSpawned = true; // не спамим ошибкой
            return false;
        }

        int seat = _spawnedCount;
        var go = Instantiate(prefab);
        go.name = $"Passenger_{seat}";
        var npc = go.GetComponent<PassengerNPC>();
        var no = go.GetComponent<NetworkObject>();

        // Инициализация сетевых полей ДО спавна, чтобы клиенты сразу получили значения.
        npc.NetSeat.Value = seat;
        npc.NetPersonality.Value = Random.Range(0, 4);
        // Старт — у входной двери.
        Vector3 doorPos = new Vector3(-0.8f, AirplaneBuilder.FloorY + 0.9f, 3.5f);
        go.transform.position = doorPos;
        npc.NetPos.Value = doorPos;
        no.Spawn(false); // переживают смену сцены (стоянка → полёт)

        return true;
    }

    /// <summary>Настроение 0..100 по состоянию салона.</summary>
    private float ComputeMood()
    {
        if (PassengerNPC.All.Count == 0) return 50f;
        float sum = 0f;
        foreach (var npc in PassengerNPC.All)
        {
            if (npc == null) continue;
            switch (npc.State)
            {
                case PassengerState.Calm:
                case PassengerState.Calmed: sum += 90f; break;
                case PassengerState.Panic: sum += 25f; break;
                case PassengerState.Aggression: sum += 10f; break;
                case PassengerState.Carried: sum += 40f; break;
                case PassengerState.Ejected: sum += 35f; break; // выброс нервирует оставшихся
            }
        }
        return sum / PassengerNPC.All.Count;
    }

    /// <summary>События «проблемного» груза во время полёта.</summary>
    private float _cargoTroubleTimer = 20f;
    private void HandleCargoTrouble(GameSession session)
    {
        if (session.Phase.Value != (int)GamePhase.Flight) return;
        var cargo = GameSession.CargoOptions[Mathf.Clamp(session.CargoId.Value, 0, 2)];
        if (cargo.trouble < 0.3f) return;

        _cargoTroubleTimer -= Time.deltaTime;
        if (_cargoTroubleTimer > 0f) return;
        _cargoTroubleTimer = Mathf.Lerp(75f, 25f, cargo.trouble) + Random.Range(0f, 20f);

        bool fragile = session.CargoId.Value == 1;
        bool contraband = session.CargoId.Value == 2;
        if (fragile && session.Turbulence.Value > 0.3f)
        {
            AddStressAll(12f, "ХРУСТЬ! Вазы!!! ВАЗЫ ПОБИЛИСЬ!");
            session.HostSfx(Vector3.zero, 0);
        }
        else if (contraband && Random.value < 0.6f)
        {
            AddStressAll(18f, "Чем это пахнет?! Аппарат шалит!");
            session.HostSfx(Vector3.zero, 3);
        }
    }

    // ================= Взаимодействия (сервер) =================

    /// <summary>Глобальный стресс всем пассажирам.</summary>
    public void AddStressAll(float amount, string reason)
    {
        if (!NetworkLauncher.IsServer) return;
        foreach (var npc in PassengerNPC.All)
        {
            if (npc == null) continue;
            npc.ServerAddStress(amount);
            if (!string.IsNullOrEmpty(reason) && Random.value < 0.4f) npc.SayExternal(reason);
        }
    }

    /// <summary>Удар игрока (из NetworkPlayer.SendPunchServerRpc).</summary>
    public void HandlePunch(Vector3 origin, Vector3 direction, ulong byClient)
    {
        if (!NetworkLauncher.IsServer) return;
        PassengerNPC best = null;
        float bestScore = 999f;
        foreach (var npc in PassengerNPC.All)
        {
            if (npc == null || npc.State == PassengerState.Ejected || npc.State == PassengerState.Carried) continue;
            Vector3 to = npc.transform.position + Vector3.up * 1.0f - origin;
            float dist = to.magnitude;
            if (dist > 2.4f) continue;
            float dot = Vector3.Dot(to.normalized, direction.normalized);
            if (dot < 0.7f) continue; // вне конуса удара
            if (dist < bestScore) { bestScore = dist; best = npc; }
        }
        if (best != null) best.PlayerPunch(byClient);
    }

    /// <summary>Выбросить несомого пассажира (ЛКМ у открытой двери).</summary>
    public void HandleThrowBy(ulong byClient)
    {
        if (!NetworkLauncher.IsServer) return;
        foreach (var npc in PassengerNPC.All)
        {
            if (npc == null || npc.CarriedBy.Value != byClient) continue;
            var session = GameSession.Instance;
            bool doorOpen = session != null && session.DoorOpen.Value;
            if (doorOpen)
            {
                npc.Eject(); // 18+ тёмный юмор
            }
            else
            {
                npc.Drop();
            }
            return;
        }
    }

    /// <summary>Пассажир начал буянить (для объявления о драке).</summary>
    public void OnFightStarted(PassengerNPC npc)
    {
        var session = GameSession.Instance;
        if (session == null) return;
        if (_fightAnnounceCd <= 0f)
        {
            _fightAnnounceCd = 40f;
            session.HostAnnounce(AnnouncementType.Fight);
        }
    }

    private float _fightAnnounceCd;
    public void OnUnrest(PassengerNPC npc) { /* хук на будущее (камера, музыка) */ }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
