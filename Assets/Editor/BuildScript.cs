// BuildScript.cs — сборка Windows x64 через BuildPipeline.BuildPlayer.
// Используется двумя способами:
//   1) Локально: меню "Dear Passengers/Build Windows x64" или статический вызов BuildWindows().
//   2) В CI (game-ci/unity-builder@v4): buildMethod = BuildScript.Build — метод читает
//      параметры -customBuildPath / -customBuildTarget / -customBuildName, которые
//      передаёт game-ci CLI.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DearPassengers.EditorTools
{
    /// <summary>Скрипт сборки.</summary>
    public static class BuildScript
    {
        private const string MenuPath = "Dear Passengers/Build Windows x64";

        [MenuItem(MenuPath, priority = 2000)]
        public static void BuildWindowsMenu()
        {
            BuildWindows();
        }

        /// <summary>Локальная сборка в builds/DearPassengers/DearPassengers.exe.</summary>
        public static void BuildWindows()
        {
            const string localPath = "builds/DearPassengers/DearPassengers.exe";
            Build(localPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        /// <summary>Точка входа game-ci (buildMethod). Читает аргументы командной строки.</summary>
        public static void Build()
        {
            string target = GetArg("-customBuildTarget", "StandaloneWindows64");
            string path = GetArg("-customBuildPath", "build/StandaloneWindows64/DearPassengers.exe");
            Build(path, ParseTarget(target), BuildOptions.None);
        }

        private static BuildTarget ParseTarget(string target)
        {
            return Enum.TryParse<BuildTarget>(target, true, out var result)
                ? result
                : BuildTarget.StandaloneWindows64;
        }

        /// <summary>Основная сборка.</summary>
        public static void Build(string locationPathName, BuildTarget target, BuildOptions options)
        {
            // Windows-билд обязан заканчиваться на .exe.
            if (target == BuildTarget.StandaloneWindows64 &&
                !locationPathName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                locationPathName += ".exe";
            }

            Debug.Log($"[BuildScript] Сборка: {target} → {locationPathName}");

            // Перед сборкой — прогоняем мастер настройки (идемпотентно).
            ProjectSetupWizard.Run();

            // Сцены: MainMenu первой (загрузочная), остальные по порядку.
            string[] sceneOrder =
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Airplane.unity",
                "Assets/Scenes/Flight.unity",
            };
            var scenes = sceneOrder.Where(File.Exists).ToArray();
            if (scenes.Length != sceneOrder.Length)
            {
                Debug.LogError("[BuildScript] Не все сцены найдены! Прерываем.");
                if (Application.isBatchMode) EditorApplication.Exit(120);
                throw new Exception("Отсутствуют сцены в Assets/Scenes.");
            }

            // Дублируем список в Build Settings (на всякий случай, для NGO scene management).
            EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(locationPathName)) ?? ".");

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = target,
                options = options,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] СБОРКА ПРОВАЛЕНА: {summary.result}, ошибок: {summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(101);
                throw new Exception($"Build failed: {summary.result}");
            }

            Debug.Log($"[BuildScript] Готово: {summary.totalSize / 1024 / 1024} МБ, " +
                      $"предупреждений: {summary.totalWarnings} → {locationPathName}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Прочитать значение аргумента командной строки: -name value.</summary>
        private static string GetArg(string name, string defaultValue)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return defaultValue;
        }
    }
}
