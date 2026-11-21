// SteeringWheel.cs — физический штурвал в кабине. E — сесть за штурвал (запрос серверу),
// R — отпустить. Сам штурвал визуально вращается по вводу пилота (синхронизировано через GameSession).

using UnityEngine;

/// <summary>Интерактивный штурвал самолёта.</summary>
public class SteeringWheel : Interactable
{
    private Transform _visual;
    private float _spinAngle;

    /// <summary>Построить визуал штурвала: кольцо из сегментов + спицы + колонка.</summary>
    public void BuildVisual()
    {
        var root = new GameObject("WheelVisual");
        root.transform.SetParent(transform, false);
        _visual = root.transform;

        var darkMat = ProceduralAssets.Mat("WheelDark", ProceduralAssets.Palette.DarkMetal);
        var accentMat = ProceduralAssets.Mat("WheelAccent", ProceduralAssets.Palette.Accent);

        // Кольцо из 10 сегментов цилиндров по окружности.
        const int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            var seg = ProceduralAssets.Cylinder(_visual, $"Seg{i}",
                new Vector3(Mathf.Cos(a) * 0.24f, Mathf.Sin(a) * 0.24f, 0f),
                new Vector3(0f, 0f, 90f), 0.045f, 0.07f, darkMat);
            seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg) * Quaternion.Euler(90f, 0f, 0f);
        }
        // Спицы («рога»).
        ProceduralAssets.Box(_visual, "SpokeH", Vector3.zero, Vector3.zero, new Vector3(0.46f, 0.05f, 0.05f), darkMat);
        ProceduralAssets.Box(_visual, "SpokeV", Vector3.zero, Vector3.zero, new Vector3(0.05f, 0.46f, 0.05f), darkMat);
        ProceduralAssets.Sphere(_visual, "Hub", Vector3.zero, 0.07f, accentMat);
        // Колонка.
        ProceduralAssets.Cylinder(_visual, "Column", new Vector3(0f, -0.35f, 0.12f), new Vector3(70f, 0f, 0f),
            0.035f, 0.5f, darkMat);
    }

    public override string GetPrompt(PlayerController player)
    {
        var session = GameSession.Instance;
        if (session == null) return null;
        ulong pilot = session.PilotClientId.Value;
        var local = NetworkPlayer.Local;
        if (local == null) return null;
        if (pilot == GameSession.NoPilot) return "Сесть за штурвал (E)";
        if (pilot == local.OwnerClientId) return "Отпустить штурвал (R)";
        return $"Штурвал занят: {GameSession.NameFor(pilot)}";
    }

    public override void Interact(PlayerController player)
    {
        var session = GameSession.Instance;
        var local = NetworkPlayer.Local;
        if (session == null || local == null) return;
        if (session.PilotClientId.Value == GameSession.NoPilot)
        {
            session.RequestPilotServerRpc(true);
            ProceduralAudio.PlayUiClick();
        }
    }

    private void Update()
    {
        if (_visual == null) return;
        var session = GameSession.Instance;
        if (session == null) return;

        // Вращение по крену (вокруг оси колонки) и наклон по тангажу.
        float roll = session.PilotRoll.Value;
        float pitch = session.PilotPitch.Value;
        _spinAngle = roll * 120f;
        _visual.localRotation = Quaternion.Euler(pitch * -18f, 0f, _spinAngle);
    }
}
