using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Stealth.Audio;
using Stealth.UI;

namespace Stealth.EditorTools
{
    /// <summary>Builds the title screen scene: a small night diorama plus the menu UI.</summary>
    public static class MenuBuilder
    {
        private static readonly Color Ink = new Color(0.93f, 0.96f, 1f);
        private static readonly Color Dim = new Color(0.6f, 0.68f, 0.78f);
        private static readonly Color Accent = new Color(0.36f, 0.85f, 1f);
        private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.1f, 0.82f);

        [MenuItem("Stealth/Step 5 - Build Main Menu", priority = 5)]
        public static void Build()
        {
            AssetFactory.LoadMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();
            GameObject canvas = BuildUi();
            BuildAudio(canvas);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StealthPaths.MenuScene);
            Debug.Log("[Stealth] Main menu scene built at " + StealthPaths.MenuScene);
        }

        private static void BuildEnvironment()
        {
            GameObject cameraGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetPositionAndRotation(new Vector3(6.5f, 3.2f, -9f), Quaternion.Euler(8f, -28f, 0f));

            Camera camera = cameraGo.GetComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 200f;

            GameObject sunGo = new GameObject("Moonlight");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.6f, 0.7f, 0.95f);
            sun.intensity = 0.6f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(35f, 20f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetFactory.NightSky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.17f, 0.23f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.09f);
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 55f;

            Transform diorama = new GameObject("Diorama").transform;
            BuildUtils.Box("Ground", diorama, new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f),
                AssetFactory.Ground, StealthLayers.Environment, false);

            BuildUtils.Box("Container_A", diorama, new Vector3(-3f, 1.35f, 4f), new Vector3(6.2f, 2.7f, 2.5f),
                AssetFactory.ContainerTeal, StealthLayers.Environment, false);
            BuildUtils.Box("Container_B", diorama, new Vector3(4.5f, 1.35f, 7.5f), new Vector3(6.2f, 2.7f, 2.5f),
                AssetFactory.ContainerRust, StealthLayers.Environment, false);
            BuildUtils.Box("Container_C", diorama, new Vector3(-6f, 1.35f, 10f), new Vector3(6.2f, 2.7f, 2.5f),
                AssetFactory.ContainerBlue, StealthLayers.Environment, false);
            BuildUtils.Box("Crate", diorama, new Vector3(1.6f, 0.65f, 1.2f), new Vector3(1.3f, 1.3f, 1.3f),
                AssetFactory.Crate, StealthLayers.Environment, false);

            GameObject lamp = (GameObject)PrefabUtility.InstantiatePrefab(PrefabFactory.Load("StreetLamp"), diorama);
            if (lamp != null) lamp.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 2f), Quaternion.Euler(0f, 110f, 0f));

            // A lone figure in the light, purely for mood.
            GameObject figure = BuildUtils.Empty("Figure", diorama, new Vector3(-4.6f, 0f, 1.4f),
                Quaternion.Euler(0f, 160f, 0f));
            BuildUtils.Primitive(PrimitiveType.Capsule, "Body", figure.transform, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.6f, 0.9f, 0.6f), AssetFactory.PlayerBody, StealthLayers.Environment, false);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Head", figure.transform, new Vector3(0f, 1.63f, 0f),
                new Vector3(0.42f, 0.42f, 0.42f), AssetFactory.PlayerBody, StealthLayers.Environment, false);
            BuildUtils.Box("Visor", figure.transform, new Vector3(0f, 1.66f, 0.17f), new Vector3(0.3f, 0.08f, 0.1f),
                AssetFactory.PlayerAccent, StealthLayers.Environment, false);
        }

        private static GameObject BuildUi()
        {
            Sprite panelSprite = AssetFactory.LoadSprite("panel");

            GameObject canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            BuildUtils.Sprite("Fade", root, AssetFactory.LoadSprite("vignette"), new Color(0f, 0f, 0f, 0.75f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI title = BuildUtils.Label("Title", root, "NIGHT SHIFT", 108f, TextAlignmentOptions.Left, Ink,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -300f), new Vector2(1100f, -160f));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 8f;

            BuildUtils.Label("Subtitle", root, "A third person stealth prototype", 30f, TextAlignmentOptions.Left, Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -350f), new Vector2(1100f, -300f));

            BuildUtils.Label("Brief", root, "Slip through the depot without being caught.\nReach the extraction point in the north.",
                24f, TextAlignmentOptions.TopLeft, Dim,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -470f), new Vector2(900f, -380f));

            Button play = BuildUtils.Button("PlayButton", root, "START MISSION", panelSprite, new Vector2(-560f, -60f),
                new Vector2(380f, 72f), new Color(0.12f, 0.32f, 0.4f, 0.95f), Ink, 30f);
            Button controls = BuildUtils.Button("ControlsButton", root, "CONTROLS", panelSprite, new Vector2(-560f, -150f),
                new Vector2(380f, 64f), PanelColor, Ink, 26f);
            Button quit = BuildUtils.Button("QuitButton", root, "QUIT", panelSprite, new Vector2(-560f, -236f),
                new Vector2(380f, 64f), PanelColor, Dim, 26f);

            // Controls panel.
            RectTransform controlsPanel = BuildUtils.Rect("ControlsPanel", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero);
            controlsPanel.sizeDelta = new Vector2(620f, 560f);
            controlsPanel.anchoredPosition = new Vector2(-400f, 0f);

            Image controlsBackground = controlsPanel.gameObject.AddComponent<Image>();
            controlsBackground.sprite = panelSprite;
            controlsBackground.type = Image.Type.Sliced;
            controlsBackground.color = PanelColor;

            BuildUtils.Label("ControlsTitle", controlsPanel, "CONTROLS", 34f, TextAlignmentOptions.Center, Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -80f), new Vector2(-20f, -24f))
                .fontStyle = FontStyles.Bold;

            BuildUtils.Label("ControlsBody", controlsPanel,
                "WASD            Move\n" +
                "Mouse           Look\n" +
                "Shift           Sprint  (loud)\n" +
                "C / Ctrl        Crouch  (quiet, low)\n" +
                "E               Interact / hide\n" +
                "Right mouse     Aim a stone\n" +
                "Left mouse      Throw it\n" +
                "Esc             Pause\n\n" +
                "Guards show a badge above their head:\n" +
                "it fills while they are noticing you.\n" +
                "Break line of sight or hide before it\n" +
                "turns into a red exclamation mark.",
                23f, TextAlignmentOptions.TopLeft, Ink,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(34f, 30f), new Vector2(-34f, -90f));

            MainMenuController controller = canvasGo.AddComponent<MainMenuController>();
            BuildUtils.SetFields(controller,
                ("_playButton", play), ("_controlsButton", controls), ("_quitButton", quit),
                ("_controlsPanel", controlsPanel.gameObject));

            return canvasGo;
        }

        private static void BuildAudio(GameObject canvas)
        {
            GameObject audioGo = new GameObject("Audio");

            AudioManager manager = audioGo.AddComponent<AudioManager>();
            BuildUtils.SetField(manager, "_library", AssetFactory.Sounds);

            AudioSource ambient = audioGo.AddComponent<AudioSource>();
            ambient.clip = AssetFactory.Clip("ambient_loop");
            ambient.loop = true;
            ambient.playOnAwake = true;
            ambient.volume = 0.4f;
            ambient.spatialBlend = 0f;
        }
    }
}
