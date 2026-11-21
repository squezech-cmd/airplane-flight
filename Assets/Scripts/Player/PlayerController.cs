// PlayerController.cs — локальное управление игроком от первого лица: ходьба, бег, прыжок,
// присед, хождение по крылу против ветра, взаимодействие (E/F/ЛКМ), пилотирование за штурвалом,
// тряска от турбулентности, пауза. Добавляется только владельцу NetworkPlayer.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Локальный контроллер игрока.</summary>
public class PlayerController : MonoBehaviour
{
    public static PlayerController Local { get; private set; }

    // ---------- Ссылки ----------
    private CharacterController _cc;
    private Camera _cam;
    private Transform _camHolder;

    // ---------- Состояние ----------
    [HideInInspector] public float yaw = 180f;
    [HideInInspector] public float pitch;
    private float _verticalVel;
    private bool _grounded = true;
    private bool _wasOnWing;

    /// <summary>true, если игрок сейчас на крыле.</summary>
    public bool isOnWing { get; private set; }
    /// <summary>Есть ли стакан воды в руке.</summary>
    public bool hasWater { get; private set; }
    /// <summary>Несомый пассажир (авто-детект по NetworkVariable).</summary>
    public PassengerNPC carriedNpc { get; private set; }

    private bool _piloting;
    private float _pilotThrottle = 0.4f;
    private Vector2 _yoke;          // x=крен, y=тангаж (-1..1)
    private float _pilotYawInput;

    private float _holdTimer = -1f;
    private PassengerNPC _holdNpc;
    private float _punchCd;
    private Vector3 _camShake;

    private bool _sceneAllowed = true;

    // ---------- Жизненный цикл ----------

    private void Start()
    {
        Local = this;
        _cc = GetComponent<CharacterController>();
        if (_cc != null)
        {
            _cc.height = 1.8f;
            _cc.radius = 0.34f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
        }

        // Камера.
        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        _cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        _camHolder = camGo.transform;
        _cam.nearClipPlane = 0.08f;
        if (GameWorld.Instance != null) GameWorld.Instance.ConfigureCamera(_cam);
        // URP: включаем пост-обработку на камере.
        SetupPostProcessing();

        yaw = NetworkLauncher.SpawnYaw != 0f ? NetworkLauncher.SpawnYaw : yaw;

        SceneManager.sceneLoaded += OnSceneChanged;
        OnSceneChanged(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SetCursorLocked(true);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneChanged;
        if (Local == this) Local = null;
    }

    /// <summary>URP пост-обработка (bloom/vignette) — глобальный Volume.</summary>
    private void SetupPostProcessing()
    {
        try
        {
            var volumeGo = new GameObject("GlobalVolume");
            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.55f);
            bloom.threshold.Override(0.9f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(6f);
            color.contrast.Override(4f);
            var volume = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            var data = _cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerController] Пост-обработка недоступна: {e.Message}");
        }
    }

    private void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        _sceneAllowed = scene.name == GameBootstrap.SceneLobby
                     || scene.name == GameBootstrap.SceneAirplane
                     || scene.name == GameBootstrap.SceneFlight;
        gameObject.SetActive(_sceneAllowed);
        if (_sceneAllowed) SetCursorLocked(true);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // ================= UPDATE =================

    private void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        // Пауза.
        if (kb.escapeKey.wasPressedThisFrame && HUD.Instance != null)
        {
            bool nowPaused = HUD.Instance.TogglePause();
            SetCursorLocked(!nowPaused);
            return;
        }
        if (HUD.Instance != null && HUD.Instance.IsPaused)
        {
            // В паузе управление заморожено.
            return;
        }

        // Пилотируем?
        var session = GameSession.Instance;
        var np = NetworkPlayer.Local;
        _piloting = session != null && np != null && session.PilotClientId.Value == np.OwnerClientId;

        if (_piloting) PilotUpdate(kb, mouse);
        else WalkUpdate(kb, mouse);

        UpdateCarried(np);
        InteractionsUpdate(kb, mouse, np);
        UpdateCamera();
    }

    // ================= ПЕШКОМ =================

    private void WalkUpdate(Keyboard kb, Mouse mouse)
    {
        // После выхода из-за штурвала включаем коллайдер обратно.
        if (_cc != null && !_cc.enabled)
        {
            _cc.enabled = true;
            transform.position = new Vector3(0.9f, 0.05f, 5.0f); // у входа в кабину
        }

        // Обзор.
        LookUpdate(mouse, free: true);

        // Движение.
        Vector2 moveInput = Vector2.zero;
        if (kb.wKey.isPressed) moveInput.y += 1f;
        if (kb.sKey.isPressed) moveInput.y -= 1f;
        if (kb.dKey.isPressed) moveInput.x += 1f;
        if (kb.aKey.isPressed) moveInput.x -= 1f;

        bool sprint = kb.leftShiftKey.isPressed;
        bool crouch = kb.cKey.isPressed;
        float speed = sprint ? 5.2f : 3.2f;
        if (carriedNpc != null) speed *= 0.65f; // с пассажиром медленнее

        var rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 move = rot * new Vector3(moveInput.x, 0f, moveInput.y) * speed;

        // Ветер на крыле.
        isOnWing = IsInWingArea(transform.position);
        if (isOnWing)
        {
            float wind = 3.4f * (crouch ? 0.35f : 1f);
            var session = GameSession.Instance;
            if (session != null) wind *= Mathf.Lerp(0.6f, 1.3f, session.SpeedKmh.Value / 400f);
            move += Vector3.back * wind; // сносит к хвосту
            move += Vector3.left * Mathf.PerlinNoise(Time.time * 0.7f, 4f) * 0.8f;
            _wasOnWing = true;
        }

        // Гравитация/прыжок.
        if (_grounded && kb.spaceKey.wasPressedThisFrame) _verticalVel = 5.6f;
        _verticalVel -= 16f * Time.deltaTime;

        if (_cc != null)
        {
            var flags = _cc.Move((move + new Vector3(0f, _verticalVel, 0f)) * Time.deltaTime);
            _grounded = (flags & CollisionFlags.Below) != 0 ||
                        transform.position.y < AirplaneBuilder.FloorY + 1.0f;
            if (_grounded && _verticalVel < 0f) _verticalVel = -0.5f;
        }

        // Сдуло с крыла / упал за борт — втягиваем обратно (комедия, не смерть).
        if (transform.position.y < -6f)
        {
            TeleportInside();
            if (HUD.Instance != null) HUD.Instance.Subtitle("Вас сдуло с крыла! Каким-то чудом вас втянуло обратно в салон.", 4f);
            _wasOnWing = false;
        }
    }

    private void LookUpdate(Mouse mouse, bool free)
    {
        if (mouse == null) return;
        Vector2 delta = mouse.delta.ReadValue() * 0.08f * GameSettings.Current.mouseSensitivity;
        if (!free) return;
        if (GameSettings.Current.invertY) delta.y = -delta.y;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
    }

    private bool IsInWingArea(Vector3 p)
        => p.x > 1.2f && p.x < 7.8f && p.z > -0.6f && p.z < 2.0f && p.y > -0.2f && p.y < 2.4f;

    // ================= ПИЛОТИРОВАНИЕ =================

    private void PilotUpdate(Keyboard kb, Mouse mouse)
    {
        var session = GameSession.Instance;
        var refs = AirplaneController.Instance != null ? AirplaneController.Instance.Refs : null;

        // Штурвал мышью.
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * 0.0035f * GameSettings.Current.mouseSensitivity;
            if (GameSettings.Current.invertY) delta.y = -delta.y;
            _yoke.x = Mathf.Clamp(_yoke.x + delta.x, -1f, 1f); // крен
            _yoke.y = Mathf.Clamp(_yoke.y - delta.y, -1f, 1f); // тангаж
        }
        // Дублирование стрелками (доступность).
        if (kb.upArrowKey.isPressed) _yoke.y = Mathf.Clamp(_yoke.y + 1.2f * Time.deltaTime, -1f, 1f);
        if (kb.downArrowKey.isPressed) _yoke.y = Mathf.Clamp(_yoke.y - 1.2f * Time.deltaTime, -1f, 1f);
        if (kb.leftArrowKey.isPressed) _yoke.x = Mathf.Clamp(_yoke.x - 1.2f * Time.deltaTime, -1f, 1f);
        if (kb.rightArrowKey.isPressed) _yoke.x = Mathf.Clamp(_yoke.x + 1.2f * Time.deltaTime, -1f, 1f);

        // Тяга.
        if (kb.wKey.isPressed) _pilotThrottle = Mathf.Clamp01(_pilotThrottle + 0.5f * Time.deltaTime);
        if (kb.sKey.isPressed) _pilotThrottle = Mathf.Clamp01(_pilotThrottle - 0.5f * Time.deltaTime);
        // Рыскание.
        _pilotYawInput = 0f;
        if (kb.aKey.isPressed) _pilotYawInput = -1f;
        if (kb.dKey.isPressed) _pilotYawInput = 1f;
        // Автопилот: выровнять.
        if (kb.tKey.wasPressedThisFrame)
        {
            _yoke = Vector2.zero;
            _pilotYawInput = 0f;
            if (HUD.Instance != null) HUD.Instance.Subtitle("Автопилот: выравниваю.", 2.5f);
        }
        // Отпустить штурвал.
        if (kb.rKey.wasPressedThisFrame)
        {
            if (session != null) session.RequestPilotServerRpc(false);
            _yoke = Vector2.zero;
        }

        // Отправка ввода на сервер.
        var np = NetworkPlayer.Local;
        if (np != null)
        {
            np.pilotInput = new Vector3(_yoke.x, _yoke.y, _pilotYawInput);
            np.pilotThrottle = _pilotThrottle;
            np.pilotInputDirty = true;
        }

        // Игрок «сидит» в кресле пилота.
        if (refs != null && refs.PilotSeat != null)
        {
            Vector3 seat = refs.PilotSeat.position + new Vector3(0f, 0.75f, 0f);
            if (_cc != null) _cc.enabled = false;
            transform.position = seat - new Vector3(0f, 1.62f, 0f); // камера на уровне глаз
        }
    }

    // ================= ИНТЕРАКЦИИ =================

    private void InteractionsUpdate(Keyboard kb, Mouse mouse, NetworkPlayer np)
    {
        if (_cam == null) return;

        // --- Поиск пассажира рядом (без физики — по дистанции и углу).
        PassengerNPC nearNpc = null;
        float bestNpc = 2.8f;
        Vector3 eye = _cam.transform.position;
        Vector3 fwd = _cam.transform.forward;
        if (!_piloting)
        {
            foreach (var npc in PassengerNPC.All)
            {
                if (npc == null || npc.State == PassengerState.Ejected) continue;
                Vector3 to = npc.transform.position + Vector3.up * 1.1f - eye;
                float dist = to.magnitude;
                if (dist > 2.8f) continue;
                if (Vector3.Dot(to.normalized, fwd) < 0.55f) continue;
                if (dist < bestNpc) { bestNpc = dist; nearNpc = npc; }
            }
        }

        // --- Поиск интерактивного объекта (реестр + дистанция/угол — без физики).
        Interactable world = null;
        float worldDist = 999f;
        if (!_piloting)
        {
            foreach (var inter in Interactable.All)
            {
                if (inter == null || !inter.CanInteract(this)) continue;
                Vector3 to = inter.transform.position - eye;
                float dist = to.magnitude;
                if (dist > inter.maxDistance) continue;
                if (Vector3.Dot(to.normalized, fwd) < 0.45f) continue;
                if (dist < worldDist) { worldDist = dist; world = inter; }
            }
        }

        // --- Подсказка.
        string prompt = null;
        if (_piloting)
        {
            prompt = "ШТУРВАЛ: мышь — крен/тангаж, W/S — тяга, A/D — руль, T — выровнять, R — отпустить";
        }
        else if (nearNpc != null && (world == null || bestNpc < worldDist))
        {
            prompt = nearNpc.GetInteractionPrompt(this);
        }
        else if (world != null)
        {
            prompt = world.GetPrompt(this);
        }
        if (HUD.Instance != null) HUD.Instance.SetPrompt(prompt);

        // --- E: интеракт / удержание (схватить пассажира).
        if (!_piloting)
        {
            bool ePressed = kb.eKey.wasPressedThisFrame;
            bool eHeld = kb.eKey.isPressed;

            if (ePressed)
            {
                if (nearNpc != null && (world == null || bestNpc < worldDist))
                {
                    if (carriedNpc == nearNpc)
                    {
                        nearNpc.DropServerRpc(); // отпустить
                    }
                    else
                    {
                        nearNpc.InteractServerRpc(false); // успокоить
                        _holdTimer = 0f;
                        _holdNpc = nearNpc;
                    }
                }
                else if (world != null)
                {
                    world.Interact(this);
                }
            }

            // Удержание E на пассажире = схватить.
            if (eHeld && _holdNpc != null && _holdTimer >= 0f && carriedNpc == null)
            {
                _holdTimer += Time.deltaTime;
                if (HUD.Instance != null) HUD.Instance.SetHoldProgress(_holdTimer / 0.9f);
                if (_holdTimer >= 0.9f)
                {
                    _holdNpc.InteractServerRpc(true); // просим «схватить»
                    _holdTimer = -1f;
                    _holdNpc = null;
                    if (HUD.Instance != null) HUD.Instance.SetHoldProgress(0f);
                }
            }
            else
            {
                if (HUD.Instance != null && _holdTimer >= 0f) HUD.Instance.SetHoldProgress(0f);
                if (!eHeld) { _holdTimer = -1f; _holdNpc = null; }
            }

            // --- F: вода/вторичное действие.
            if (kb.fKey.wasPressedThisFrame)
            {
                if (nearNpc != null && hasWater)
                {
                    nearNpc.WaterServerRpc();
                    SetHasWater(false);
                }
                else if (world != null)
                {
                    world.SecondaryInteract(this);
                }
            }

            // --- ЛКМ: удар или выброс.
            _punchCd -= Time.deltaTime;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && _punchCd <= 0f)
            {
                _punchCd = 0.45f;
                if (carriedNpc != null)
                {
                    // Выброс у открытой двери.
                    var session = GameSession.Instance;
                    var refs = AirplaneController.Instance != null ? AirplaneController.Instance.Refs : null;
                    bool doorOpen = session != null && session.DoorOpen.Value;
                    bool nearDoor = refs != null && refs.MainDoor != null &&
                        Vector3.Distance(transform.position, refs.MainDoor.transform.position) < 3.2f;
                    if (doorOpen && nearDoor)
                    {
                        np?.SendThrowCarriedServerRpc();
                    }
                    else if (HUD.Instance != null)
                    {
                        HUD.Instance.Subtitle(doorOpen ? "Подойдите к двери, чтобы выбросить." :
                            "Сначала откройте дверь (E у двери). И вы вообще уверены?.. 18+", 3f);
                    }
                }
                else
                {
                    // Обычный удар.
                    np?.SendPunchServerRpc(_cam.transform.position, _cam.transform.forward);
                    _camShake += new Vector3(-0.06f, 0.03f, 0f);
                    ProceduralAudio.PlayAt(_cam.transform.position + fwd * 1.5f, ProceduralAudio.Punch, 0.8f);
                }
            }
        }

        // --- Y: старт фазы (хост, на стоянке).
        if (kb.yKey.wasPressedThisFrame && NetworkLauncher.IsServer)
        {
            var session = GameSession.Instance;
            if (session != null && session.Phase.Value == (int)GamePhase.Boarding)
            {
                session.ConfirmTakeoffServerRpc();
            }
        }
    }

    /// <summary>Авто-определение несомого пассажира.</summary>
    private void UpdateCarried(NetworkPlayer np)
    {
        carriedNpc = null;
        if (np == null) return;
        foreach (var npc in PassengerNPC.All)
        {
            if (npc != null && npc.CarriedBy.Value == np.OwnerClientId)
            {
                carriedNpc = npc;
                break;
            }
        }
    }

    // ================= КАМЕРА =================

    private void UpdateCamera()
    {
        if (_camHolder == null) return;

        if (_piloting)
        {
            // В кабине: лёгкий наклон за штурвалом.
            _camHolder.localRotation = Quaternion.Euler(
                _yoke.y * 7f, 0f, -_yoke.x * 8f);
        }
        else
        {
            _camHolder.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Тряска от турбулентности/удара.
        var session = GameSession.Instance;
        float turb = session != null ? session.Turbulence.Value : 0f;
        _camShake = Vector3.MoveTowards(_camShake, Random.insideUnitSphere * turb * 0.05f, Time.deltaTime * 2f);
        _camHolder.localPosition = new Vector3(0f, 1.62f, 0f) + _camShake;
    }

    // ================= Телепорты/предметы =================

    /// <summary>Выйти на крыло (через люк).</summary>
    public void TeleportToWing()
    {
        transform.position = new Vector3(4.3f, 0.75f, 0.7f);
        _verticalVel = 0f;
        yaw = 90f; // смотрим вдоль крыла
    }

    /// <summary>Вернуться в салон.</summary>
    public void TeleportInside()
    {
        transform.position = new Vector3(0.9f, 0.05f, 0.7f);
        _verticalVel = 0f;
        yaw = 180f;
        if (_cc != null) _cc.enabled = true;
    }

    /// <summary>Взять/убрать стакан воды.</summary>
    public void SetHasWater(bool value)
    {
        hasWater = value;
        NetworkPlayer.Local?.SetHasWaterServerRpc(value);
    }
}
