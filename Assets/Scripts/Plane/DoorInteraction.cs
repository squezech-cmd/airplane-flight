// DoorInteraction.cs — двери самолёта: главная левая дверь (посадка/высадка/выброс пассажиров)
// и правый люк выхода на крыло. Состояние главной двери синхронизировано (GameSession.DoorOpen).

using Unity.Netcode;
using UnityEngine;

/// <summary>Главная дверь самолёта.</summary>
public class DoorInteraction : Interactable
{
    /// <summary>true — это главная передняя дверь (не люк на крыло).</summary>
    public bool isMainDoor;

    private Transform _doorPanel;
    private bool _opened;
    private float _anim;

    /// <summary>Визуал двери: проём + створка (поворотная).</summary>
    public void BuildVisual()
    {
        var metalMat = ProceduralAssets.Mat("DoorMetal", ProceduralAssets.Palette.Metal);
        var accentMat = ProceduralAssets.Mat("DoorAccent", ProceduralAssets.Palette.Accent);

        // Рама проёма.
        ProceduralAssets.Box(transform, "FrameTop", new Vector3(0f, 0.95f, 0f), Vector3.zero,
            new Vector3(0.1f, 0.12f, 1.0f), metalMat);
        ProceduralAssets.Box(transform, "FrameBot", new Vector3(0f, -0.95f, 0f), Vector3.zero,
            new Vector3(0.1f, 0.12f, 1.0f), metalMat);

        // Створка: вращается вокруг верхней кромки (эффект «откинуть»).
        var hinge = new GameObject("Hinge");
        hinge.transform.SetParent(transform, false);
        hinge.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        _doorPanel = new GameObject("DoorPanel").transform;
        _doorPanel.SetParent(hinge.transform, false);
        _doorPanel.localPosition = new Vector3(0f, -0.9f, 0f);
        ProceduralAssets.Box(_doorPanel, "Leaf", Vector3.zero, Vector3.zero,
            new Vector3(0.08f, 1.78f, 0.92f), metalMat);
        ProceduralAssets.Box(_doorPanel, "LeafStripe", new Vector3(0f, 0.55f, 0f), Vector3.zero,
            new Vector3(0.09f, 0.22f, 0.9f), accentMat);
        ProceduralAssets.Box(_doorPanel, "Handle", new Vector3(-0.08f, 0.1f, 0.3f), Vector3.zero,
            new Vector3(0.06f, 0.06f, 0.2f), accentMat);
    }

    public override string GetPrompt(PlayerController player)
    {
        bool open = GameSession.Instance != null && GameSession.Instance.DoorOpen.Value;
        if (player != null && player.carriedNpc != null)
        {
            return open ? "ВЫБРОСИТЬ ПАССАЖИРА (ЛКМ)!" : "Открыть дверь, чтобы выбросить (E)";
        }
        return open ? "Закрыть дверь (E)" : "Открыть дверь (E)";
    }

    public override void Interact(PlayerController player)
    {
        var session = GameSession.Instance;
        if (session == null) return;
        // Состояние двери — серверная переменная.
        session.ToggleDoorServerRpc();
        ProceduralAudio.PlayUiClick();
    }

    private void Update()
    {
        var session = GameSession.Instance;
        if (session == null || _doorPanel == null) return;

        bool open = session.DoorOpen.Value;
        if (open != _opened)
        {
            _opened = open;
            // Порыв ветра при открытии в полёте.
            if (open && session.Phase.Value == (int)GamePhase.Flight)
            {
                ProceduralAudio.PlayAt(transform.position, ProceduralAudio.Wind, 0.8f);
            }
        }
        // Анимация створки (поворот вверх-наружу).
        _anim = Mathf.MoveTowards(_anim, _opened ? 1f : 0f, Time.deltaTime * 1.8f);
        _doorPanel.parent.localRotation = Quaternion.Euler(_anim * -80f, 0f, 0f);
    }
}

/// <summary>Люк выхода на крыло (правый борт).</summary>
public class WingHatch : Interactable
{
    private Transform _panel;

    public void BuildVisual()
    {
        var metalMat = ProceduralAssets.Mat("DoorMetal", ProceduralAssets.Palette.Metal);
        var accentMat = ProceduralAssets.Mat("DoorAccent", ProceduralAssets.Palette.Accent);
        ProceduralAssets.Box(transform, "FrameTop", new Vector3(0f, 0.8f, 0f), Vector3.zero,
            new Vector3(0.1f, 0.1f, 0.9f), metalMat);
        var hinge = new GameObject("Hinge");
        hinge.transform.SetParent(transform, false);
        hinge.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        _panel = new GameObject("HatchPanel").transform;
        _panel.SetParent(hinge.transform, false);
        _panel.localPosition = new Vector3(0f, -0.72f, 0f);
        ProceduralAssets.Box(_panel, "Leaf", Vector3.zero, Vector3.zero,
            new Vector3(0.07f, 1.4f, 0.84f), metalMat);
        ProceduralAssets.Box(_panel, "Mark", new Vector3(-0.05f, 0.3f, 0f), Vector3.zero,
            new Vector3(0.04f, 0.3f, 0.5f), accentMat);
    }

    public override string GetPrompt(PlayerController player)
        => "Выйти на крыло (E)";

    public override void Interact(PlayerController player)
    {
        if (player == null) return;
        // Телепорт — позиция локального игрока авторитетна у владельца.
        player.TeleportToWing();
        ProceduralAudio.PlayUiClick();
    }

    private void Update()
    {
        if (_panel == null) return;
        var session = GameSession.Instance;
        // В полёте люк открыт (иначе не выйти), на стоянке — закрыт.
        bool open = session != null && session.Phase.Value >= (int)GamePhase.Takeoff;
        _panel.parent.localRotation = Quaternion.Euler(open ? -75f : 0f, 0f, 0f);
    }
}

/// <summary>Точка возврата с крыла в салон (стоит у корня крыла).</summary>
public class WingEntry : Interactable
{
    public override string GetPrompt(PlayerController player) => "Вернуться в салон (E)";

    public override void Interact(PlayerController player)
    {
        if (player == null) return;
        player.TeleportInside();
        ProceduralAudio.PlayUiClick();
    }
}
