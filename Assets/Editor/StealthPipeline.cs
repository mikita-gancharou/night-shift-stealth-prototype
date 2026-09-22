using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Stealth.EditorTools
{
    /// <summary>
    /// Entry points for regenerating the game and producing a build, both from the editor menu and from
    /// the command line:
    ///   Unity.exe -batchmode -quit -projectPath &lt;project&gt; -executeMethod Stealth.EditorTools.StealthPipeline.BuildContent
    /// </summary>
    public static class StealthPipeline
    {
        public const string BuildFolder = "Build/Windows";
        public const string ExecutableName = "NightShift.exe";

        [MenuItem("Stealth/Rebuild Everything", priority = 30)]
        public static void RebuildEverythingMenu()
        {
            AssetFactory.BuildAll();
            PrefabFactory.BuildAll();
            LevelBuilder.Build();
            MenuBuilder.Build();
            ConfigureBuildSettings();
        }

        // ----- command line steps -----------------------------------------------------------------

        public static void SetupProject()
        {
            Run("setup", () =>
            {
                StealthSetup.Configure();
                AssetDatabase.Refresh();
            });
        }

        /// <summary>
        /// Imports the TextMeshPro essential resources and waits for the import to finish.
        /// Must run without -quit: package import is asynchronous, so the editor is closed from the callback.
        /// </summary>
        public static void ImportTextMeshPro()
        {
            if (StealthSetup.TextMeshProReady)
            {
                Debug.Log("[Stealth] TextMeshPro resources already imported.");
                Quit(0);
                return;
            }

            AssetDatabase.importPackageCompleted += packageName =>
            {
                Debug.Log($"[Stealth] Imported package '{packageName}'.");
                AssetDatabase.Refresh();
                Quit(0);
            };

            AssetDatabase.importPackageFailed += (packageName, error) =>
            {
                Debug.LogError($"[Stealth] Import of '{packageName}' failed: {error}");
                Quit(1);
            };

            AssetDatabase.importPackageCancelled += packageName =>
            {
                Debug.LogError($"[Stealth] Import of '{packageName}' was cancelled.");
                Quit(1);
            };

            _importDeadline = EditorApplication.timeSinceStartup + 240d;
            EditorApplication.update += WatchImport;

            StealthSetup.ImportTextMeshProResources();
        }

        private static double _importDeadline;

        private static void WatchImport()
        {
            if (StealthSetup.TextMeshProReady)
            {
                EditorApplication.update -= WatchImport;
                Debug.Log("[Stealth] TextMeshPro resources are in place.");
                Quit(0);
                return;
            }

            if (EditorApplication.timeSinceStartup <= _importDeadline) return;

            EditorApplication.update -= WatchImport;
            Debug.LogError("[Stealth] Timed out waiting for the TextMeshPro import.");
            Quit(1);
        }

        private static void Quit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static void BuildContent()
        {
            Run("content", () =>
            {
                if (!StealthSetup.TextMeshProReady)
                {
                    throw new Exception("TextMeshPro resources are missing - run StealthPipeline.SetupProject first.");
                }

                AssetFactory.BuildAll();
                PrefabFactory.BuildAll();
                LevelBuilder.Build();
                MenuBuilder.Build();
                ConfigureBuildSettings();
                AssetDatabase.SaveAssets();
            });
        }

        public static void ValidateLevel()
        {
            Run("validate", () =>
            {
                EditorSceneManager.OpenScene(StealthPaths.LevelScene, OpenSceneMode.Single);
                bool ok = LevelValidator.Validate(out string report);
                Debug.Log(report);

                if (!ok) throw new Exception("Level validation failed.");
            });
        }

        public static void BuildWindowsPlayer()
        {
            Run("player", () =>
            {
                ConfigureBuildSettings();

                string directory = Path.Combine(Directory.GetCurrentDirectory(), BuildFolder);
                Directory.CreateDirectory(directory);

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { StealthPaths.MenuScene, StealthPaths.LevelScene },
                    locationPathName = Path.Combine(BuildFolder, ExecutableName),
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                Debug.Log($"[Stealth] Build result: {summary.result}, size: {summary.totalSize / (1024 * 1024)} MB, " +
                          $"time: {summary.totalTime.TotalSeconds:0} s, errors: {summary.totalErrors}");

                if (summary.result != BuildResult.Succeeded) throw new Exception("Player build failed.");
            });
        }

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(StealthPaths.MenuScene, true),
                new EditorBuildSettingsScene(StealthPaths.LevelScene, true)
            };
        }

        private static void Run(string step, Action action)
        {
            try
            {
                action();
                Debug.Log($"[Stealth] Pipeline step '{step}' finished.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Stealth] Pipeline step '{step}' failed: {exception}");

                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
