using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Stealth.Enemies;
using Stealth.Interaction;
using Stealth.Level;
using Stealth.Perception;
using Stealth.Player;
using Stealth.Visuals;

namespace Stealth.EditorTools
{
    /// <summary>
    /// Builds every prefab of the game out of primitives and wires all component references.
    /// Running it again overwrites the prefabs, so the whole game content is reproducible.
    /// </summary>
    public static class PrefabFactory
    {
        private const float BodyHeight = 1.8f;

        [MenuItem("Stealth/Step 3 - Build Prefabs", priority = 3)]
        public static void BuildAll()
        {
            AssetFactory.LoadMaterials();
            BuildUtils.EnsureFolder(StealthPaths.Prefabs);

            GameObject stone = BuildStone();
            BuildPlayer(stone.GetComponent<Stone>());
            BuildGuard();
            BuildSecurityCamera();
            BuildHidingSpot();
            BuildStonePile();
            BuildIntel();
            BuildLightSwitch();
            BuildStreetLamp();
            BuildExtractionPoint();

            AssetDatabase.SaveAssets();
            Debug.Log("[Stealth] Prefabs built.");
        }

        public static GameObject Load(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{StealthPaths.Prefabs}/{name}.prefab");

        private static GameObject Save(GameObject instance, string name)
        {
            string path = $"{StealthPaths.Prefabs}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ----- player -----------------------------------------------------------------------------

        private static void BuildPlayer(Stone stonePrefab)
        {
            GameObject root = new GameObject("Player") { layer = StealthLayers.Player };
            root.tag = "Player";

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = BodyHeight;
            capsule.radius = 0.32f;
            capsule.center = new Vector3(0f, BodyHeight * 0.5f, 0f);

            // Visual body. The stance component scales this root down when crouching.
            GameObject visual = BuildUtils.Empty("Visual", root.transform);
            BuildUtils.Primitive(PrimitiveType.Capsule, "Body", visual.transform, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.6f, 0.9f, 0.6f), AssetFactory.PlayerBody, StealthLayers.Player, false);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Head", visual.transform, new Vector3(0f, 1.63f, 0f),
                new Vector3(0.42f, 0.42f, 0.42f), AssetFactory.PlayerBody, StealthLayers.Player, false);
            BuildUtils.Box("Visor", visual.transform, new Vector3(0f, 1.66f, 0.17f), new Vector3(0.3f, 0.08f, 0.1f),
                AssetFactory.PlayerAccent, StealthLayers.Player, false);
            BuildUtils.Box("Backpack", visual.transform, new Vector3(0f, 1.12f, -0.26f), new Vector3(0.36f, 0.45f, 0.22f),
                AssetFactory.GuardBody, StealthLayers.Player, false);

            GameObject throwOrigin = BuildUtils.Empty("ThrowOrigin", root.transform, new Vector3(0.22f, 1.45f, 0.3f));

            // Aim preview: a line plus a landing marker on the ground.
            GameObject arcGo = BuildUtils.Empty("AimArc", root.transform);
            LineRenderer line = arcGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.06f;
            line.numCapVertices = 2;
            line.sharedMaterial = AssetFactory.ArcGlow;
            line.textureMode = LineTextureMode.Tile;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            GameObject marker = BuildUtils.Primitive(PrimitiveType.Quad, "LandingMarker", arcGo.transform,
                Vector3.zero, new Vector3(1.6f, 1.6f, 1f), AssetFactory.MarkerGlow, StealthLayers.VisionCone, false);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ThrowArc arc = arcGo.AddComponent<ThrowArc>();
            BuildUtils.SetFields(arc,
                ("_landingMarker", marker.transform),
                ("_collisionMask", StealthLayers.Mask(StealthLayers.Environment, 0)));

            // Components.
            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            PlayerStance stance = root.AddComponent<PlayerStance>();
            PlayerMotor motor = root.AddComponent<PlayerMotor>();
            PlayerNoise noise = root.AddComponent<PlayerNoise>();
            PlayerVisibility visibility = root.AddComponent<PlayerVisibility>();
            PlayerHiding hiding = root.AddComponent<PlayerHiding>();
            PlayerInteractor interactor = root.AddComponent<PlayerInteractor>();
            PlayerThrower thrower = root.AddComponent<PlayerThrower>();
            PlayerController controller = root.AddComponent<PlayerController>();

            BuildUtils.SetField(stance, "_visualRoot", visual.transform);
            BuildUtils.SetField(motor, "_groundMask", StealthLayers.Mask(StealthLayers.Environment, 0));
            BuildUtils.SetFields(noise, ("_motor", motor), ("_stance", stance));
            BuildUtils.SetFields(visibility, ("_stance", stance), ("_motor", motor));
            BuildUtils.SetFields(hiding,
                ("_motor", motor), ("_stance", stance), ("_rigidbody", body), ("_visualRoot", visual));
            BuildUtils.SetFields(interactor,
                ("_player", controller), ("_interactableMask", StealthLayers.Mask(StealthLayers.Interactable)));
            BuildUtils.SetFields(thrower,
                ("_stonePrefab", stonePrefab),
                ("_throwOrigin", throwOrigin.transform),
                ("_arc", arc),
                ("_aimMask", StealthLayers.Mask(StealthLayers.Environment, 0)));
            BuildUtils.SetFields(controller,
                ("_input", input), ("_motor", motor), ("_stance", stance), ("_noise", noise),
                ("_visibility", visibility), ("_hiding", hiding), ("_interactor", interactor), ("_thrower", thrower));

            Save(root, "Player");
        }

        // ----- guard ------------------------------------------------------------------------------

        private static void BuildGuard()
        {
            GameObject root = new GameObject("Guard") { layer = StealthLayers.Enemy };

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = BodyHeight;
            agent.baseOffset = 0f;
            agent.speed = 2f;
            agent.angularSpeed = 420f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = BodyHeight;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, BodyHeight * 0.5f, 0f);

            GameObject eye = BuildUtils.Empty("Eye", root.transform, new Vector3(0f, 1.62f, 0.2f));

            GameObject visual = BuildUtils.Empty("Visual", root.transform);
            BuildUtils.Primitive(PrimitiveType.Capsule, "Body", visual.transform, new Vector3(0f, 0.92f, 0f),
                new Vector3(0.68f, 0.92f, 0.68f), AssetFactory.GuardBody, StealthLayers.Enemy, false);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Head", visual.transform, new Vector3(0f, 1.66f, 0f),
                new Vector3(0.46f, 0.46f, 0.46f), AssetFactory.GuardBody, StealthLayers.Enemy, false);
            GameObject visor = BuildUtils.Box("Visor", visual.transform, new Vector3(0f, 1.68f, 0.19f),
                new Vector3(0.34f, 0.1f, 0.1f), AssetFactory.GuardAccent, StealthLayers.Enemy, false);
            GameObject shoulder = BuildUtils.Box("ShoulderLamp", visual.transform, new Vector3(0.28f, 1.42f, 0.05f),
                new Vector3(0.14f, 0.14f, 0.14f), AssetFactory.GuardAccent, StealthLayers.Enemy, false);

            GameObject lampGo = BuildUtils.Empty("Flashlight", visual.transform, new Vector3(0.28f, 1.42f, 0.16f));
            Light lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.range = 16f;
            lamp.spotAngle = 55f;
            lamp.intensity = 2.4f;
            lamp.color = new Color(1f, 0.93f, 0.78f);
            lamp.shadows = LightShadows.None;
            lamp.renderMode = LightRenderMode.ForcePixel;

            // Sensors.
            VisionSensor vision = root.AddComponent<VisionSensor>();
            BuildUtils.SetFields(vision,
                ("_eye", eye.transform),
                ("_obstacleMask", StealthLayers.Mask(StealthLayers.Environment, 0)),
                ("_targetMask", StealthLayers.Mask(StealthLayers.Player)));

            GameObject zone = BuildUtils.Empty("ProximityZone", root.transform, new Vector3(0f, 0.9f, 0f));
            zone.layer = StealthLayers.Zone;
            SphereCollider sphere = zone.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 4.5f;
            ProximitySensor proximity = zone.AddComponent<ProximitySensor>();
            BuildUtils.SetField(proximity, "_targetMask", StealthLayers.Mask(StealthLayers.Player));

            GameObject coneGo = BuildUtils.Empty("VisionCone", root.transform);
            coneGo.layer = StealthLayers.VisionCone;
            coneGo.AddComponent<MeshFilter>();
            MeshRenderer coneRenderer = coneGo.AddComponent<MeshRenderer>();
            coneRenderer.sharedMaterial = AssetFactory.VisionCone;
            coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coneRenderer.receiveShadows = false;
            VisionCone cone = coneGo.AddComponent<VisionCone>();
            BuildUtils.SetField(cone, "_sensor", vision);

            // World space UI: alert badge plus speech bubble.
            (AlertIcon icon, SpeechBubble bubble) = BuildGuardUi(root.transform, 2.35f);

            // Logic.
            GuardPerception perception = root.AddComponent<GuardPerception>();
            BuildUtils.SetFields(perception, ("_vision", vision), ("_proximity", proximity));

            GuardVoice voice = root.AddComponent<GuardVoice>();
            BuildUtils.SetField(voice, "_bubble", bubble);

            GuardVisuals visuals = root.AddComponent<GuardVisuals>();
            BuildUtils.SetFields(visuals, ("_cone", cone), ("_icon", icon), ("_lamp", lamp));
            BuildUtils.SetArray(visuals, "_accentRenderers",
                visor.GetComponent<Renderer>(), shoulder.GetComponent<Renderer>());

            GuardController controller = root.AddComponent<GuardController>();
            BuildUtils.SetFields(controller,
                ("_perception", perception), ("_visuals", visuals), ("_voice", voice), ("_eye", eye.transform));

            Save(root, "Guard");
        }

        private static (AlertIcon, SpeechBubble) BuildGuardUi(Transform parent, float height)
        {
            GameObject canvasGo = new GameObject("GuardUI", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, height, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.01f;
            canvasGo.layer = StealthLayers.VisionCone;

            RectTransform canvasRect = (RectTransform)canvasGo.transform;
            canvasRect.sizeDelta = new Vector2(260f, 160f);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<Billboard>();

            // Alert badge.
            RectTransform iconRect = BuildUtils.Rect("AlertIcon", canvasGo.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            iconRect.sizeDelta = new Vector2(70f, 70f);
            iconRect.anchoredPosition = new Vector2(0f, 45f);
            CanvasGroup iconGroup = iconRect.gameObject.AddComponent<CanvasGroup>();
            AlertIcon icon = iconRect.gameObject.AddComponent<AlertIcon>();

            RectTransform scaleRoot = BuildUtils.Rect("ScaleRoot", iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image ring = BuildUtils.Sprite("Ring", scaleRoot, AssetFactory.LoadSprite("ring_thin"),
                new Color(1f, 1f, 1f, 0.45f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image fill = BuildUtils.Sprite("Fill", scaleRoot, AssetFactory.LoadSprite("ring"), Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;

            TextMeshProUGUI symbol = BuildUtils.Label("Symbol", scaleRoot, "?", 46f, TextAlignmentOptions.Center,
                Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            symbol.fontStyle = FontStyles.Bold;

            BuildUtils.SetFields(icon,
                ("_group", iconGroup), ("_fill", fill), ("_ring", ring),
                ("_symbol", symbol), ("_scaleRoot", scaleRoot));

            // Speech bubble.
            RectTransform bubbleRect = BuildUtils.Rect("SpeechBubble", canvasGo.transform, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            bubbleRect.sizeDelta = new Vector2(420f, 60f);
            bubbleRect.anchoredPosition = new Vector2(0f, 10f);
            CanvasGroup bubbleGroup = bubbleRect.gameObject.AddComponent<CanvasGroup>();
            SpeechBubble bubble = bubbleRect.gameObject.AddComponent<SpeechBubble>();

            TextMeshProUGUI bubbleLabel = BuildUtils.Label("Label", bubbleRect, string.Empty, 34f,
                TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bubbleLabel.fontStyle = FontStyles.Italic;

            BuildUtils.SetFields(bubble, ("_group", bubbleGroup), ("_label", bubbleLabel));

            return (icon, bubble);
        }

        // ----- security camera --------------------------------------------------------------------

        private static void BuildSecurityCamera()
        {
            GameObject root = new GameObject("SecurityCamera") { layer = StealthLayers.Enemy };

            BuildUtils.Primitive(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 2.1f, 0f),
                new Vector3(0.14f, 2.1f, 0.14f), AssetFactory.Metal, StealthLayers.Environment);
            BuildUtils.Box("Arm", root.transform, new Vector3(0f, 4.15f, 0.25f), new Vector3(0.12f, 0.12f, 0.6f),
                AssetFactory.Metal, StealthLayers.Environment);

            GameObject head = BuildUtils.Empty("Head", root.transform, new Vector3(0f, 4f, 0.5f));
            head.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

            BuildUtils.Box("Housing", head.transform, Vector3.zero, new Vector3(0.42f, 0.36f, 0.72f),
                AssetFactory.Truck, StealthLayers.Enemy, false);
            GameObject lens = BuildUtils.Primitive(PrimitiveType.Sphere, "Lens", head.transform, new Vector3(0f, 0f, 0.38f),
                new Vector3(0.22f, 0.22f, 0.22f), AssetFactory.GuardAccent, StealthLayers.Enemy, false);

            GameObject eye = BuildUtils.Empty("Eye", head.transform, new Vector3(0f, 0f, 0.42f));

            GameObject lampGo = BuildUtils.Empty("Beam", head.transform, new Vector3(0f, 0f, 0.4f));
            Light lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.range = 20f;
            lamp.spotAngle = 42f;
            lamp.intensity = 1.6f;
            lamp.color = new Color(0.7f, 0.95f, 1f);
            lamp.shadows = LightShadows.None;

            VisionSensor vision = root.AddComponent<VisionSensor>();
            BuildUtils.SetFields(vision,
                ("_eye", eye.transform),
                ("_obstacleMask", StealthLayers.Mask(StealthLayers.Environment, 0)),
                ("_targetMask", StealthLayers.Mask(StealthLayers.Player)),
                ("_viewDistance", 17f),
                ("_viewAngle", 42f));

            GameObject coneGo = BuildUtils.Empty("VisionCone", root.transform);
            coneGo.layer = StealthLayers.VisionCone;
            coneGo.AddComponent<MeshFilter>();
            MeshRenderer coneRenderer = coneGo.AddComponent<MeshRenderer>();
            coneRenderer.sharedMaterial = AssetFactory.VisionCone;
            coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coneRenderer.receiveShadows = false;
            VisionCone cone = coneGo.AddComponent<VisionCone>();
            BuildUtils.SetFields(cone, ("_sensor", vision), ("_probeHeight", 1.6f));

            (AlertIcon icon, SpeechBubble _) = BuildGuardUi(root.transform, 5.1f);

            GuardPerception perception = root.AddComponent<GuardPerception>();
            BuildUtils.SetFields(perception,
                ("_vision", vision), ("_hearingRadius", 0.1f), ("_noiseSensitivity", 0f),
                ("_meter._riseSpeed", 0.55f), ("_meter._decaySpeed", 0.5f));

            GuardVisuals visuals = root.AddComponent<GuardVisuals>();
            BuildUtils.SetFields(visuals, ("_cone", cone), ("_icon", icon), ("_lamp", lamp));
            BuildUtils.SetArray(visuals, "_accentRenderers", lens.GetComponent<Renderer>());

            SecurityCamera camera = root.AddComponent<SecurityCamera>();
            BuildUtils.SetFields(camera,
                ("_perception", perception), ("_head", head.transform), ("_visuals", visuals));

            Save(root, "SecurityCamera");
        }

        // ----- props ------------------------------------------------------------------------------

        private static GameObject BuildStone()
        {
            GameObject root = BuildUtils.Primitive(PrimitiveType.Sphere, "Stone", null, Vector3.zero,
                new Vector3(0.18f, 0.18f, 0.18f), AssetFactory.Stone, StealthLayers.Throwable);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 0.4f;
            body.linearDamping = 0.02f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            root.AddComponent<Stone>();
            return Save(root, "Stone");
        }

        private static void BuildHidingSpot()
        {
            GameObject root = new GameObject("HidingSpot") { layer = StealthLayers.Environment };

            // Open topped bin: four walls and a floor, so the camera can look down into it.
            GameObject visual = BuildUtils.Empty("Visual", root.transform);
            BuildUtils.Box("Floor", visual.transform, new Vector3(0f, 0.06f, 0f), new Vector3(2f, 0.12f, 1.5f),
                AssetFactory.Metal, StealthLayers.Environment);
            BuildUtils.Box("WallBack", visual.transform, new Vector3(0f, 0.75f, -0.69f), new Vector3(2f, 1.5f, 0.12f),
                AssetFactory.ContainerRust, StealthLayers.Environment);
            BuildUtils.Box("WallFront", visual.transform, new Vector3(0f, 0.75f, 0.69f), new Vector3(2f, 1.5f, 0.12f),
                AssetFactory.ContainerRust, StealthLayers.Environment);
            BuildUtils.Box("WallLeft", visual.transform, new Vector3(-0.94f, 0.75f, 0f), new Vector3(0.12f, 1.5f, 1.5f),
                AssetFactory.ContainerRust, StealthLayers.Environment);
            BuildUtils.Box("WallRight", visual.transform, new Vector3(0.94f, 0.75f, 0f), new Vector3(0.12f, 1.5f, 1.5f),
                AssetFactory.ContainerRust, StealthLayers.Environment);
            BuildUtils.Box("Stripe", visual.transform, new Vector3(0f, 1.42f, 0.7f), new Vector3(2.02f, 0.16f, 0.14f),
                AssetFactory.Hazard, StealthLayers.Environment, false);

            GameObject hidePoint = BuildUtils.Empty("HidePoint", root.transform, new Vector3(0f, 0.15f, 0f));
            GameObject exitPoint = BuildUtils.Empty("ExitPoint", root.transform, new Vector3(0f, 0f, 1.6f));

            GameObject trigger = BuildUtils.Trigger("Interact", root.transform, new Vector3(0f, 1f, 0f),
                new Vector3(3.2f, 2.4f, 3f), StealthLayers.Interactable);

            HidingSpot spot = root.AddComponent<HidingSpot>();
            BuildUtils.SetFields(spot,
                ("_hidePoint", hidePoint.transform), ("_exitPoint", exitPoint.transform), ("_displayName", "container"));

            Save(root, "HidingSpot");
        }

        private static void BuildStonePile()
        {
            GameObject root = new GameObject("StonePile") { layer = StealthLayers.Environment };

            GameObject visual = BuildUtils.Empty("Visual", root.transform);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Stone1", visual.transform, new Vector3(0f, 0.14f, 0f),
                new Vector3(0.28f, 0.24f, 0.28f), AssetFactory.Stone, StealthLayers.Environment, false);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Stone2", visual.transform, new Vector3(0.22f, 0.1f, 0.12f),
                new Vector3(0.22f, 0.2f, 0.22f), AssetFactory.Stone, StealthLayers.Environment, false);
            BuildUtils.Primitive(PrimitiveType.Sphere, "Stone3", visual.transform, new Vector3(-0.16f, 0.09f, 0.18f),
                new Vector3(0.2f, 0.18f, 0.2f), AssetFactory.Stone, StealthLayers.Environment, false);
            GameObject glow = BuildUtils.Primitive(PrimitiveType.Quad, "Glow", visual.transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(2.4f, 2.4f, 1f), AssetFactory.PickupGlow, StealthLayers.VisionCone, false);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            BuildUtils.Trigger("Interact", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(2.6f, 2f, 2.6f),
                StealthLayers.Interactable);

            StonePile pile = root.AddComponent<StonePile>();
            BuildUtils.SetField(pile, "_visualRoot", visual);

            Save(root, "StonePile");
        }

        private static void BuildIntel()
        {
            GameObject root = new GameObject("IntelPickup") { layer = StealthLayers.Environment };

            GameObject visual = BuildUtils.Empty("Visual", root.transform, new Vector3(0f, 0.55f, 0f));
            GameObject spinner = BuildUtils.Empty("Spinner", visual.transform);
            GameObject box = BuildUtils.Box("Case", spinner.transform, Vector3.zero, new Vector3(0.42f, 0.3f, 0.12f),
                AssetFactory.IntelGlow, StealthLayers.Environment, false);
            box.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            GameObject lightGo = BuildUtils.Empty("Glow", visual.transform);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.78f, 0.35f);
            light.shadows = LightShadows.None;

            BuildUtils.Trigger("Interact", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(2.2f, 2f, 2.2f),
                StealthLayers.Interactable);

            Beacon beacon = root.AddComponent<Beacon>();
            BuildUtils.SetFields(beacon,
                ("_spinner", spinner.transform), ("_pulseRoot", visual.transform), ("_light", light),
                ("_spinSpeed", 55f), ("_pulseAmount", 0.08f), ("_minIntensity", 0.9f), ("_maxIntensity", 1.8f));

            IntelPickup intel = root.AddComponent<IntelPickup>();
            BuildUtils.SetField(intel, "_visualRoot", visual);

            Save(root, "IntelPickup");
        }

        private static void BuildLightSwitch()
        {
            GameObject root = new GameObject("LightSwitch") { layer = StealthLayers.Environment };

            BuildUtils.Box("Post", root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.14f, 1.1f, 0.14f),
                AssetFactory.Metal, StealthLayers.Environment);
            BuildUtils.Box("Box", root.transform, new Vector3(0f, 1.12f, 0f), new Vector3(0.5f, 0.6f, 0.26f),
                AssetFactory.Truck, StealthLayers.Environment);
            GameObject indicator = BuildUtils.Box("Indicator", root.transform, new Vector3(0f, 1.3f, 0.15f),
                new Vector3(0.16f, 0.16f, 0.06f), AssetFactory.LampGlow, StealthLayers.Environment, false);

            BuildUtils.Trigger("Interact", root.transform, new Vector3(0f, 1f, 0f), new Vector3(2.4f, 2.4f, 2.4f),
                StealthLayers.Interactable);

            LightSwitch lightSwitch = root.AddComponent<LightSwitch>();
            BuildUtils.SetField(lightSwitch, "_indicatorRenderer", indicator.GetComponent<Renderer>());

            Save(root, "LightSwitch");
        }

        private static void BuildStreetLamp()
        {
            GameObject root = new GameObject("StreetLamp") { layer = StealthLayers.Environment };

            BuildUtils.Primitive(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 2.4f, 0f),
                new Vector3(0.16f, 2.4f, 0.16f), AssetFactory.LampHousing, StealthLayers.Environment);
            BuildUtils.Box("Arm", root.transform, new Vector3(0f, 4.75f, 0.35f), new Vector3(0.14f, 0.14f, 0.9f),
                AssetFactory.LampHousing, StealthLayers.Environment, false);
            GameObject bulb = BuildUtils.Box("Bulb", root.transform, new Vector3(0f, 4.6f, 0.75f),
                new Vector3(0.55f, 0.18f, 0.55f), AssetFactory.LampGlow, StealthLayers.Environment, false);

            GameObject lightGo = BuildUtils.Empty("Light", root.transform, new Vector3(0f, 4.5f, 0.75f));
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 13f;
            light.spotAngle = 88f;
            light.intensity = 3.2f;
            light.color = new Color(1f, 0.9f, 0.72f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;

            GameObject pool = BuildUtils.Primitive(PrimitiveType.Quad, "LightPool", root.transform,
                new Vector3(0f, 0.03f, 0.75f), new Vector3(10f, 10f, 1f), AssetFactory.LampPool, StealthLayers.VisionCone, false);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject zoneGo = BuildUtils.Trigger("LitZone", root.transform, new Vector3(0f, 1.2f, 0.75f),
                new Vector3(7.5f, 3.5f, 7.5f));
            LightZone zone = zoneGo.AddComponent<LightZone>();

            Save(root, "StreetLamp");
        }

        private static void BuildExtractionPoint()
        {
            GameObject root = new GameObject("ExtractionPoint") { layer = StealthLayers.Environment };

            // Van.
            GameObject van = BuildUtils.Empty("Van", root.transform, new Vector3(0f, 0f, 3.4f));
            BuildUtils.Box("Body", van.transform, new Vector3(0f, 1.35f, 0f), new Vector3(2.3f, 1.9f, 5f),
                AssetFactory.Truck, StealthLayers.Environment);
            BuildUtils.Box("Cabin", van.transform, new Vector3(0f, 1.9f, 1.9f), new Vector3(2.1f, 1f, 1.4f),
                AssetFactory.Metal, StealthLayers.Environment, false);
            foreach (Vector3 offset in new[]
                     {
                         new Vector3(1.1f, 0.42f, 1.6f), new Vector3(-1.1f, 0.42f, 1.6f),
                         new Vector3(1.1f, 0.42f, -1.6f), new Vector3(-1.1f, 0.42f, -1.6f)
                     })
            {
                GameObject wheel = BuildUtils.Primitive(PrimitiveType.Cylinder, "Wheel", van.transform, offset,
                    new Vector3(0.42f, 0.14f, 0.42f), AssetFactory.LampHousing, StealthLayers.Environment, false);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Landing pad.
            GameObject pad = BuildUtils.Primitive(PrimitiveType.Quad, "Pad", root.transform, new Vector3(0f, 0.04f, 0f),
                new Vector3(9f, 9f, 1f), AssetFactory.GroundGlow, StealthLayers.VisionCone, false);
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject pulseRoot = BuildUtils.Empty("Beacon", root.transform, new Vector3(0f, 0.1f, 0f));
            GameObject spinner = BuildUtils.Empty("Spinner", pulseRoot.transform);
            BuildUtils.Box("Blade", spinner.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 0.18f),
                AssetFactory.BeaconGlow, StealthLayers.Environment, false);
            BuildUtils.Box("Blade2", spinner.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.6f, 0.18f, 0.18f),
                AssetFactory.BeaconGlow, StealthLayers.Environment, false);

            GameObject lightGo = BuildUtils.Empty("Light", pulseRoot.transform, new Vector3(0f, 1.6f, 0f));
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 1.8f;
            light.color = new Color(0.4f, 1f, 0.7f);
            light.shadows = LightShadows.None;

            Beacon beacon = root.AddComponent<Beacon>();
            BuildUtils.SetFields(beacon,
                ("_spinner", spinner.transform), ("_pulseRoot", pulseRoot.transform), ("_light", light));

            AudioSource hum = root.AddComponent<AudioSource>();
            hum.clip = AssetFactory.Clip("goal_hum");
            hum.loop = true;
            hum.playOnAwake = true;
            hum.spatialBlend = 1f;
            hum.volume = 0.35f;
            hum.rolloffMode = AudioRolloffMode.Linear;
            hum.maxDistance = 26f;

            GameObject trigger = BuildUtils.Trigger("GoalTrigger", root.transform, new Vector3(0f, 1.2f, 0f),
                new Vector3(5f, 2.4f, 5f));
            trigger.AddComponent<LevelGoal>();

            Save(root, "ExtractionPoint");
        }
    }
}
