// ProjectSetupWizard.cs — мастер первичной настройки проекта (Editor). Запускается автоматически
// после первого импорта (InitializeOnLoad + delayCall) и перед сборкой (вызывает BuildScript).
// Что делает:
//   1) копирует префабы-«раковины» из Assets/Prefabs в Assets/Resources/Prefabs (для Resources.Load);
//   2) добавляет NetworkObject и сетевые скрипты в префабы Player / PassengerNPC / GameSession
//      (NetworkObject.OnValidate генерирует GlobalObjectIdHash — обязательно для Netcode);
//   3) создаёт URP-ассеты (UniversalRenderPipelineAsset + Forward Renderer) и назначает их;
//   4) прописывает 4 сцены в Build Settings;
//   5) выставляет имя продукта, компанию, обработчик ввода (Both);
//   6) пытается импортировать TMP Essentials (лучше-усилия).
// Все шаги идемпотентны — можно вызывать сколько угодно раз.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DearPassengers.EditorTools
{
    /// <summary>Мастер настройки проекта.</summary>
    [InitializeOnLoad]
    public static class ProjectSetupWizard
    {
        private const string MenuPath = "Assets/Dear Passengers/Настроить проект (мастер)";

        static ProjectSetupWizard()
        {
            // Запуск после импорта проекта (в т.ч. в batch-режиме CI).
            EditorApplication.delayCall += () => Run();
        }

        [MenuItem(MenuPath, priority = 1000)]
        public static void RunFromMenu()
        {
            Run();
            EditorUtility.DisplayDialog("Dear Passengers", "Настройка проекта завершена.\n" +
                "• Префабы подготовлены (NetworkObject + скрипты)\n" +
                "• URP-ассеты созданы и назначены\n" +
                "• Сцены прописаны в Build Settings\n\n" +
                "Можно нажимать Play!", "Отлично");
        }

        /// <summary>Основной (идемпотентный) прогон мастера.</summary>
        public static void Run()
        {
            try
            {
                EnsureFolders();
                CopyPrefabsToResources();
                FixNetworkPrefabs();
                EnsureUrpAssets();
                EnsureBuildScenes();
                EnsurePlayerSettings();
                TryImportTmpEssentials();
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ProjectSetupWizard] Ошибка настройки: {e}");
            }
        }

        // ---------- 1. Папки ----------

        private static void EnsureFolders()
        {
            foreach (var folder in new[]
            {
                "Assets/Resources",
                "Assets/Resources/Prefabs",
                "Assets/Settings",
                "Assets/Settings/URP",
            })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'),
                        System.IO.Path.GetFileName(folder));
                }
            }
        }

        // ---------- 2. Копии префабов в Resources ----------

        private static void CopyPrefabsToResources()
        {
            string[] prefabNames = { "Player", "PassengerNPC", "Bird", "SteeringWheel", "Door", "RefuelHose", "LobbyUI", "GameSession" };
            foreach (var name in prefabNames)
            {
                string src = $"Assets/Prefabs/{name}.prefab";
                string dst = $"Assets/Resources/Prefabs/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null) continue;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(src) == null)
                {
                    // Исходника нет — создаём пустую «раковину» сразу в Resources.
                    var go = new GameObject(name);
                    PrefabUtility.SaveAsPrefabAsset(go, dst);
                    Object.DestroyImmediate(go);
                    continue;
                }
                if (!AssetDatabase.CopyAsset(src, dst))
                {
                    Debug.LogWarning($"[ProjectSetupWizard] Не удалось скопировать {src} → {dst}; создаю пустую раковину.");
                    var go = new GameObject(name);
                    PrefabUtility.SaveAsPrefabAsset(go, dst);
                    Object.DestroyImmediate(go);
                }
            }
        }

        // ---------- 3. Сетевые префабы ----------

        /// <summary>Добавить компоненты в сетевые префабы (если их нет).</summary>
        private static void FixNetworkPrefabs()
        {
            EnsurePrefabComponents("Assets/Resources/Prefabs/Player.prefab",
                typeof(Unity.Netcode.NetworkObject), typeof(NetworkPlayer));
            EnsurePrefabComponents("Assets/Resources/Prefabs/PassengerNPC.prefab",
                typeof(Unity.Netcode.NetworkObject), typeof(PassengerNPC));
            EnsurePrefabComponents("Assets/Resources/Prefabs/GameSession.prefab",
                typeof(Unity.Netcode.NetworkObject), typeof(GameSession));
        }

        private static void EnsurePrefabComponents(string path, params System.Type[] components)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[ProjectSetupWizard] Префаб не найден: {path}");
                return;
            }

            bool changed = false;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var type in components)
                {
                    if (contents.GetComponent(type) == null)
                    {
                        contents.AddComponent(type);
                        changed = true;
                    }
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Debug.Log($"[ProjectSetupWizard] {path}: добавлены компоненты ({string.Join(", ", System.Linq.Enumerable.Select(components, t => t.Name))}).");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ---------- 4. URP ----------

        private static void EnsureUrpAssets()
        {
            const string rendererPath = "Assets/Settings/URP/ForwardRenderer.asset";
            const string pipelinePath = "Assets/Settings/URP/UniversalRenderPipelineAsset.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // Связываем renderer с pipeline (внутреннее поле m_RendererDataList).
            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            if (list != null && list.isArray)
            {
                if (list.arraySize == 0 || list.GetArrayElementAtIndex(0).objectReferenceValue != rendererData)
                {
                    list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("[ProjectSetupWizard] Поле m_RendererDataList не найдено — URP использует свои значения по умолчанию.");
            }

            // Назначаем пайплайн (глобально + во все уровни качества).
            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
            }
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                if (QualitySettings.renderPipeline != pipeline)
                {
                    QualitySettings.renderPipeline = pipeline;
                }
            }

            AssetDatabase.SaveAssets();
        }

        // ---------- 5. Сцены ----------

        private static void EnsureBuildScenes()
        {
            // MainMenu обязана быть первой (загрузочная сцена билда).
            string[] ordered =
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Airplane.unity",
                "Assets/Scenes/Flight.unity",
            };
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in ordered)
            {
                if (System.IO.File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    Debug.LogWarning($"[ProjectSetupWizard] Сцена не найдена: {path}");
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- 6. PlayerSettings ----------

        private static void EnsurePlayerSettings()
        {
            if (PlayerSettings.productName != "Dear Passengers")
            {
                PlayerSettings.productName = "Dear Passengers";
            }
            if (PlayerSettings.companyName != "DearPassengersFans")
            {
                PlayerSettings.companyName = "DearPassengersFans";
            }

            // Ввод: оба бэкенда (Input System используется кодом; легаси — для совместимости).
            try
            {
                var settingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (settingsAsset != null && settingsAsset.Length > 0)
                {
                    var so = new SerializedObject(settingsAsset[0]);
                    var prop = so.FindProperty("activeInputHandler");
                    if (prop != null && prop.intValue == 0)
                    {
                        prop.intValue = 2; // Both
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ProjectSetupWizard] activeInputHandler: {e.Message}");
            }

            // FullScreenMode по умолчанию (можно менять в настройках игры).
            try
            {
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultScreenWidth = 1600;
                PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.runInBackground = true;
            }
            catch { /* не критично */ }
        }

        // ---------- 7. TMP Essentials (best effort) ----------

        private static void TryImportTmpEssentials()
        {
            try
            {
                var importerType = System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
                if (importerType == null) return;
                foreach (var methodName in new[] { "ImportProjectResources", "ImportPackageResources", "ImportResources" })
                {
                    var method = importerType.GetMethod(methodName,
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(null, null);
                        Debug.Log("[ProjectSetupWizard] TMP Essentials: импорт запущен.");
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ProjectSetupWizard] TMP Essentials не импортированы автоматически ({e.Message}). " +
                    "Если TMP-текст не отображается: Window → TextMeshPro → Import TMP Essential Resources.");
            }
        }
    }
}
