using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Stealth.EditorTools
{
    /// <summary>
    /// One-time project configuration: layers, rendering, quality and the TextMeshPro resources.
    /// Run from the Stealth menu or from the command line (see <see cref="StealthPipeline"/>).
    /// </summary>
    public static class StealthSetup
    {
        private const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        private const string TmpFolder = "Assets/TextMesh Pro";

        [MenuItem("Stealth/Step 1 - Configure Project", priority = 1)]
        public static void Configure()
        {
            ConfigureLayers();
            ConfigurePlayerSettings();
            ConfigureQuality();
            ConfigurePhysics();
            ImportTextMeshProResources();

            AssetDatabase.SaveAssets();
            Debug.Log("[Stealth] Project configured.");
        }

        public static bool TextMeshProReady => AssetDatabase.IsValidFolder(TmpFolder);

        public static void ImportTextMeshProResources()
        {
            if (TextMeshProReady)
            {
                Debug.Log("[Stealth] TextMeshPro resources already present.");
                return;
            }

            Debug.Log("[Stealth] Importing TextMeshPro essential resources...");
            AssetDatabase.ImportPackage(TmpPackage, false);
            AssetDatabase.Refresh();
        }

        private static void ConfigureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Stealth] TagManager.asset not found.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            SetLayer(layers, StealthLayers.Environment, "Environment");
            SetLayer(layers, StealthLayers.Player, "Player");
            SetLayer(layers, StealthLayers.Enemy, "Enemy");
            SetLayer(layers, StealthLayers.Interactable, "Interactable");
            SetLayer(layers, StealthLayers.Throwable, "Throwable");
            SetLayer(layers, StealthLayers.VisionCone, "VisionCone");
            SetLayer(layers, StealthLayers.Zone, "Zone");

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            if (layers == null || index >= layers.arraySize) return;

            layers.GetArrayElementAtIndex(index).stringValue = name;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Night Shift";
            PlayerSettings.companyName = "Stealth Prototype";
            PlayerSettings.bundleVersion = "1.0";

            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;

            // Linear space makes the night lighting behave far better than gamma.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
        }

        private static void ConfigureQuality()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty levels = settings.FindProperty("m_QualitySettings");
            if (levels == null) return;

            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                SetInt(level, "pixelLightCount", 10);
                SetInt(level, "shadows", 2);           // hard and soft shadows
                SetInt(level, "shadowResolution", 2);  // high
                SetInt(level, "shadowCascades", 2);
                SetFloat(level, "shadowDistance", 75f);
                SetInt(level, "antiAliasing", 2);
                SetInt(level, "vSyncCount", 1);
                SetFloat(level, "lodBias", 1.6f);
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePhysics()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject physics = new SerializedObject(assets[0]);

            SerializedProperty sleepThreshold = physics.FindProperty("m_SleepThreshold");
            if (sleepThreshold != null) sleepThreshold.floatValue = 0.01f;

            SerializedProperty defaultSolverIterations = physics.FindProperty("m_DefaultSolverIterations");
            if (defaultSolverIterations != null) defaultSolverIterations.intValue = 8;

            physics.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(SerializedProperty parent, string name, int value)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property != null) property.intValue = value;
        }

        private static void SetFloat(SerializedProperty parent, string name, float value)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property != null) property.floatValue = value;
        }
    }
}
