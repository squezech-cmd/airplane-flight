// SettingsMenu.cs — панель настроек: громкость, чувствительность, инверсия Y, качество,
// полный экран, режим голоса. Открывается из главного меню и из паузы. Сохранение мгновенное.

using System;
using UnityEngine;

/// <summary>Панель настроек.</summary>
public class SettingsMenu : MonoBehaviour
{
    private static RectTransform _panel;

    /// <summary>Открыть настройки поверх указанного родителя. onClose вызывается при «Назад».</summary>
    public static void Open(Transform parent, Action onClose)
    {
        if (_panel != null) { _panel.gameObject.SetActive(true); return; }

        _panel = UIFactory.CreateCenterPanel(parent, "SettingsPanel", 740f, 620f);
        BuildContent(_panel, onClose);
    }

    private static void BuildContent(RectTransform panel, Action onClose)
    {
        var title = UIFactory.CreateLabel(panel, "Title", "НАСТРОЙКИ", 40, TextAnchor.UpperCenter,
            Color.white, FontStyle.Bold);
        title.go.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -70f);
        title.go.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -16f);

        float y = -90f;
        Action<RectTransform> place = r =>
        {
            r.anchoredPosition = new Vector2(0f, y);
            y -= 64f;
        };

        // Громкость.
        var vol = UIFactory.CreateStepper(panel, "Vol", "Громкость", VolumeNames(), VolIdx(),
            _ =>
        {
            GameSettings.Current.masterVolume = 0.2f + _ * 0.2f;
            GameSettings.Apply(); GameSettings.Save();
        });
        place(vol.root);

        // Чувствительность мыши.
        var sens = UIFactory.CreateStepper(panel, "Sens", "Чувствительность мыши", SensNames(), SensIdx(),
            _ =>
        {
            GameSettings.Current.mouseSensitivity = 0.4f + _ * 0.4f;
            GameSettings.Save();
        });
        place(sens.root);

        // Инверсия Y.
        var inv = UIFactory.CreateStepper(panel, "Inv", "Инверсия оси Y", OffOn(), 
            GameSettings.Current.invertY ? 1 : 0, _ =>
        {
            GameSettings.Current.invertY = _ == 1;
            GameSettings.Save();
        });
        place(inv.root);

        // Качество.
        var quality = UIFactory.CreateStepper(panel, "Quality", "Качество графики", GameSettings.QualityNames,
            GameSettings.Current.qualityLevel, _ =>
        {
            GameSettings.Current.qualityLevel = _;
            GameSettings.Apply(); GameSettings.Save();
        });
        place(quality.root);

        // Полный экран.
        var fs = UIFactory.CreateStepper(panel, "Fullscreen", "Полный экран", OffOn(),
            GameSettings.Current.fullscreen ? 1 : 0, _ =>
        {
            GameSettings.Current.fullscreen = _ == 1;
            GameSettings.Apply(); GameSettings.Save();
        });
        place(fs.root);

        // Режим голоса.
        var voice = UIFactory.CreateStepper(panel, "Voice", "Голосовой чат", new[] { "Push-to-talk (V)", "Активация голосом" },
            GameSettings.Current.voiceVox ? 1 : 0, _ =>
        {
            GameSettings.Current.voiceVox = _ == 1;
            GameSettings.Save();
        });
        place(voice.root);

        UIFactory.CreateButton(panel, "Back", "НАЗАД", new Vector2(0f, -panel.sizeDelta.y / 2f + 46f),
            new Vector2(320f, 58f), 26, () =>
        {
            ProceduralAudio.PlayUiClick();
            if (onClose != null) onClose();
            else panel.gameObject.SetActive(false);
        });
    }

    private static string[] OffOn() => new[] { "Выкл", "Вкл" };
    private static string[] VolumeNames() => new[] { "Тихо", "20%", "40%", "60%", "80%", "Громко" };
    private static int VolIdx()
    {
        float v = GameSettings.Current.masterVolume;
        return Mathf.Clamp(Mathf.RoundToInt((v - 0.2f) / 0.2f), 0, 5);
    }
    private static string[] SensNames() => new[] { "0.4", "0.8", "1.2", "1.6", "2.0", "2.4", "2.8" };
    private static int SensIdx()
    {
        float s = GameSettings.Current.mouseSensitivity;
        return Mathf.Clamp(Mathf.RoundToInt((s - 0.4f) / 0.4f), 0, 6);
    }
}
