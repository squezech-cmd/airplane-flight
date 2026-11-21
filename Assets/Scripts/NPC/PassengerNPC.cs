// PassengerNPC.cs — пассажир: полная машина состояний (Calm/Panic/Aggression/Calmed/Carried/Ejected),
// процедурное тело с анимациями, реплики-«пузыри», стресс, драки, заражение паникой, выброс за борт.
// Симуляция — на сервере; визуал и анимации — на всех клиентах.

using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Состояния пассажира.</summary>
public enum PassengerState { Calm = 0, Panic = 1, Aggression = 2, Calmed = 3, Carried = 4, Ejected = 5 }

/// <summary>Тип характера (влияет на стойкость к стрессу).</summary>
public enum Personality { Normal = 0, Nervous = 1, Rowdy = 2, Drunk = 3 }

/// <summary>Один пассажир.</summary>
public class PassengerNPC : NetworkBehaviour
{
    /// <summary>Все живые пассажиры.</summary>
    public static readonly List<PassengerNPC> All = new List<PassengerNPC>();

    /// <summary>Очистка реестра (при смене сцены).</summary>
    public static void ClearAll() => All.Clear();

    public static readonly string[] Names =
    {
        "Иван П.", "Тамара Львовна", "Витёк", "Марья Семёновна", "Гена", "Люся",
        "Аркадий Пётрович", "Зина", "Костян", "Оля", "Дядя Слава", "Нинель",
        "Толик", "Алла Борисовна", "Жора", "Клава",
    };

    private static readonly string[][] Bubbles = new string[][]
    {
        // Calm
        new[] { "Ещё бы воды…", "Долго лететь?", "А кормить будут?", "Тут сквозняк.", "*читает газету*" },
        // Panic
        new[] { "МЫ ВСЕ УМРЁМ!!!", "ПОЧЕМУ МЫ КАТИМСЯ ВНИЗ?!", "Я ХОЧУ К МАМЕ!", "ДВЕРЬ! ОТКРОЙТЕ ДВЕРЬ!", "АААААА!" },
        // Aggression
        new[] { "А ТЫ ЧЁ?! ", "ВЫХОДИ, ПОГОВОРИМ!", "Я С ТВОЕГО СЕЛА!", "ЗА ЧТО?! А НУ СЮДА!", "ДА Я!!!" },
        // Calmed
        new[] { "Спасибо… дышу…", "Всё хорошо… хорошо…", "Валерьянка — величие.", "*медитирует*" },
        // Carried
        new[] { "Поставьте меня на место!", "Я буду жаловаться!", "КУДА?! КУДА МЕНЯ?!", "ААА! НЕ НАДО!" },
        // Ejected
        new[] { "РАССТАВЬТЕ ПРЕДМЕТЫ ПРАВИЛЬНО", "ПРИВЕТ ЗЕМЛЕ!", "Я БУДУ ЖАЛОВАТЬСЯ!!!", "ХОРОШО-ТО КАК…" },
    };

    // ---------- Сетевые переменные (пишет сервер) ----------
    public readonly NetworkVariable<int> NetState = new((int)PassengerState.Calm,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> NetPanic = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> NetSeat = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> NetPersonality = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString64Bytes> NetName = new(new FixedString64Bytes("Пассажир"),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString64Bytes> NetBubble = new(new FixedString64Bytes(""),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> NetBubbleSeq = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<Vector3> NetPos = new(Vector3.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> NetYaw = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>Кто несёт пассажира (ulong.MaxValue — никто).</summary>
    public readonly NetworkVariable<ulong> CarriedBy = new(ulong.MaxValue,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>Начальная скорость выброса (для анимации падения).</summary>
    public readonly NetworkVariable<Vector3> EjectVelocity = new(Vector3.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ---------- Серверная симуляция ----------
    private float _stress;             // накопленный стресс 0..100
    private float _hp = 60f;           // «здоровье» для драк
    private float _knockTimer;         // нокаут (сервер)
    private float _moveTimer;          // таймер выбора цели движения
    private Vector3 _serverPos;        // авторитетная позиция сервера
    private float _serverYaw;
    private PassengerNPC _fightTarget; // с кем дерётся
    private float _punchCd;            // кулдаун удара
    private float _calmCd;             // кулдаун «успокоить»
    private float _bubbleCd;
    private float _syncTimer;
    private float _ejectTimer;         // до деспавна выброшенного
    private Vector3 _walkTarget;
    private bool _walkingToSeat;       // идёт к креслу при посадке
    private System.Random _rng;
    private int _lastBubbleSeq;

    // ---------- Визуал ----------
    private Transform _body;
    private Transform _legL, _legR, _armL, _armR, _torso, _head;
    private TextMesh _bubble;
    private float _bubbleShowUntil;
    private Vector3 _visualVel;
    private float _animPhase;
    private Vector3 _ejectVel;
    private bool _ejectStarted;

    public PassengerState State => (PassengerState)NetState.Value;
    public string PaxName => NetName.Value.ToString();
    /// <summary>Серверный стресс (для менеджера).</summary>
    public float ServerStress => _stress;
    /// <summary>Нокаутирован ли (сервер).</summary>
    public bool ServerKnockedOut => _knockTimer > 0f;
    public Vector3 SeatPosition => AirplaneController.Instance != null && AirplaneController.Instance.Refs != null
        ? AirplaneController.Instance.Refs.Seats[Mathf.Clamp(NetSeat.Value, 0, 23)].position
        : Vector3.zero;

    private void Awake()
    {
        BuildBody();
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        if (IsServer)
        {
            _rng = new System.Random(GetInstanceID() * 31 + 7);
            NetName.Value = new FixedString64Bytes(Names[NetSeat.Value % Names.Length]);
            _serverPos = NetPos.Value;
        }
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
    }

    // ================= ВИЗУАЛ =================

    /// <summary>Процедурный человечек: торс, голова, ноги, руки; цвета по «сиденью».</summary>
    private void BuildBody()
    {
        var root = new GameObject("Body");
        root.transform.SetParent(transform, false);
        _body = root.transform;

        int seed = Random.Range(0, 999);
        var shirtColors = new[]
        {
            new Color(0.75f, 0.3f, 0.25f), new Color(0.3f, 0.55f, 0.8f), new Color(0.85f, 0.7f, 0.2f),
            new Color(0.45f, 0.7f, 0.4f), new Color(0.6f, 0.4f, 0.7f), new Color(0.8f, 0.5f, 0.6f),
        };
        var shirt = ProceduralAssets.Mat($"Shirt{seed % 6}", shirtColors[seed % 6]);
        var skin = ProceduralAssets.Mat("Skin", new Color(0.87f, 0.7f, 0.55f));
        var pants = ProceduralAssets.Mat("Pants", new Color(0.25f, 0.27f, 0.32f));

        _torso = ProceduralAssets.Box(_body, "Torso", new Vector3(0f, 1.02f, 0f), Vector3.zero,
            new Vector3(0.42f, 0.6f, 0.26f), shirt).transform;
        _head = ProceduralAssets.Sphere(_body, "Head", new Vector3(0f, 1.5f, 0f), 0.19f, skin).transform;

        // Ноги/руки с пивотами у бедра/плеча (для анимации).
        _legL = MakeLimb("LegL", new Vector3(-0.11f, 0.72f, 0f), new Vector3(0.15f, 0.7f, 0.17f), pants);
        _legR = MakeLimb("LegR", new Vector3(0.11f, 0.72f, 0f), new Vector3(0.15f, 0.7f, 0.17f), pants);
        _armL = MakeLimb("ArmL", new Vector3(-0.27f, 1.26f, 0f), new Vector3(0.11f, 0.55f, 0.13f), shirt);
        _armR = MakeLimb("ArmR", new Vector3(0.27f, 1.26f, 0f), new Vector3(0.11f, 0.55f, 0.13f), shirt);

        // Пузырь реплики.
        var bubbleGo = new GameObject("Bubble");
        bubbleGo.transform.SetParent(_body, false);
        bubbleGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        _bubble = bubbleGo.AddComponent<TextMesh>();
        _bubble.fontSize = 26;
        _bubble.characterSize = 0.12f;
        _bubble.anchor = TextAnchor.LowerCenter;
        _bubble.alignment = TextAlignment.Center;
        _bubble.color = new Color(1f, 1f, 0.75f);
        var mr = bubbleGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = ProceduralAssets.BuiltinFont.material;
        bubbleGo.SetActive(false);
    }

    private Transform MakeLimb(string name, Vector3 hipPos, Vector3 size, Material mat)
    {
        var pivot = new GameObject(name + "Pivot");
        pivot.transform.SetParent(_body, false);
        pivot.transform.localPosition = hipPos;
        ProceduralAssets.Box(pivot.transform, name, new Vector3(0f, -size.y * 0.5f, 0f), Vector3.zero, size, mat);
        return pivot.transform;
    }

    // ================= ОБНОВЛЕНИЕ =================

    private void Update()
    {
        if (IsServer) ServerTick(Time.deltaTime);
        UpdateVisual(Time.deltaTime);
    }

    // ================= СЕРВЕР: МАШИНА СОСТОЯНИЙ =================

    private void ServerTick(float dt)
    {
        var session = GameSession.Instance;
        if (session == null) return;

        // Позиция-основа: кресло.
        Vector3 seat = SeatPosition;
        var state = (PassengerState)NetState.Value;

        // ---------- Накопление стресса ----------
        if (state == PassengerState.Calm)
        {
            float stressRate = 0.35f; // базовый бытовой стресс
            stressRate += session.Turbulence.Value * 6f;
            if (session.Engine1Fire.Value || session.Engine2Fire.Value) stressRate += 4f;
            if (session.WindshieldCracks.Value > 0) stressRate += 1.5f;
            if (session.DoorOpen.Value && session.Phase.Value == (int)GamePhase.Flight) stressRate += 2f;
            if (session.SpeedKmh.Value / 3.6f < 40f) stressRate += 3f; // сваливание пугает
            // «Проблемный» груз добавляет нервозности.
            stressRate += GameSession.CargoOptions[Mathf.Clamp(session.CargoId.Value, 0, 2)].trouble * 0.6f;
            // Личность: нервные быстрее паникуют, буйные быстрее злятся, «принявшие» медленнее.
            float pMult = NetPersonality.Value switch
            {
                (int)Personality.Nervous => 1.6f,
                (int)Personality.Rowdy => 1.1f,
                (int)Personality.Drunk => 0.7f,
                _ => 1f,
            };
            _stress += stressRate * pMult * dt;

            if (_stress >= 100f)
            {
                // Нервные и пьяные — в панику; буйные — в агрессию.
                bool rowdy = NetPersonality.Value == (int)Personality.Rowdy && _rng.NextDouble() < 0.6;
                SetState(rowdy ? PassengerState.Aggression : PassengerState.Panic);
                if (rowdy) PassengerManager.Instance?.OnFightStarted(this);
            }
        }

        // ---------- Поведение по состояниям ----------
        switch (state)
        {
            case PassengerState.Calm:
            case PassengerState.Calmed:
            {
                // Идём к креслу (посадка/возврат) и сидим.
                if (_walkingToSeat)
                {
                    MoveTowards(seat, 1.1f, dt);
                    if (Vector3.Distance(_serverPos, seat) < 0.25f)
                    {
                        _walkingToSeat = false;
                        _serverPos = seat;
                    }
                }
                else
                {
                    _serverPos = seat;
                    // Спокойные просят воды.
                    SayRandom(dt, PassengerState.Calm, 0.004f);
                }
                if (state == PassengerState.Calmed) _stress = Mathf.Max(0f, _stress - 4f * dt);
                break;
            }
            case PassengerState.Panic:
            {
                // Бег по салону + крики + заражение соседей.
                _moveTimer -= dt;
                if (_moveTimer <= 0f)
                {
                    _moveTimer = 1.2f + (float)_rng.NextDouble() * 1.5f;
                    // Иногда бежит к двери!
                    _walkTarget = _rng.NextDouble() < 0.3
                        ? new Vector3(-0.9f, 0f, 3.6f)
                        : new Vector3(0f, 0f, Random.Range(-4.5f, 3.2f));
                    _serverYaw = Random.Range(0f, 360f);
                }
                MoveTowards(_walkTarget, 2.4f, dt);
                SayRandom(dt, PassengerState.Panic, 0.05f);

                // Заражение паникой.
                foreach (var other in All)
                {
                    if (other == null || other == this || other.State != PassengerState.Calm) continue;
                    if ((other.transform.position - transform.position).sqrMagnitude < 9f)
                    {
                        other._stress += 14f * dt;
                    }
                }
                // Паника сама не проходит — только разговор/вода.
                NetPanic.Value = Mathf.Clamp01(_stress / 100f);
                break;
            }
            case PassengerState.Aggression:
            {
                // Ищем цель драки.
                if (_fightTarget == null || _fightTarget.State != PassengerState.Aggression)
                {
                    _fightTarget = FindFightTarget();
                }
                if (_fightTarget != null)
                {
                    Vector3 tp = _fightTarget.transform.position;
                    if (Vector3.Distance(_serverPos, tp) > 0.9f)
                    {
                        MoveTowards(tp, 2.0f, dt);
                    }
                    else
                    {
                        // Обмен ударами.
                        _punchCd -= dt;
                        if (_punchCd <= 0f)
                        {
                            _punchCd = 0.8f;
                            _fightTarget.TakeDamage(18f, this);
                            session.HostSfx(transform.position, 1);
                            SayRandom(dt, PassengerState.Aggression, 1f);
                        }
                    }
                }
                else
                {
                    // Ближайший «мирный» тоже вариант.
                    SayRandom(dt, PassengerState.Aggression, 0.02f);
                    if (_moveTimer <= 0f)
                    {
                        _moveTimer = 2f;
                        _walkTarget = new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-4.5f, 3.2f));
                    }
                    _moveTimer -= dt;
                    MoveTowards(_walkTarget, 1.6f, dt);
                    _stress += 1.2f * dt; // без цели злость остывает в панику
                    if (_stress > 150f) SetState(PassengerState.Panic);
                }
                break;
            }
            case PassengerState.Carried:
            {
                // Позицию ведёт клиент несущего; сервер молчит.
                SayRandom(dt, PassengerState.Carried, 0.02f);
                break;
            }
            case PassengerState.Ejected:
            {
                _ejectTimer -= dt;
                if (_ejectTimer <= 0f)
                {
                    var no = GetComponent<NetworkObject>();
                    if (no != null) no.Despawn(true);
                    return;
                }
                break;
            }
        }

        // Нокаут: лежит «звёздочкой» до таймера.
        if (_knockTimer > 0f)
        {
            _knockTimer -= dt;
            if (_knockTimer <= 0f && state != PassengerState.Ejected && state != PassengerState.Carried)
            {
                SetState(PassengerState.Calmed);
                _walkingToSeat = true;
                Say("Ох… что это было…");
            }
        }

        // Синхронизация позиции (10 Гц).
        _syncTimer -= dt;
        if (_syncTimer <= 0f)
        {
            _syncTimer = 0.1f;
            if (state != PassengerState.Carried && state != PassengerState.Ejected)
            {
                NetPos.Value = _serverPos;
                NetYaw.Value = _serverYaw;
            }
        }
    }

    /// <summary>Движение сервера к цели (в плоскости пола салона).</summary>
    private void MoveTowards(Vector3 target, float speed, float dt)
    {
        Vector3 dir = target - _serverPos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Vector3 step = Vector3.ClampMagnitude(dir.normalized * speed * dt, dir.magnitude);
        _serverPos += step;
        _serverPos.x = Mathf.Clamp(_serverPos.x, -1.0f, 1.0f);
        _serverPos.z = Mathf.Clamp(_serverPos.z, -5.6f, 4.4f);
        _serverPos.y = AirplaneBuilder.FloorY + 0.9f; // стоим на полу
        _serverYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
    }

    private PassengerNPC FindFightTarget()
    {
        PassengerNPC best = null;
        float bestD = 999f;
        foreach (var other in All)
        {
            if (other == null || other == this) continue;
            var st = other.State;
            if (st == PassengerState.Ejected) continue;
            float d = Vector3.Distance(other.transform.position, transform.position);
            if (d < bestD) { bestD = d; best = other; }
        }
        return best;
    }

    private void SayRandom(float dt, PassengerState st, float chancePerSecond)
    {
        if (_bubbleCd > 0f) { _bubbleCd -= dt; return; }
        if (_rng != null && _rng.NextDouble() < chancePerSecond * dt * 60f)
        {
            var lines = Bubbles[(int)st];
            Say(lines[_rng.Next(lines.Length)]);
        }
    }

    /// <summary>Показать реплику всем (сервер).</summary>
    private void Say(string text)
    {
        _bubbleCd = 2.5f;
        NetBubble.Value = new FixedString64Bytes(text);
        NetBubbleSeq.Value++;
    }

    private void SetState(PassengerState newState)
    {
        if (IsServer) NetState.Value = (int)newState;
        if (newState == PassengerState.Panic || newState == PassengerState.Aggression)
        {
            SayRandom(0f, newState, 1f);
            PassengerManager.Instance?.OnUnrest(this);
        }
    }

    /// <summary>Урон (драки, удары игрока).</summary>
    public void TakeDamage(float dmg, PassengerNPC from)
    {
        if (!IsServer) return;
        _hp -= dmg;
        if (_hp <= 0f)
        {
            KnockOut("Хрусть… *отключился*");
        }
        else if (State == PassengerState.Calm && from != null)
        {
            // Мирного ударили — он либо паникует, либо даёт сдачи.
            if (NetPersonality.Value == (int)Personality.Rowdy) SetState(PassengerState.Aggression);
            else SetState(PassengerState.Panic);
        }
    }

    /// <summary>Нокаут (сервер): лежит, потом успокаивается.</summary>
    private void KnockOut(string message)
    {
        _knockTimer = 20f;
        _fightTarget = null;
        _walkingToSeat = false;
        NetState.Value = (int)PassengerState.Calmed;
        Say(message);
    }

    /// <summary>Удар игрока (сервер): урон + нокаут.</summary>
    public void PlayerPunch(ulong byClient)
    {
        if (!IsServer) return;
        _hp -= 40f;
        _stress += 10f;
        var session = GameSession.Instance;
        if (session != null) session.HostSfx(transform.position, 1);
        if (_hp <= 0f)
        {
            KnockOut("*нокаут*");
        }
        else if (State == PassengerState.Aggression)
        {
            _stress -= 15f; // приструнили
            if (_stress < 40f) { SetState(PassengerState.Calmed); _walkingToSeat = true; }
        }
        else if (State == PassengerState.Calm || State == PassengerState.Panic)
        {
            SetState(PassengerState.Panic);
        }
    }

    /// <summary>Взять пассажира «на руки» (сервер).</summary>
    public bool TryGrab(ulong byClient)
    {
        if (!IsServer) return false;
        if (CarriedBy.Value != ulong.MaxValue) return false;
        bool grabbable = _knockTimer > 0f || State == PassengerState.Panic || State == PassengerState.Calmed;
        if (!grabbable)
        {
            Say("Не дамся!");
            return false;
        }
        CarriedBy.Value = byClient;
        NetState.Value = (int)PassengerState.Carried;
        return true;
    }

    /// <summary>Отпустить (сервер).</summary>
    public void Drop()
    {
        if (!IsServer) return;
        CarriedBy.Value = ulong.MaxValue;
        _walkingToSeat = true;
        NetState.Value = (int)PassengerState.Calm;
    }

    /// <summary>Отпустить (запрос от игрока).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc(ServerRpcParams rpcParams = default)
    {
        // Отпустить может только тот, кто несёт.
        if (CarriedBy.Value == rpcParams.Receive.SenderClientId) Drop();
    }

    /// <summary>ВЫБРОСИТЬ ЗА БОРТ (сервер). Тёмный юмор, 18+.</summary>
    public void Eject()
    {
        if (!IsServer) return;
        CarriedBy.Value = ulong.MaxValue;
        NetState.Value = (int)PassengerState.Ejected;
        // В мировых координатах самолёт в нуле: дверь слева-спереди, снос к хвосту и вниз.
        _ejectPos = AirplaneController.Instance != null && AirplaneController.Instance.Refs.MainDoor != null
            ? AirplaneController.Instance.Refs.MainDoor.transform.position
            : transform.position;
        NetPos.Value = _ejectPos;
        EjectVelocity.Value = new Vector3(-3.5f, 1.5f, -6f);
        _ejectTimer = 6f;
        var session = GameSession.Instance;
        if (session != null)
        {
            session.StatEjected.Value++;
            session.HostSfx(transform.position, 2);
            session.HostAnnounce(AnnouncementType.PassengerOut);
            if (PassengerManager.Instance != null) PassengerManager.Instance.AddStressAll(25f, "НАС ВЫБРАСЫВАЮТ!!!");
        }
    }

    // ================= КЛИЕНТ: ВИЗУАЛ =================

    private void UpdateVisual(float dt)
    {
        var state = (PassengerState)NetState.Value;

        // Новый пузырь?
        if (NetBubbleSeq.Value != _lastBubbleSeq)
        {
            _lastBubbleSeq = NetBubbleSeq.Value;
            if (_bubble != null)
            {
                _bubble.text = NetBubble.Value.ToString();
                _bubble.gameObject.SetActive(true);
                _bubbleShowUntil = Time.time + 2.6f;
            }
        }
        if (_bubble != null && _bubble.gameObject.activeSelf && Time.time > _bubbleShowUntil)
        {
            _bubble.gameObject.SetActive(false);
        }

        if (_body == null) return;

        switch (state)
        {
            case PassengerState.Carried:
            {
                // Прикрепляемся к несущему игроку.
                var carrier = FindCarrier();
                if (carrier != null)
                {
                    Vector3 f = carrier.transform.forward;
                    transform.position = carrier.transform.position + f * 0.85f - new Vector3(0f, 0.55f, 0f);
                    _body.localRotation = Quaternion.Euler(75f, 0f, 0f); // «под мышкой»
                    FlailArms(dt);
                }
                break;
            }
            case PassengerState.Ejected:
            {
                // Падение: старт у двери, дальше интегрируем скорость с гравитацией.
                if (!_ejectStarted)
                {
                    _ejectStarted = true;
                    transform.position = NetPos.Value;
                    _ejectVel = EjectVelocity.Value;
                }
                _ejectVel.y -= 9.8f * dt;
                transform.position += _ejectVel * dt;
                _body.localRotation = Quaternion.Euler(Time.time * 420f % 360f, Time.time * 300f % 360f, 0f);
                if (_bubble != null && !_bubble.gameObject.activeSelf)
                {
                    _bubble.text = "АААААААА!";
                    _bubble.gameObject.SetActive(true);
                    _bubbleShowUntil = Time.time + 3f;
                }
                break;
            }
            default:
            {
                // Плавное следование сетевой позиции.
                Vector3 target = NetPos.Value;
                if ((target - transform.position).sqrMagnitude > 36f) transform.position = target;
                else transform.position = Vector3.SmoothDamp(transform.position, target, ref _visualVel, 0.15f);
                transform.rotation = Quaternion.Euler(0f, NetYaw.Value, 0f);

                bool sitting = (state == PassengerState.Calm || state == PassengerState.Calmed)
                    && Vector3.Distance(transform.position, SeatPosition) < 0.3f;
                if (sitting)
                {
                    // Сидим: ноги вперёд.
                    _body.localRotation = Quaternion.identity;
                    _legL.localRotation = Quaternion.Euler(-85f, 0f, 0f);
                    _legR.localRotation = Quaternion.Euler(-85f, 0f, 0f);
                    _armL.localRotation = Quaternion.Euler(-20f, 0f, 10f);
                    _armR.localRotation = Quaternion.Euler(-20f, 0f, -10f);
                }
                else
                {
                    // Идём/бежим: покачивание.
                    _body.localRotation = Quaternion.identity;
                    bool panicked = state == PassengerState.Panic;
                    float freq = panicked ? 11f : 6.5f;
                    _animPhase += dt * freq;
                    float swing = Mathf.Sin(_animPhase) * (panicked ? 0.8f : 0.5f);
                    _legL.localRotation = Quaternion.Euler(swing * 45f, 0f, 0f);
                    _legR.localRotation = Quaternion.Euler(-swing * 45f, 0f, 0f);
                    if (panicked)
                    {
                        // Руки вверх, машет.
                        _armL.localRotation = Quaternion.Euler(-160f + swing * 25f, 0f, 15f);
                        _armR.localRotation = Quaternion.Euler(-160f - swing * 25f, 0f, -15f);
                    }
                    else if (state == PassengerState.Aggression)
                    {
                        // Боксёрская стойка.
                        _armL.localRotation = Quaternion.Euler(-70f + swing * 40f, 0f, 25f);
                        _armR.localRotation = Quaternion.Euler(-70f - swing * 40f, 0f, -25f);
                    }
                    else
                    {
                        _armL.localRotation = Quaternion.Euler(-swing * 30f, 0f, 5f);
                        _armR.localRotation = Quaternion.Euler(swing * 30f, 0f, -5f);
                    }
                }
                break;
            }
        }
    }

    private void FlailArms(float dt)
    {
        _animPhase += dt * 14f;
        float s = Mathf.Sin(_animPhase);
        _armL.localRotation = Quaternion.Euler(-140f + s * 40f, 0f, 30f);
        _armR.localRotation = Quaternion.Euler(-140f - s * 40f, 0f, -30f);
    }

    private NetworkPlayer FindCarrier()
    {
        ulong id = CarriedBy.Value;
        if (id == ulong.MaxValue) return null;
        foreach (var p in NetworkPlayer.All)
        {
            if (p != null && p.OwnerClientId == id) return p;
        }
        return null;
    }

    // ================= ИНТЕРАКТИВНОСТЬ =================

    /// <summary>Подсказка при наведении.</summary>
    public string GetInteractionPrompt(PlayerController player)
    {
        if (State == PassengerState.Ejected || State == PassengerState.Carried) return null;
        if (player != null && player.carriedNpc == this) return "Отпустить (E) / Выбросить у двери (ЛКМ)";
        switch (State)
        {
            case PassengerState.Calm: return $"{PaxName}: успокоить (E), схватить (удерживать E)";
            case PassengerState.Panic: return $"{PaxName} ПАНИКУЕТ: успокоить (E), дать воды (F), схватить (удерживать E)";
            case PassengerState.Aggression: return $"{PaxName} БУЙНЫЙ: ударить (ЛКМ), потом нести (удерживать E)";
            case PassengerState.Calmed: return $"{PaxName} спокоен. Молодец.";
            default: return null;
        }
    }

    /// <summary>E — попытка успокоить / взять.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(bool grab, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong sender = rpcParams.Receive.SenderClientId;

        if (grab)
        {
            if (_calmCd > 0f) return;
            _calmCd = 1.2f;
            TryGrab(sender);
            return;
        }

        if (_calmCd > 0f) return;
        _calmCd = 1.2f;

        switch (State)
        {
            case PassengerState.Panic:
            {
                // Шанс успокоить разговором: чем выше паника, тем меньше.
                float chance = Mathf.Lerp(0.65f, 0.15f, _stress / 100f);
                if (_rng != null && _rng.NextDouble() < chance)
                {
                    NetState.Value = (int)PassengerState.Calmed;
                    _stress = 0f;
                    _walkingToSeat = true;
                    Say("Ну ладно… дышим…");
                }
                else
                {
                    _stress += 5f;
                    Say("НЕ ТРОГАЙТЕ МЕНЯ!!!");
                }
                break;
            }
            case PassengerState.Calm:
                Say("Я спокоен. Почти.");
                _stress = Mathf.Max(0f, _stress - 3f);
                break;
            case PassengerState.Aggression:
                Say("САМ ТЫ УСПОКОЙСЯ!");
                break;
        }
    }

    /// <summary>F — напоить водой (сильное успокоение).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void WaterServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (State == PassengerState.Panic || State == PassengerState.Aggression || State == PassengerState.Calm)
        {
            NetState.Value = (int)PassengerState.Calmed;
            _stress = 0f;
            _walkingToSeat = true;
            Say("О… вода! Спасибо!");
        }
    }

    /// <summary>Добавить стресс (сервер, вызывается менеджером).</summary>
    public void ServerAddStress(float amount)
    {
        if (IsServer) _stress += amount;
    }

    /// <summary>Показать фразу от менеджера (сервер).</summary>
    public void SayExternal(string text)
    {
        if (IsServer) Say(text);
    }

    private void LateUpdate()
    {
        // Пузырь всегда лицом к камере.
        if (_bubble != null && _bubble.gameObject.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                _bubble.transform.rotation = Quaternion.LookRotation(
                    _bubble.transform.position - cam.transform.position);
            }
        }
    }
}
