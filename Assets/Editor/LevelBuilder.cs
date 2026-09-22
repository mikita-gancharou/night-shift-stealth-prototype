using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Stealth.Audio;
using Stealth.Core;
using Stealth.Enemies;
using Stealth.Interaction;
using Stealth.Level;
using Stealth.Player;

namespace Stealth.EditorTools
{
    /// <summary>
    /// Generates the whole mission scene: the depot geometry, lighting, guards with their routes,
    /// interactables, HUD and the navigation mesh. Re-running it rebuilds the level from scratch.
    /// </summary>
    public static class LevelBuilder
    {
        private const float WallHeight = 4.5f;
        private const float CoverHeight = 1.25f;

        private static Transform _geometry;
        private static Transform _props;
        private static Transform _lights;
        private static Transform _zones;
        private static Transform _enemies;
        private static Transform _routes;
        private static Transform _interactables;

        [MenuItem("Stealth/Step 4 - Build Level Scene", priority = 4)]
        public static void Build()
        {
            AssetFactory.LoadMaterials();
            Groups.Clear();
            GroupLights.Clear();
            GroupBulbs.Clear();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureLighting();

            GameObject systems = new GameObject("--- Systems ---");
            GameObject levelRoot = new GameObject("--- Level ---");
            GameObject gameplay = new GameObject("--- Gameplay ---");

            _geometry = new GameObject("Geometry").transform;
            _props = new GameObject("Props").transform;
            _lights = new GameObject("Lights").transform;
            _zones = new GameObject("Zones").transform;
            _geometry.SetParent(levelRoot.transform);
            _props.SetParent(levelRoot.transform);
            _lights.SetParent(levelRoot.transform);
            _zones.SetParent(levelRoot.transform);

            _enemies = new GameObject("Enemies").transform;
            _routes = new GameObject("Routes").transform;
            _interactables = new GameObject("Interactables").transform;
            _enemies.SetParent(gameplay.transform);
            _routes.SetParent(gameplay.transform);
            _interactables.SetParent(gameplay.transform);

            BuildSystems(systems.transform);
            BuildGround();
            BuildPerimeter();
            BuildStartArea();
            BuildContainerYard();
            BuildWestAlley();
            BuildMainYard();
            BuildWarehouse();
            BuildNorthYard();

            LevelGoal goal = BuildExtraction();
            BuildPlayerAndCamera(gameplay.transform);
            BuildNavMesh(levelRoot.transform);

            GameObject hud = HudBuilder.Build(goal);
            hud.transform.SetParent(systems.transform);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(systems.transform);

            BuildUtils.EnsureFolder(StealthPaths.Scenes);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StealthPaths.LevelScene);

            Debug.Log("[Stealth] Level scene built at " + StealthPaths.LevelScene);
        }

        // ----- systems ----------------------------------------------------------------------------

        private static void BuildSystems(Transform parent)
        {
            GameObject manager = new GameObject("GameManager");
            manager.transform.SetParent(parent);
            manager.AddComponent<GameManager>();

            AudioManager audio = manager.AddComponent<AudioManager>();
            BuildUtils.SetField(audio, "_library", AssetFactory.Sounds);

            GameObject musicGo = new GameObject("Music");
            musicGo.transform.SetParent(manager.transform);
            AudioSource ambient = musicGo.AddComponent<AudioSource>();
            AudioSource tension = musicGo.AddComponent<AudioSource>();
            AudioSource chase = musicGo.AddComponent<AudioSource>();
            foreach (AudioSource source in new[] { ambient, tension, chase })
            {
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
            }

            MusicDirector music = musicGo.AddComponent<MusicDirector>();
            BuildUtils.SetFields(music,
                ("_library", AssetFactory.Sounds), ("_ambient", ambient), ("_tension", tension), ("_chase", chase));
        }

        private static void ConfigureLighting()
        {
            GameObject sunGo = new GameObject("Moonlight");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.62f, 0.71f, 0.95f);
            sun.intensity = 0.6f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sunGo.transform.rotation = Quaternion.Euler(38f, -36f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetFactory.NightSky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.19f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.09f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 105f;

            try
            {
                LightingSettings lighting = new LightingSettings
                {
                    name = "NightShift Lighting",
                    bakedGI = false,
                    realtimeGI = false,
                    autoGenerate = false
                };
                Lightmapping.lightingSettings = lighting;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Stealth] Could not apply lighting settings: " + exception.Message);
            }
        }

        // ----- geometry ---------------------------------------------------------------------------

        private static void BuildGround()
        {
            BuildUtils.Box("Ground", _geometry, new Vector3(0f, -0.5f, 2f), new Vector3(70f, 1f, 78f), AssetFactory.Ground);
        }

        private static void BuildPerimeter()
        {
            Wall("Wall_South", new Vector2(0f, -36f), new Vector3(70f, 6f, 1f));
            Wall("Wall_North", new Vector2(0f, 40f), new Vector3(70f, 6f, 1f));
            Wall("Wall_West", new Vector2(-34f, 2f), new Vector3(1f, 6f, 78f));
            Wall("Wall_East", new Vector2(34f, 2f), new Vector3(1f, 6f, 78f));
        }

        private static void BuildStartArea()
        {
            // Divider with a west and an east opening: the first route choice of the level.
            Wall("Wall_StartA", new Vector2(-29f, -26f), new Vector3(10f, WallHeight, 0.8f));
            Wall("Wall_StartB", new Vector2(-6f, -26f), new Vector3(26f, WallHeight, 0.8f));
            Wall("Wall_StartC", new Vector2(23f, -26f), new Vector3(22f, WallHeight, 0.8f));

            Crate(new Vector2(-8f, -31f), 1.4f);
            Crate(new Vector2(-7f, -29.4f), 1.4f);
            Crate(new Vector2(9f, -30f), 1.6f);
            Container(new Vector2(16f, -31f), 0f, AssetFactory.ContainerBlue);

            Lamp("Lamp_Start", new Vector2(-5f, -31f), 90f, null);

            StonePile(new Vector2(-4f, -31f));

            Hint(new Vector2(0f, -30f), new Vector3(18f, 4f, 7f),
                "Get out of the depot: reach the extraction point in the north.\nWASD to move, mouse to look around.");
            Hint(new Vector2(0f, -23.5f), new Vector3(44f, 4f, 3f),
                "SHIFT sprints but is loud.  C crouches: slower, quieter, and low enough to hide behind crates.");
        }

        private static void BuildContainerYard()
        {
            Container(new Vector2(-16f, -22f), 0f, AssetFactory.ContainerTeal);
            Container(new Vector2(-8f, -20f), 90f, AssetFactory.ContainerRust);
            Container(new Vector2(-8f, -20f), 90f, AssetFactory.ContainerBlue, 2.7f);
            Container(new Vector2(-16f, -14f), 90f, AssetFactory.ContainerRust);
            Container(new Vector2(-4f, -14f), 0f, AssetFactory.ContainerBlue);
            Container(new Vector2(5f, -20f), 90f, AssetFactory.ContainerTeal);
            Container(new Vector2(11f, -15f), 0f, AssetFactory.ContainerRust);
            Container(new Vector2(-25f, -16f), 0f, AssetFactory.ContainerBlue);

            // Low cover: crouching drops the player's head below these.
            CoverWall("Cover_YardA", new Vector2(-11.5f, -18f), new Vector3(5f, CoverHeight, 1f));
            CoverWall("Cover_YardB", new Vector2(1f, -17.5f), new Vector3(6f, CoverHeight, 1f));
            CoverWall("Cover_YardC", new Vector2(7f, -11f), new Vector3(1f, CoverHeight, 5f));
            Crate(new Vector2(-26f, -11f), 1.2f);
            Crate(new Vector2(-25f, -12f), 1.2f);

            Lamp("Lamp_Yard", new Vector2(-2f, -22f), 0f, null);

            HidingSpot(new Vector2(-12.5f, -10.5f), 0f, "container");

            Hint(new Vector2(-21f, -24f), new Vector3(8f, 4f, 4f),
                "Guards only see inside their cone. Watch the badge above their head: it fills up before they spot you.");

            // Mid wall: west passage, centre gate (watched by a camera) and the warehouse door.
            Wall("Wall_MidA", new Vector2(-33f, -8f), new Vector3(2f, WallHeight, 0.8f));
            Wall("Wall_MidB", new Vector2(-14.5f, -8f), new Vector3(25f, WallHeight, 0.8f));
            Wall("Wall_MidC", new Vector2(9f, -8f), new Vector3(14f, WallHeight, 0.8f));
            Wall("Wall_MidD", new Vector2(27f, -8f), new Vector3(14f, WallHeight, 0.8f));

            Guard("Guard_Alpha", AssetFactory.PatrolGuardProfile, new Vector2(-21f, -23f), 0f, PatrolRoute.RouteMode.Loop,
                (new Vector2(-21f, -23f), 2f, true),
                (new Vector2(-21f, -10.5f), 1.5f, false),
                (new Vector2(-4f, -10.5f), 2.5f, true),
                (new Vector2(-4f, -22f), 1.5f, false));
        }

        private static void BuildWestAlley()
        {
            // Wall that separates the dark alley from the lit main yard, with one opening.
            Wall("Wall_AlleyA", new Vector2(-24f, -1f), new Vector3(0.8f, WallHeight, 14f));
            Wall("Wall_AlleyB", new Vector2(-24f, 16f), new Vector3(0.8f, WallHeight, 12f));

            Crate(new Vector2(-26f, 0f), 1.2f);
            Crate(new Vector2(-31f, 11f), 1.2f);
            Container(new Vector2(-31f, -4f), 90f, AssetFactory.ContainerRust);

            Intel(new Vector2(-31f, 6f));
            LightSwitch(new Vector2(-30.5f, 14f), 90f, "YardLights");

            Guard("Guard_Bravo", AssetFactory.PatrolGuardProfile, new Vector2(-28f, -3f), 0f, PatrolRoute.RouteMode.PingPong,
                (new Vector2(-28f, -3f), 2f, true),
                (new Vector2(-28f, 9f), 1f, false),
                (new Vector2(-28f, 18f), 3f, true));
        }

        private static void BuildMainYard()
        {
            Truck(new Vector2(-10f, 8f), 0f);
            Truck(new Vector2(6f, -4f), 90f);

            Container(new Vector2(-18f, 4f), 0f, AssetFactory.ContainerTeal);
            Container(new Vector2(-6f, 18f), 90f, AssetFactory.ContainerBlue);
            Container(new Vector2(9f, 12f), 0f, AssetFactory.ContainerRust);

            CoverWall("Cover_MainA", new Vector2(-13f, -3f), new Vector3(8f, CoverHeight, 1f));
            CoverWall("Cover_MainB", new Vector2(2f, 6f), new Vector3(1f, CoverHeight, 7f));
            CoverWall("Cover_MainC", new Vector2(-16f, 14f), new Vector3(6f, CoverHeight, 1f));
            Crate(new Vector2(11f, 3f), 1.3f);
            Crate(new Vector2(10f, 4.2f), 1.3f);
            Crate(new Vector2(-2f, 20f), 1.3f);

            Lamp("Lamp_YardWest", new Vector2(-14f, 2f), 0f, "YardLights");
            Lamp("Lamp_YardEast", new Vector2(4f, 14f), 180f, "YardLights");

            HidingSpot(new Vector2(-20f, 12f), 90f, "dumpster");
            StonePile(new Vector2(11f, 0f));
            Intel(new Vector2(6f, 8f));

            SecurityCamera("Camera_Gate", new Vector2(-1.5f, -5.5f), 0f);

            Guard("Guard_Charlie", AssetFactory.PatrolGuardProfile, new Vector2(-18f, -2f), 90f, PatrolRoute.RouteMode.Loop,
                (new Vector2(-18f, -2f), 1.5f, false),
                (new Vector2(10f, -2f), 2f, true),
                (new Vector2(10f, 17f), 1.5f, false),
                (new Vector2(-18f, 16f), 2.5f, true));

            Hint(new Vector2(-21.5f, 8f), new Vector3(5f, 4f, 5f),
                "Cameras cannot chase you, but they call every guard nearby. Cut the power or slip through the dark.");
        }

        private static void BuildWarehouse()
        {
            Wall("Wall_WhWestA", new Vector2(14f, -4f), new Vector3(0.8f, 5f, 8f));
            Wall("Wall_WhWestB", new Vector2(14f, 13f), new Vector3(0.8f, 5f, 18f));
            Wall("Wall_WhPartition", new Vector2(28f, 8f), new Vector3(12f, 5f, 0.8f));

            // Shelves.
            for (int i = 0; i < 3; i++)
            {
                float z = -5f + i * 5f;
                BuildUtils.Box($"Shelf_{i}", _props, new Vector3(23f, 1.1f, z), new Vector3(9f, 2.2f, 1.2f),
                    AssetFactory.Crate);
            }

            Crate(new Vector2(17f, 11f), 1.4f);
            Crate(new Vector2(17f, 12.4f), 1.4f);
            Crate(new Vector2(30f, 3f), 1.4f);
            Container(new Vector2(30f, 19f), 0f, AssetFactory.ContainerTeal);

            // Metal walkway: crossing it at speed is very loud.
            Surface("Surface_Metal", new Vector2(24f, 1.5f), new Vector3(16f, 3f, 8f), SurfaceKind.Metal, 2.1f,
                AssetFactory.GroundMetal);

            HidingSpot(new Vector2(31f, 14f), -90f, "locker");
            Intel(new Vector2(31f, -5f));
            StonePile(new Vector2(17f, 18f));

            Guard("Guard_Delta", AssetFactory.WatchmanProfile, new Vector2(17f, -3f), 0f, PatrolRoute.RouteMode.Loop,
                (new Vector2(17f, -3f), 2f, true),
                (new Vector2(18f, 18f), 1.5f, false),
                (new Vector2(30f, 15f), 2f, true),
                (new Vector2(30f, -4f), 1.5f, false));

            Hint(new Vector2(18f, 2f), new Vector3(4f, 4f, 4f),
                "Metal grating rattles under your boots. Crouch to cross it quietly.");
        }

        private static void BuildNorthYard()
        {
            Wall("Wall_NorthA", new Vector2(-33f, 22f), new Vector3(2f, WallHeight, 0.8f));
            Wall("Wall_NorthB", new Vector2(-16f, 22f), new Vector3(24f, WallHeight, 0.8f));
            Wall("Wall_NorthC", new Vector2(12f, 22f), new Vector3(24f, WallHeight, 0.8f));
            Wall("Wall_NorthD", new Vector2(31f, 22f), new Vector3(6f, WallHeight, 0.8f));

            Surface("Surface_Gravel", new Vector2(0f, 29f), new Vector3(32f, 3f, 11f), SurfaceKind.Gravel, 1.8f,
                AssetFactory.GroundGravel);

            Container(new Vector2(-11f, 31f), 0f, AssetFactory.ContainerRust);
            Container(new Vector2(11f, 31f), 0f, AssetFactory.ContainerBlue);
            Container(new Vector2(-19f, 27f), 90f, AssetFactory.ContainerTeal);
            Container(new Vector2(19f, 27f), 90f, AssetFactory.ContainerRust);
            CoverWall("Cover_NorthA", new Vector2(-5f, 25f), new Vector3(6f, CoverHeight, 1f));
            CoverWall("Cover_NorthB", new Vector2(6f, 34f), new Vector3(7f, CoverHeight, 1f));

            Lamp("Lamp_GateWest", new Vector2(-7f, 24f), 0f, "GateLights");
            Lamp("Lamp_GateEast", new Vector2(7f, 24f), 0f, "GateLights");
            LightSwitch(new Vector2(29f, 24.5f), 180f, "GateLights");

            HidingSpot(new Vector2(16f, 33f), 180f, "crate bin");

            // Stationary sentry watching the whole approach with a long, narrow cone.
            Guard("Guard_Echo", AssetFactory.SentryProfile, new Vector2(0f, 30f), 180f, PatrolRoute.RouteMode.Loop);
            SecurityCamera("Camera_North", new Vector2(-13f, 25f), 25f);

            Hint(new Vector2(0f, 23.5f), new Vector3(34f, 4f, 3f),
                "Gravel is loud. Crouch across it, throw a stone to pull the sentry away, or cut the gate lights first.");
        }

        private static LevelGoal BuildExtraction()
        {
            GameObject prefab = PrefabFactory.Load("ExtractionPoint");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 0f, 34f);
            instance.transform.SetParent(_props.parent);

            return instance.GetComponentInChildren<LevelGoal>();
        }

        // ----- gameplay objects -------------------------------------------------------------------

        private static void BuildPlayerAndCamera(Transform parent)
        {
            GameObject prefab = PrefabFactory.Load("Player");
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -29f), Quaternion.identity);

            GameObject cameraGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(parent);
            cameraGo.transform.position = new Vector3(0f, 3f, -34f);

            Camera camera = cameraGo.GetComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 260f;
            camera.clearFlags = CameraClearFlags.Skybox;

            ThirdPersonCamera follow = cameraGo.AddComponent<ThirdPersonCamera>();
            BuildUtils.SetFields(follow,
                ("_target", player.transform),
                ("_collisionMask", StealthLayers.Mask(StealthLayers.Environment, 0)));
        }

        private static void BuildNavMesh(Transform parent)
        {
            GameObject surfaceGo = new GameObject("NavMeshSurface");
            surfaceGo.transform.SetParent(parent);

            NavMeshSurface surface = surfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = StealthLayers.Mask(StealthLayers.Environment, 0);
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[Stealth] NavMesh build produced no data.");
                return;
            }

            AssetDatabase.CreateAsset(surface.navMeshData, StealthPaths.NavMeshAsset);
            AssetDatabase.SaveAssets();
        }

        // ----- building blocks --------------------------------------------------------------------

        private static void Wall(string name, Vector2 xz, Vector3 size)
        {
            BuildUtils.Box(name, _geometry, new Vector3(xz.x, size.y * 0.5f, xz.y), size, AssetFactory.Wall);
        }

        private static void CoverWall(string name, Vector2 xz, Vector3 size)
        {
            BuildUtils.Box(name, _props, new Vector3(xz.x, size.y * 0.5f, xz.y), size, AssetFactory.Concrete);
        }

        private static void Crate(Vector2 xz, float size)
        {
            BuildUtils.Box("Crate", _props, new Vector3(xz.x, size * 0.5f, xz.y),
                new Vector3(size, size, size), AssetFactory.Crate);
        }

        private static void Container(Vector2 xz, float yaw, Material material, float y = 0f)
        {
            GameObject go = BuildUtils.Box("Container", _props, new Vector3(xz.x, y + 1.35f, xz.y),
                new Vector3(6.2f, 2.7f, 2.5f), material);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void Truck(Vector2 xz, float yaw)
        {
            GameObject root = BuildUtils.Empty("Truck", _props, new Vector3(xz.x, 0f, xz.y),
                Quaternion.Euler(0f, yaw, 0f));
            BuildUtils.Box("Body", root.transform, new Vector3(0f, 1.5f, -0.6f), new Vector3(2.5f, 2.4f, 5f),
                AssetFactory.Truck);
            BuildUtils.Box("Cabin", root.transform, new Vector3(0f, 1.1f, 2.4f), new Vector3(2.3f, 1.8f, 1.9f),
                AssetFactory.Metal);
        }

        private static void Surface(string name, Vector2 xz, Vector3 size, SurfaceKind kind, float multiplier,
            Material material)
        {
            BuildUtils.Box(name + "_Floor", _geometry, new Vector3(xz.x, 0.01f, xz.y),
                new Vector3(size.x, 0.02f, size.z), material, StealthLayers.Environment, false);

            GameObject zone = BuildUtils.Trigger(name, _zones, new Vector3(xz.x, size.y * 0.5f, xz.y),
                new Vector3(size.x, size.y, size.z));
            SurfaceZone surface = zone.AddComponent<SurfaceZone>();
            BuildUtils.SetFields(surface, ("_surface", (int)kind), ("_noiseMultiplier", multiplier));
        }

        private static void Hint(Vector2 xz, Vector3 size, string message)
        {
            GameObject zone = BuildUtils.Trigger("Hint", _zones, new Vector3(xz.x, size.y * 0.5f, xz.y), size);
            TutorialTrigger trigger = zone.AddComponent<TutorialTrigger>();
            BuildUtils.SetFields(trigger, ("_message", message), ("_duration", 7f));
        }

        private static readonly Dictionary<string, LightGroup> Groups = new Dictionary<string, LightGroup>();
        private static readonly Dictionary<string, List<Light>> GroupLights = new Dictionary<string, List<Light>>();
        private static readonly Dictionary<string, List<Renderer>> GroupBulbs = new Dictionary<string, List<Renderer>>();

        private static void Lamp(string name, Vector2 xz, float yaw, string groupName)
        {
            GameObject prefab = PrefabFactory.Load("StreetLamp");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _lights);
            instance.name = name;
            instance.transform.SetPositionAndRotation(new Vector3(xz.x, 0f, xz.y), Quaternion.Euler(0f, yaw, 0f));

            if (string.IsNullOrEmpty(groupName)) return;

            LightGroup group = GetOrCreateGroup(groupName);
            BuildUtils.SetField(instance.GetComponentInChildren<LightZone>(), "_group", group);

            Light light = instance.GetComponentInChildren<Light>();
            Renderer bulb = FindChildRenderer(instance.transform, "Bulb");
            if (light != null) GroupLights[groupName].Add(light);
            if (bulb != null) GroupBulbs[groupName].Add(bulb);

            BuildUtils.SetArray(group, "_lights", GroupLights[groupName].ToArray());
            BuildUtils.SetArray(group, "_lampRenderers", GroupBulbs[groupName].ToArray());
        }

        private static LightGroup GetOrCreateGroup(string groupName)
        {
            if (Groups.TryGetValue(groupName, out LightGroup existing) && existing != null) return existing;

            GameObject go = new GameObject(groupName);
            go.transform.SetParent(_lights);
            LightGroup group = go.AddComponent<LightGroup>();

            Groups[groupName] = group;
            GroupLights[groupName] = new List<Light>();
            GroupBulbs[groupName] = new List<Renderer>();
            return group;
        }

        private static Renderer FindChildRenderer(Transform root, string name)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == name) return renderer;
            }

            return null;
        }

        private static void LightSwitch(Vector2 xz, float yaw, string groupName)
        {
            GameObject prefab = PrefabFactory.Load("LightSwitch");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _interactables);
            instance.name = "LightSwitch_" + groupName;
            instance.transform.SetPositionAndRotation(new Vector3(xz.x, 0f, xz.y), Quaternion.Euler(0f, yaw, 0f));

            BuildUtils.SetField(instance.GetComponent<Stealth.Interaction.LightSwitch>(), "_group",
                GetOrCreateGroup(groupName));
        }

        private static void HidingSpot(Vector2 xz, float yaw, string displayName)
        {
            GameObject prefab = PrefabFactory.Load("HidingSpot");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _interactables);
            instance.name = "HidingSpot_" + displayName;
            instance.transform.SetPositionAndRotation(new Vector3(xz.x, 0f, xz.y), Quaternion.Euler(0f, yaw, 0f));

            BuildUtils.SetField(instance.GetComponent<Stealth.Interaction.HidingSpot>(), "_displayName", displayName);
        }

        private static void StonePile(Vector2 xz)
        {
            GameObject prefab = PrefabFactory.Load("StonePile");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _interactables);
            instance.transform.position = new Vector3(xz.x, 0f, xz.y);
        }

        private static void Intel(Vector2 xz)
        {
            GameObject prefab = PrefabFactory.Load("IntelPickup");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _interactables);
            instance.transform.position = new Vector3(xz.x, 0f, xz.y);
        }

        private static void SecurityCamera(string name, Vector2 xz, float yaw)
        {
            GameObject prefab = PrefabFactory.Load("SecurityCamera");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _enemies);
            instance.name = name;
            instance.transform.SetPositionAndRotation(new Vector3(xz.x, 0f, xz.y), Quaternion.Euler(0f, yaw, 0f));

            BuildUtils.SetField(instance.GetComponent<Stealth.Enemies.SecurityCamera>(), "_profile",
                AssetFactory.CameraProfile);
        }

        private static void Guard(string name, GuardProfile profile, Vector2 xz, float yaw, PatrolRoute.RouteMode mode,
            params (Vector2 point, float wait, bool look)[] stops)
        {
            GameObject prefab = PrefabFactory.Load("Guard");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _enemies);
            instance.name = name;
            instance.transform.SetPositionAndRotation(new Vector3(xz.x, 0f, xz.y), Quaternion.Euler(0f, yaw, 0f));

            GuardController controller = instance.GetComponent<GuardController>();
            BuildUtils.SetField(controller, "_profile", profile);

            // Tint the body so the three archetypes are easy to tell apart.
            Material body = AssetFactory.BodyMaterialFor(profile);
            foreach (string part in new[] { "Body", "Head" })
            {
                Renderer renderer = FindChildRenderer(instance.transform, part);
                if (renderer != null) renderer.sharedMaterial = body;
            }

            if (stops == null || stops.Length == 0) return;

            GameObject routeGo = new GameObject("Route_" + name);
            routeGo.transform.SetParent(_routes);
            PatrolRoute route = routeGo.AddComponent<PatrolRoute>();

            List<PatrolRoute.Stop> list = new List<PatrolRoute.Stop>();
            for (int i = 0; i < stops.Length; i++)
            {
                GameObject point = new GameObject($"WP{i}");
                point.transform.SetParent(routeGo.transform);
                point.transform.position = new Vector3(stops[i].point.x, 0f, stops[i].point.y);

                list.Add(new PatrolRoute.Stop
                {
                    Point = point.transform,
                    WaitSeconds = stops[i].wait,
                    LookAround = stops[i].look
                });
            }

            route.SetStops(list, mode);
            BuildUtils.SetField(controller, "_route", route);
        }
    }
}
