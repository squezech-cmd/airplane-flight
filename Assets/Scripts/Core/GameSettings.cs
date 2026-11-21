// GameSettings.cs — глобальные настройки игры: хранение, загрузка и сохранение (JSON).
// Автор фан-переиздания: сообщество. Игра 18+, создана в развлекательных целях.

using System;
using System.IO;
using UnityEngine;

/// <summary>Пользовательские настройки. Сохраняются в persistentDataPath/settings.json.</summary>
public static class GameSettings
{
    [Serializable]
    public class Data
    {
        // Звук
        public float masterVolume = 0.8f;   // общая громкость (0..1)
        public float voiceVolume = 0.9f;    // громкость голосового чата
        public float sfxVolume = 1.0f;      // громкость эффектов

        // Управление
        public float mouseSensitivity = 1.0f; // чувствительность мыши (0.2..3)
        public bool invertY = false;          // инверсия оси Y

        // Графика
        public int qualityLevel = 1;        // 0 — низкое, 1 — среднее, 2 — высокое
        public bool fullscreen = true;      // полный экран

        // Голосовой чат
        public bool voiceVox = false;       // false — push-to-talk (V), true — активация по голосу (VOX)

        // 18+ подтверждение
        public bool ageConfirmed = false;   // подтверждено, что игроку 18+
    }

    public static Data Current = new Data();

    private static string SettingsPath => Path.Combine(Application.persistentDataPath, "dear_passengers_settings.json");

    /// <summary>Загрузка настроек с диска (при отсутствии — значения по умолчанию).</summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var loaded = JsonUtility.FromJson<Data>(json);
                if (loaded != null) Current = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Не удалось загрузить настройки: {e.Message}");
        }
        Apply();
    }

    /// <summary>Сохранение настроек на диск.</summary>
    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(Current, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Не удалось сохранить настройки: {e.Message}");
        }
    }

    /// <summary>Применение настроек к движку (звук, экран, качество).</summary>
    public static void Apply()
    {
        AudioListener.volume = Mathf.Clamp01(Current.masterVolume);
        Screen.fullScreen = Current.fullscreen;
        QualitySettings.SetQualityLevel(Mathf.Clamp(Current.qualityLevel, 0, 2), true);
        // Дальность тумана и теней настраивается GameWorld/качеством рендера при создании сцены.
    }

    /// <summary>Имена уровней качества для UI.</summary>
    public static string[] QualityNames => new[] { "Низкое", "Среднее", "Высокое" };
}
