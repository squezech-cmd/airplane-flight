// NetworkPlayer.cs — сетевой игрок. Живёт на префабе Player (NetworkObject + этот скрипт
// добавляются мастером настройки проекта). Владелец двигает локально и пишет NetworkVariable,
// остальные читают и интерполируют. Здесь же: голосовой чат (RPC), удары, пилотирование.

using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Сетевой игрок рейса.</summary>
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>Локальный игрок (может быть null в меню).</summary>
    public static NetworkPlayer Local { get; private set; }

    /// <summary>Все игроки текущей сессии.</summary>
    public static readonly List<NetworkPlayer> All = new List<NetworkPlayer>();

    // ---------- Синхронизируемое состояние ----------
    public readonly NetworkVariable<FixedString64Bytes> PlayerName = new(new FixedString64Bytes("Экипаж"));
    public readonly NetworkVariable<Vector3> NetPos = new(Vector3.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<float> NetYaw = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> Talking = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> HasWater = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>Текущий ввод пилота (заполняет PlayerController, отправка 20 Гц).</summary>
    public Vector3 pilotInput;   // x=roll, y=pitch, z=yaw
    public float pilotThrottle;  // 0..1
    public bool pilotInputDirty;

    private AudioSource _voiceSource;     // голос удалённого игрока
    private PlayerController _controller; // только у владельца
    private Transform _remoteBody;        // только у чужих
    private TextMesh _nameTag;            // только у чужих
    private float _sendTimer;
    private Vector3 _posVel;

    private void Awake()
    {
        _voiceSource = gameObject.AddComponent<AudioSource>();
        _voiceSource.spatialBlend = 1f;
        _voiceSource.minDistance = 1.5f;
        _voiceSource.maxDistance = 25f;
        _voiceSource.rolloffMode = AudioRolloffMode.Linear;
        _voiceSource.playOnAwake = false;
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);

        if (IsOwner)
        {
            Local = this;
            // Локальный игрок: контроллер и CharacterController.
            gameObject.AddComponent<CharacterController>();
            _controller = gameObject.AddComponent<PlayerController>();
        }
        else
        {
            BuildRemoteBody();
        }

        if (IsServer)
        {
            PlayerName.Value = new FixedString64Bytes($"Экипаж {OwnerClientId + 1}");
        }
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        if (Local == this) Local = null;
        if (_controller != null) Destroy(_controller);
    }

    /// <summary>Упрощённое тело чужого игрока + табличка с именем.</summary>
    private void BuildRemoteBody()
    {
        var root = new GameObject("RemoteBody");
        root.transform.SetParent(transform, false);
        _remoteBody = root.transform;

        var bodyMat = ProceduralAssets.Mat("CrewBody", new Color(0.18f, 0.2f, 0.24f));
        var headMat = ProceduralAssets.Mat("CrewHead", new Color(0.85f, 0.68f, 0.55f));
        var accent = ProceduralAssets.Mat("CrewAccent", ProceduralAssets.Palette.Accent);

        var torso = ProceduralAssets.Box(root.transform, "Torso", new Vector3(0f, 1.05f, 0f), Vector3.zero,
            new Vector3(0.46f, 0.62f, 0.28f), bodyMat);
        ProceduralAssets.Box(root.transform, "Stripe", new Vector3(0f, 0.86f, 0f), Vector3.zero,
            new Vector3(0.48f, 0.07f, 0.3f), accent);
        ProceduralAssets.Sphere(root.transform, "Head", new Vector3(0f, 1.55f, 0f), 0.19f, headMat);
        ProceduralAssets.Box(root.transform, "LegL", new Vector3(-0.12f, 0.36f, 0f), Vector3.zero,
            new Vector3(0.16f, 0.72f, 0.18f), bodyMat);
        ProceduralAssets.Box(root.transform, "LegR", new Vector3(0.12f, 0.36f, 0f), Vector3.zero,
            new Vector3(0.16f, 0.72f, 0.18f), bodyMat);
        ProceduralAssets.Box(root.transform, "ArmL", new Vector3(-0.31f, 1.05f, 0f), Vector3.zero,
            new Vector3(0.12f, 0.55f, 0.16f), bodyMat);
        ProceduralAssets.Box(root.transform, "ArmR", new Vector3(0.31f, 1.05f, 0f), Vector3.zero,
            new Vector3(0.12f, 0.55f, 0.16f), bodyMat);

        var tagGo = new GameObject("NameTag");
        tagGo.transform.SetParent(root.transform, false);
        tagGo.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        _nameTag = tagGo.AddComponent<TextMesh>();
        _nameTag.text = PlayerName.Value.ToString();
        _nameTag.fontSize = 30;
        _nameTag.characterSize = 0.16f;
        _nameTag.anchor = TextAnchor.MiddleCenter;
        _nameTag.alignment = TextAlignment.Center;
        _nameTag.color = new Color(1f, 0.75f, 0.3f);
        var tagMr = tagGo.AddComponent<MeshRenderer>();
        tagMr.sharedMaterial = ProceduralAssets.BuiltinFont.material;
    }

    private void Update()
    {
        // --- Владелец: публикуем позицию и отправляем ввод пилота.
        if (IsOwner)
        {
            NetPos.Value = transform.position;
            NetYaw.Value = _controller != null ? _controller.yaw : 0f;

            if (GameSession.Instance != null &&
                GameSession.Instance.PilotClientId.Value == OwnerClientId)
            {
                _sendTimer += Time.deltaTime;
                if (_sendTimer >= 0.05f && pilotInputDirty) // 20 Гц
                {
                    _sendTimer = 0f;
                    pilotInputDirty = false;
                    SetPilotInputServerRpc(pilotInput.y, pilotInput.x, pilotInput.z, pilotThrottle);
                }
            }
            return;
        }

        // --- Чужой игрок: плавно догоняем сетевую позицию.
        Vector3 target = NetPos.Value;
        if ((target - transform.position).sqrMagnitude > 100f)
        {
            transform.position = target; // телепорт при большом отрыве
            _posVel = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _posVel, 0.12f);
        }
        transform.rotation = Quaternion.Euler(0f, NetYaw.Value, 0f);

        if (_nameTag != null)
        {
            _nameTag.text = PlayerName.Value.ToString();
            // Табличка «говорит» подсвечивается.
            _nameTag.color = Talking.Value ? new Color(0.35f, 1f, 0.4f) : new Color(1f, 0.75f, 0.3f);
        }
    }

    // ================= Действия игрока (клиент → сервер) =================

    /// <summary>Удар кулаком: сервер ищет пассажира на линии удара.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendPunchServerRpc(Vector3 origin, Vector3 direction)
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.HandlePunch(origin, direction, OwnerClientId);
        }
    }

    /// <summary>Взять/положить стакан воды (визуально несёт в руке).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetHasWaterServerRpc(bool has)
    {
        HasWater.Value = has;
    }

    /// <summary>Выбросить несомого пассажира (вызывает PassengerManager).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendThrowCarriedServerRpc()
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.HandleThrowBy(OwnerClientId);
        }
    }

    // ================= Голосовой чат =================

    /// <summary>Кадр голоса (μ-law 8 кГц) от владельца → сервер.</summary>
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void SendVoiceServerRpc(byte[] frame)
    {
        if (frame == null || frame.Length == 0) return;
        // Хост слышит сразу.
        PlayVoiceFrame(frame, OwnerClientId);
        // Пересылаем остальным (кроме отправителя).
        VoiceFrameClientRpc(frame, OwnerClientId);
    }

    [ClientRpc]
    private void VoiceFrameClientRpc(byte[] frame, ulong senderId)
    {
        if (IsOwner) return; // свой голос не играем
        PlayVoiceFrame(frame, senderId);
    }

    private void PlayVoiceFrame(byte[] frame, ulong senderId)
    {
        if (senderId == OwnerClientId) return;
        // Ищем отправителя среди игроков и играем его голосом.
        foreach (var p in All)
        {
            if (p != null && p.OwnerClientId == senderId)
            {
                p.PlayVoice(frame);
                return;
            }
        }
    }

    /// <summary>Проиграть кадр голоса у этого игрока.</summary>
    public void PlayVoice(byte[] muLawFrame)
    {
        if (muLawFrame == null || muLawFrame.Length == 0) return;
        const int rate = 8000;
        var samples = new float[muLawFrame.Length];
        for (int i = 0; i < muLawFrame.Length; i++)
        {
            samples[i] = VoiceChatManager.MuLawToFloat(muLawFrame[i]) * GameSettings.Current.voiceVolume;
        }
        var clip = AudioClip.Create("voice", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        _voiceSource.spatialBlend = 1f;
        _voiceSource.PlayOneShot(clip);
    }
}
