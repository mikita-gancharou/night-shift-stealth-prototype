using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Stealth.Level;
using Stealth.UI;

namespace Stealth.EditorTools
{
    /// <summary>Builds the in-game HUD, the pause menu and the end screen into the level scene.</summary>
    public static class HudBuilder
    {
        private static readonly Color Ink = new Color(0.93f, 0.96f, 1f);
        private static readonly Color Dim = new Color(0.62f, 0.7f, 0.8f);
        private static readonly Color Accent = new Color(0.36f, 0.85f, 1f);
        private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.1f, 0.72f);
        private static readonly Color DarkOverlay = new Color(0.02f, 0.03f, 0.05f, 0.88f);

        public static GameObject Build(LevelGoal goal)
        {
            Sprite panelSprite = AssetFactory.LoadSprite("panel");
            Sprite barSprite = AssetFactory.LoadSprite("bar_fill");

            GameObject canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            // ----- full screen effects ------------------------------------------------------------
            Image vignette = BuildUtils.Sprite("Vignette", root, AssetFactory.LoadSprite("vignette"),
                new Color(1f, 0.2f, 0.2f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image flash = BuildUtils.Sprite("Flash", root, null, new Color(1f, 0.85f, 0.85f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ----- objective (top left) -----------------------------------------------------------
            RectTransform objectivePanel = Panel("ObjectivePanel", root, panelSprite,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -150f), new Vector2(430f, -34f));

            BuildUtils.Label("Caption", objectivePanel, "OBJECTIVE", 18f, TextAlignmentOptions.TopLeft, Dim,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -34f), new Vector2(-18f, -12f));
            TextMeshProUGUI objectiveLabel = BuildUtils.Label("Objective", objectivePanel, "Reach the extraction point",
                24f, TextAlignmentOptions.TopLeft, Ink,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -70f), new Vector2(-18f, -38f));
            TextMeshProUGUI intelLabel = BuildUtils.Label("Intel", objectivePanel, "INTEL  0/0", 20f,
                TextAlignmentOptions.TopLeft, Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -104f), new Vector2(-18f, -74f));

            // ----- run info (top right) -----------------------------------------------------------
            TextMeshProUGUI timerLabel = BuildUtils.Label("Timer", root, "00:00", 34f, TextAlignmentOptions.TopRight, Ink,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -84f), new Vector2(-34f, -34f));
            TextMeshProUGUI spottedLabel = BuildUtils.Label("Spotted", root, "SPOTTED  0", 20f,
                TextAlignmentOptions.TopRight, Dim,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -114f), new Vector2(-34f, -86f));

            // ----- player status (bottom left) ----------------------------------------------------
            RectTransform statusPanel = Panel("StatusPanel", root, panelSprite,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 34f), new Vector2(360f, 170f));

            TextMeshProUGUI stanceLabel = BuildUtils.Label("Stance", statusPanel, "STANDING", 26f,
                TextAlignmentOptions.TopLeft, Ink,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -46f), new Vector2(-18f, -12f));

            Image visibilityFill = Meter("Visibility", statusPanel, barSprite, "VISIBILITY", -60f, Accent);
            Image noiseFill = Meter("Noise", statusPanel, barSprite, "NOISE", -100f, new Color(1f, 0.72f, 0.3f));

            // ----- stones (bottom right) ----------------------------------------------------------
            RectTransform stonePanel = Panel("StonePanel", root, panelSprite,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-330f, 34f), new Vector2(-34f, 150f));

            BuildUtils.Sprite("StoneIcon", stonePanel, AssetFactory.LoadSprite("icon_stone"), Ink,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -66f), new Vector2(66f, -18f));
            TextMeshProUGUI stoneLabel = BuildUtils.Label("StoneCount", stonePanel, "3/6", 30f,
                TextAlignmentOptions.TopLeft, Ink,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(78f, -62f), new Vector2(-18f, -20f));
            BuildUtils.Label("ThrowHint", stonePanel, "RMB aim   ·   LMB throw", 17f, TextAlignmentOptions.BottomLeft,
                Dim, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 16f), new Vector2(-18f, 46f));

            // ----- centre -------------------------------------------------------------------------
            Image crosshair = BuildUtils.Sprite("Crosshair", root, AssetFactory.LoadSprite("crosshair"),
                new Color(1f, 1f, 1f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-26f, -26f), new Vector2(26f, 26f));
            crosshair.enabled = false;

            Image detectionRing = BuildUtils.Sprite("DetectionRing", root, AssetFactory.LoadSprite("ring"),
                Accent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-46f, -46f), new Vector2(46f, 46f));
            detectionRing.type = Image.Type.Filled;
            detectionRing.fillMethod = Image.FillMethod.Radial360;
            detectionRing.fillOrigin = (int)Image.Origin360.Top;
            detectionRing.fillAmount = 0f;

            RectTransform arrowsRoot = BuildUtils.Rect("DetectionArrows", root, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image arrowTemplate = BuildUtils.Sprite("ArrowTemplate", arrowsRoot, AssetFactory.LoadSprite("arrow"),
                Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-24f, -24f), new Vector2(24f, 24f));
            arrowTemplate.type = Image.Type.Filled;
            arrowTemplate.fillMethod = Image.FillMethod.Vertical;
            arrowTemplate.fillOrigin = 0;

            CanvasGroup bannerGroup = Group("Banner", root, new Vector2(0.5f, 1f), new Vector2(0f, -140f),
                new Vector2(900f, 60f));
            TextMeshProUGUI bannerLabel = BuildUtils.Label("BannerLabel", bannerGroup.transform, string.Empty, 40f,
                TextAlignmentOptions.Center, Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bannerLabel.fontStyle = FontStyles.Bold;

            CanvasGroup promptGroup = Group("InteractionPrompt", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f),
                new Vector2(700f, 50f));
            TextMeshProUGUI promptLabel = BuildUtils.Label("PromptLabel", promptGroup.transform, string.Empty, 26f,
                TextAlignmentOptions.Center, Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CanvasGroup hintGroup = Group("Hint", root, new Vector2(0.5f, 0f), new Vector2(0f, 210f),
                new Vector2(1100f, 90f));
            Image hintBackground = BuildUtils.Sprite("HintBackground", hintGroup.transform, panelSprite, PanelColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hintBackground.type = Image.Type.Sliced;
            TextMeshProUGUI hintLabel = BuildUtils.Label("HintLabel", hintGroup.transform, string.Empty, 24f,
                TextAlignmentOptions.Center, Ink, Vector2.zero, Vector2.one, new Vector2(24f, 12f), new Vector2(-24f, -12f));

            // ----- menus --------------------------------------------------------------------------
            GameObject pausePanel = BuildPausePanel(root, panelSprite, out Button resume, out Button restart,
                out Button toMenu, out Button quit);
            GameObject endPanel = BuildEndPanel(root, panelSprite, out TextMeshProUGUI endTitle,
                out TextMeshProUGUI endSubtitle, out TextMeshProUGUI endStats, out Button retry, out Button endMenu);

            // ----- components ---------------------------------------------------------------------
            HUDController hud = canvasGo.AddComponent<HUDController>();
            BuildUtils.SetFields(hud,
                ("_objectiveLabel", objectiveLabel), ("_intelLabel", intelLabel), ("_goal", goal),
                ("_timerLabel", timerLabel), ("_spottedLabel", spottedLabel),
                ("_stanceLabel", stanceLabel), ("_stoneLabel", stoneLabel),
                ("_visibilityFill", visibilityFill), ("_noiseFill", noiseFill),
                ("_crosshair", crosshair), ("_detectionFill", detectionRing),
                ("_bannerGroup", bannerGroup), ("_bannerLabel", bannerLabel));

            DetectionIndicators indicators = canvasGo.AddComponent<DetectionIndicators>();
            BuildUtils.SetFields(indicators, ("_root", arrowsRoot), ("_arrowTemplate", arrowTemplate));

            ScreenEffects effects = canvasGo.AddComponent<ScreenEffects>();
            BuildUtils.SetFields(effects, ("_vignette", vignette), ("_flash", flash));

            InteractionPrompt prompt = canvasGo.AddComponent<InteractionPrompt>();
            BuildUtils.SetFields(prompt, ("_group", promptGroup), ("_label", promptLabel));

            HintDisplay hints = canvasGo.AddComponent<HintDisplay>();
            BuildUtils.SetFields(hints, ("_group", hintGroup), ("_label", hintLabel));

            PauseMenu pause = canvasGo.AddComponent<PauseMenu>();
            BuildUtils.SetFields(pause,
                ("_panel", pausePanel), ("_resumeButton", resume), ("_restartButton", restart),
                ("_menuButton", toMenu), ("_quitButton", quit));

            EndScreen end = canvasGo.AddComponent<EndScreen>();
            BuildUtils.SetFields(end,
                ("_panel", endPanel), ("_title", endTitle), ("_subtitle", endSubtitle), ("_stats", endStats),
                ("_retryButton", retry), ("_menuButton", endMenu));

            return canvasGo;
        }

        private static GameObject BuildPausePanel(Transform root, Sprite panelSprite, out Button resume,
            out Button restart, out Button menu, out Button quit)
        {
            RectTransform panel = BuildUtils.Rect("PausePanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = DarkOverlay;

            BuildUtils.Label("PauseTitle", panel, "PAUSED", 68f, TextAlignmentOptions.Center, Ink,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, 150f), new Vector2(400f, 250f))
                .fontStyle = FontStyles.Bold;

            resume = BuildUtils.Button("ResumeButton", panel, "RESUME", panelSprite, new Vector2(0f, 60f),
                new Vector2(340f, 62f), new Color(0.12f, 0.3f, 0.38f, 0.95f), Ink);
            restart = BuildUtils.Button("RestartButton", panel, "RESTART MISSION", panelSprite, new Vector2(0f, -16f),
                new Vector2(340f, 62f), PanelColor, Ink);
            menu = BuildUtils.Button("MenuButton", panel, "MAIN MENU", panelSprite, new Vector2(0f, -92f),
                new Vector2(340f, 62f), PanelColor, Ink);
            quit = BuildUtils.Button("QuitButton", panel, "QUIT", panelSprite, new Vector2(0f, -168f),
                new Vector2(340f, 62f), PanelColor, Dim);

            BuildUtils.Label("PauseHint", panel, "WASD move   ·   Shift sprint   ·   C crouch   ·   E interact   ·   RMB aim + LMB throw",
                20f, TextAlignmentOptions.Center, Dim,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, 60f), new Vector2(700f, 100f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static GameObject BuildEndPanel(Transform root, Sprite panelSprite, out TextMeshProUGUI title,
            out TextMeshProUGUI subtitle, out TextMeshProUGUI stats, out Button retry, out Button menu)
        {
            RectTransform panel = BuildUtils.Rect("EndPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = DarkOverlay;

            title = BuildUtils.Label("EndTitle", panel, "MISSION COMPLETE", 72f, TextAlignmentOptions.Center, Ink,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 180f), new Vector2(600f, 290f));
            title.fontStyle = FontStyles.Bold;

            subtitle = BuildUtils.Label("EndSubtitle", panel, "RANK: GHOST", 32f, TextAlignmentOptions.Center, Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 120f), new Vector2(600f, 176f));

            RectTransform statsPanel = BuildUtils.Rect("EndStatsPanel", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-330f, -76f), new Vector2(330f, 106f));
            Image statsBackground = statsPanel.gameObject.AddComponent<Image>();
            statsBackground.sprite = panelSprite;
            statsBackground.type = Image.Type.Sliced;
            statsBackground.color = new Color(0.04f, 0.06f, 0.09f, 0.9f);
            statsBackground.raycastTarget = false;

            stats = BuildUtils.Label("EndStats", statsPanel, string.Empty, 26f, TextAlignmentOptions.Center, Ink,
                Vector2.zero, Vector2.one, new Vector2(24f, 18f), new Vector2(-24f, -18f));

            retry = BuildUtils.Button("RetryButton", panel, "TRY AGAIN", panelSprite, new Vector2(-180f, -140f),
                new Vector2(320f, 64f), new Color(0.12f, 0.3f, 0.38f, 0.95f), Ink);
            menu = BuildUtils.Button("EndMenuButton", panel, "MAIN MENU", panelSprite, new Vector2(180f, -140f),
                new Vector2(320f, 64f), PanelColor, Ink);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        // ----- small helpers --------------------------------------------------------------------

        private static RectTransform Panel(string name, Transform parent, Sprite sprite, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = BuildUtils.Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = PanelColor;
            image.raycastTarget = false;
            return rect;
        }

        private static CanvasGroup Group(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = BuildUtils.Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static Image Meter(string name, Transform parent, Sprite barSprite, string caption, float top, Color color)
        {
            BuildUtils.Label(name + "Caption", parent, caption, 15f, TextAlignmentOptions.TopLeft, Dim,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, top - 16f), new Vector2(140f, top));

            RectTransform background = BuildUtils.Rect(name + "Bar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(120f, top - 16f), new Vector2(-18f, top - 2f));
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = barSprite;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.12f);
            backgroundImage.raycastTarget = false;

            Image fill = BuildUtils.Sprite(name + "Fill", background, barSprite, color,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            return fill;
        }
    }
}
