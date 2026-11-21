// VoiceChatManager.cs — встроенный голосовой чат: микрофон → μ-law 8 кГц → ServerRpc → ClientRpc →
// проигрывание у удалённых игроков. Push-to-talk (V) или VOX (по уровню сигнала).
// Не требует внешних сервисов и работает «из коробки» в LAN/прятом подключении.

using UnityEngine;

/// <summary>Локальный менеджер голосового чата.</summary>
public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager Instance { get; set; }

    private const int SampleRate = 8000;
    private const int FrameSamples = 800;        // 100 мс на кадр
    private const float FrameSeconds = 0.1f;

    private AudioClip _micClip;
    private int _micLastPos;
    private float _frameTimer;
    private float[] _frameBuf = new float[FrameSamples];
    private bool _micFailed;

    /// <summary>Порог VOX (RMS).</summary>
    private const float VoxThreshold = 0.035f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        var local = NetworkPlayer.Local;
        if (local == null) return;

        bool wantTalk = WantTalk();

        // Индикатор «говорит».
        if (local.Talking.Value != wantTalk)
        {
            local.Talking.Value = wantTalk;
        }

        if (!wantTalk)
        {
            _micLastPos = Microphone.GetPosition(null); // не копим хвосты
            return;
        }

        // Ленивая инициализация микрофона.
        if (_micClip == null && !_micFailed)
        {
            try
            {
                _micClip = Microphone.Start(null, true, 1, SampleRate);
                _micLastPos = 0;
                if (_micClip == null)
                {
                    _micFailed = true;
                    Debug.LogWarning("[VoiceChat] Микрофон недоступен.");
                }
            }
            catch (System.Exception e)
            {
                _micFailed = true;
                Debug.LogWarning($"[VoiceChat] Микрофон недоступен: {e.Message}");
            }
        }
        if (_micClip == null) return;

        // Читаем новые сэмплы кадрами по 100 мс.
        _frameTimer += Time.deltaTime;
        if (_frameTimer < FrameSeconds) return;
        _frameTimer = 0f;

        int pos = Microphone.GetPosition(null);
        int available = pos - _micLastPos;
        if (available < 0) available += _micClip.samples; // кольцевой буфер
        if (available < FrameSamples) return;

        int readPos = _micLastPos;
        _micLastPos = (readPos + FrameSamples) % _micClip.samples;

        if (!_micClip.GetData(_frameBuf, readPos)) return;

        // μ-law сжатие кадра.
        var frame = new byte[FrameSamples];
        for (int i = 0; i < FrameSamples; i++)
        {
            frame[i] = FloatToMuLaw(_frameBuf[i]);
        }
        local.SendVoiceServerRpc(frame);
    }

    /// <summary>Нажата ли «передача» (PTT или VOX).</summary>
    private bool WantTalk()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return false;

        if (GameSettings.Current.voiceVox)
        {
            // VOX: говорим, если микрофон громкий (упрощённо проверяем каждый кадр
            // по последнему буферу — инициализируем микрофон заранее).
            EnsureMicForVox();
            if (_micClip != null)
            {
                int pos = Microphone.GetPosition(null);
                int n = Mathf.Min(256, _micClip.samples);
                var buf = new float[n];
                int readPos = (pos - n + _micClip.samples * 2) % _micClip.samples;
                if (_micClip.GetData(buf, readPos))
                {
                    float sum = 0f;
                    for (int i = 0; i < n; i++) sum += buf[i] * buf[i];
                    float rms = Mathf.Sqrt(sum / n);
                    return rms > VoxThreshold;
                }
            }
            return false;
        }

        return kb.vKey.isPressed; // push-to-talk
    }

    private void EnsureMicForVox()
    {
        if (_micClip == null && !_micFailed)
        {
            try
            {
                _micClip = Microphone.Start(null, true, 1, SampleRate);
                _micLastPos = 0;
                if (_micClip == null) _micFailed = true;
            }
            catch { _micFailed = true; }
        }
    }

    // ================= μ-law кодек (G.711) =================

    private const int MuBias = 132;      // BIAS
    private const int MuClip = 32635;    // CLIP

    /// <summary>Кодирование float (-1..1) в μ-law байт.</summary>
    public static byte FloatToMuLaw(float sample)
    {
        int pcm = Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), -MuClip, MuClip);
        int sign = pcm < 0 ? 0x80 : 0x00;
        if (sign != 0) pcm = -pcm;
        pcm += MuBias;

        // Ищем экспоненту: старший значащий бит в диапазоне 0x80..0x4000.
        int exponent = 0;
        int mask = 0x4000;
        for (int i = 0; i < 8; i++)
        {
            if ((pcm & mask) != 0) { exponent = 7 - i; break; }
            mask >>= 1;
        }
        int mantissa = (pcm >> (exponent + 3)) & 0x0F;
        int compressed = ~(sign | (exponent << 4) | mantissa);
        return (byte)(compressed & 0xFF);
    }

    /// <summary>Декодирование μ-law байта во float (-1..1).</summary>
    public static float MuLawToFloat(byte muLaw)
    {
        int ulaw = ~muLaw & 0xFF; // инверсия всех битов (стандарт G.711)
        int sign = ulaw & 0x80;
        int exponent = (ulaw >> 4) & 0x07;
        int mantissa = ulaw & 0x0F;
        int sample = (((mantissa << 3) + MuBias) << exponent) - MuBias;
        int value = sign != 0 ? -sample : sample;
        return Mathf.Clamp(value / 32768f, -1f, 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        try { if (Microphone.IsRecording(null)) Microphone.End(null); } catch { }
    }
}
