using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Stealth.Audio;
using Stealth.Enemies;

namespace Stealth.EditorTools
{
    /// <summary>
    /// Builds the shared assets the level is made of: import settings for the generated art and audio,
    /// every material, the guard profiles and the sound library.
    /// </summary>
    public static class AssetFactory
    {
        public static Material Ground;
        public static Material GroundGravel;
        public static Material GroundMetal;
        public static Material Wall;
        public static Material Concrete;
        public static Material ContainerTeal;
        public static Material ContainerRust;
        public static Material ContainerBlue;
        public static Material Crate;
        public static Material Metal;
        public static Material Truck;
        public static Material Hazard;
        public static Material PlayerBody;
        public static Material PlayerAccent;
        public static Material GuardBody;
        public static Material GuardAccent;
        public static Material LampHousing;
        public static Material LampGlow;
        public static Material Stone;
        public static Material IntelGlow;
        public static Material BeaconGlow;
        public static Material VisionCone;
        public static Material GroundGlow;
        public static Material ArcGlow;
        public static Material LampPool;
        public static Material PickupGlow;
        public static Material MarkerGlow;
        public static Material NightSky;

        [MenuItem("Stealth/Step 2 - Build Assets", priority = 2)]
        public static void BuildAll()
        {
            BuildUtils.EnsureFolder(StealthPaths.Materials);
            BuildUtils.EnsureFolder(StealthPaths.Settings);
            BuildUtils.EnsureFolder(StealthPaths.Prefabs);
            BuildUtils.EnsureFolder(StealthPaths.Scenes);

            ConfigureTextureImports();
            ConfigureAudioImports();
            CreateMaterials();
            CreateGuardProfiles();
            CreateSoundLibrary();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Stealth] Assets built.");
        }

        /// <summary>Loads (or rebuilds) the shared material references used by the other builders.</summary>
        public static void LoadMaterials()
        {
            if (Ground != null) return;
            CreateMaterials();
        }

        // ----- import settings --------------------------------------------------------------------

        private static void ConfigureTextureImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { StealthPaths.Sprites });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 100f;

                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (file == "panel" || file == "panel_outline") importer.spriteBorder = new Vector4(18f, 18f, 18f, 18f);

                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAudioImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { StealthPaths.Audio });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;

                bool isLoop = path.Contains("_loop") || path.Contains("goal_hum");

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = isLoop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = 0.7f;

                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = isLoop;
                importer.SaveAndReimport();
            }
        }

        // ----- materials --------------------------------------------------------------------------

        private static void CreateMaterials()
        {
            Ground = Standard("M_Ground", new Color32(0x22, 0x25, 0x2A, 0xFF), 0f, 0.12f);
            GroundGravel = Standard("M_GroundGravel", new Color32(0x3A, 0x34, 0x2B, 0xFF), 0f, 0.05f);
            GroundMetal = Standard("M_GroundMetal", new Color32(0x3B, 0x40, 0x47, 0xFF), 0.65f, 0.45f);
            Wall = Standard("M_Wall", new Color32(0x2B, 0x2F, 0x36, 0xFF), 0f, 0.1f);
            Concrete = Standard("M_Concrete", new Color32(0x3A, 0x3F, 0x47, 0xFF), 0f, 0.15f);
            ContainerTeal = Standard("M_ContainerTeal", new Color32(0x25, 0x54, 0x4C, 0xFF), 0.25f, 0.28f);
            ContainerRust = Standard("M_ContainerRust", new Color32(0x6E, 0x38, 0x2A, 0xFF), 0.25f, 0.25f);
            ContainerBlue = Standard("M_ContainerBlue", new Color32(0x2C, 0x3C, 0x5C, 0xFF), 0.25f, 0.3f);
            Crate = Standard("M_Crate", new Color32(0x5E, 0x48, 0x2E, 0xFF), 0f, 0.18f);
            Metal = Standard("M_Metal", new Color32(0x4A, 0x50, 0x58, 0xFF), 0.7f, 0.42f);
            Truck = Standard("M_Truck", new Color32(0x2E, 0x33, 0x3B, 0xFF), 0.4f, 0.4f);
            Hazard = Standard("M_Hazard", new Color32(0xC9, 0x9A, 0x2E, 0xFF), 0.2f, 0.35f);

            PlayerBody = Standard("M_PlayerBody", new Color32(0x2E, 0x3A, 0x52, 0xFF), 0.1f, 0.25f);
            PlayerAccent = Standard("M_PlayerAccent", new Color32(0x3C, 0xE8, 0xD2, 0xFF), 0f, 0.6f,
                new Color(0.22f, 0.95f, 0.85f) * 2.4f);

            GuardBody = Standard("M_GuardBody", new Color32(0x57, 0x5F, 0x6D, 0xFF), 0.15f, 0.3f);
            GuardAccent = Standard("M_GuardAccent", Color.white, 0f, 0.5f, Color.white * 1.2f);

            LampHousing = Standard("M_LampHousing", new Color32(0x33, 0x37, 0x3D, 0xFF), 0.6f, 0.4f);
            LampGlow = Standard("M_LampGlow", new Color32(0xFF, 0xE9, 0xC0, 0xFF), 0f, 0.5f,
                new Color(1f, 0.92f, 0.72f) * 2.2f);

            Stone = Standard("M_Stone", new Color32(0x6E, 0x72, 0x78, 0xFF), 0.1f, 0.2f);
            IntelGlow = Standard("M_IntelGlow", new Color32(0xFF, 0xC4, 0x5C, 0xFF), 0f, 0.5f,
                new Color(1f, 0.72f, 0.28f) * 1.8f);
            BeaconGlow = Standard("M_BeaconGlow", new Color32(0x4B, 0xF5, 0xA4, 0xFF), 0f, 0.5f,
                new Color(0.25f, 0.95f, 0.6f) * 2f);

            VisionCone = FromShader("M_VisionCone", "Stealth/VisionCone", new Color(0.45f, 0.85f, 1f, 0.3f));
            ArcGlow = FromShader("M_ArcGlow", "Stealth/UnlitGlow", new Color(0.4f, 0.95f, 1f, 0.7f));

            // Ground decals. They are additive, so the alpha is really "how much light to add".
            GroundGlow = Glow("M_GroundGlow", new Color(0.35f, 1f, 0.7f, 0.3f));   // extraction pad
            LampPool = Glow("M_LampPool", new Color(1f, 0.86f, 0.6f, 0.16f));      // warm pool under a lamp
            PickupGlow = Glow("M_PickupGlow", new Color(0.5f, 0.95f, 1f, 0.12f));  // subtle hint under a pickup
            MarkerGlow = Glow("M_MarkerGlow", new Color(0.45f, 1f, 0.9f, 0.55f));  // stone landing marker

            NightSky = CreateNightSky();
        }

        /// <summary>Additive ground decal material using the soft radial gradient sprite.</summary>
        private static Material Glow(string name, Color color)
        {
            Material material = FromShader(name, "Stealth/UnlitGlow", color);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{StealthPaths.Sprites}/soft_glow.png");
            if (texture != null && material != null) material.SetTexture("_MainTex", texture);

            return material;
        }

        private static Material CreateNightSky()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;

            string path = $"{StealthPaths.Materials}/M_NightSky.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (sky == null)
            {
                sky = new Material(shader);
                AssetDatabase.CreateAsset(sky, path);
            }

            sky.shader = shader;
            sky.SetFloat("_SunSize", 0.02f);
            sky.SetFloat("_AtmosphereThickness", 0.45f);
            sky.SetColor("_SkyTint", new Color(0.08f, 0.11f, 0.2f));
            sky.SetColor("_GroundColor", new Color(0.03f, 0.035f, 0.05f));
            sky.SetFloat("_Exposure", 0.55f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        /// <summary>Per profile body material, so the three guard archetypes read apart at a glance.</summary>
        public static Material BodyMaterialFor(GuardProfile profile)
        {
            if (profile == null) return GuardBody;

            string safeName = profile.name.Replace("Profile_", string.Empty);
            return Standard($"M_Body_{safeName}", profile.BodyColor, 0.15f, 0.3f);
        }

        private static Material Standard(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string path = $"{StealthPaths.Materials}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Standard");
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FromShader(string name, string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Stealth] Shader '{shaderName}' not found.");
                return null;
            }

            string path = $"{StealthPaths.Materials}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ----- guard profiles ---------------------------------------------------------------------

        public static GuardProfile PatrolGuardProfile => Load<GuardProfile>($"{StealthPaths.Settings}/Profile_PatrolGuard.asset");
        public static GuardProfile SentryProfile => Load<GuardProfile>($"{StealthPaths.Settings}/Profile_Sentry.asset");
        public static GuardProfile WatchmanProfile => Load<GuardProfile>($"{StealthPaths.Settings}/Profile_Watchman.asset");
        public static GuardProfile CameraProfile => Load<GuardProfile>($"{StealthPaths.Settings}/Profile_SecurityCamera.asset");
        public static SoundLibrary Sounds => Load<SoundLibrary>($"{StealthPaths.Settings}/SoundLibrary.asset");

        private static void CreateGuardProfiles()
        {
            // Standard patrol: medium range, medium cone, walks a route.
            GuardProfile patrol = CreateProfile("Profile_PatrolGuard");
            patrol.DisplayName = "Patrol Guard";
            patrol.BodyColor = new Color32(0x57, 0x5F, 0x6D, 0xFF);
            patrol.PatrolSpeed = 2f;
            patrol.InvestigateSpeed = 3.2f;
            patrol.ChaseSpeed = 4.8f;
            patrol.ViewDistance = 14f;
            patrol.ViewAngle = 90f;
            patrol.ProximityRadius = 4.5f;
            patrol.HearingRadius = 18f;
            patrol.ScanAngle = 80f;
            patrol.ScanSpeed = 26f;

            // Sentry: stands still, narrow but very long sight, sweeps slowly.
            GuardProfile sentry = CreateProfile("Profile_Sentry");
            sentry.DisplayName = "Sentry";
            sentry.BodyColor = new Color32(0x6A, 0x5B, 0x4A, 0xFF);
            sentry.PatrolSpeed = 1.8f;
            sentry.InvestigateSpeed = 3f;
            sentry.ChaseSpeed = 4.5f;
            sentry.ViewDistance = 21f;
            sentry.ViewAngle = 55f;
            sentry.ProximityRadius = 4f;
            sentry.HearingRadius = 20f;
            sentry.SearchDuration = 8f;
            sentry.ScanAngle = 120f;
            sentry.ScanSpeed = 20f;

            // Watchman: short sighted but very wide cone and a big close range bubble - dangerous indoors.
            GuardProfile watchman = CreateProfile("Profile_Watchman");
            watchman.DisplayName = "Watchman";
            watchman.BodyColor = new Color32(0x4E, 0x5A, 0x6B, 0xFF);
            watchman.PatrolSpeed = 2.2f;
            watchman.InvestigateSpeed = 3.4f;
            watchman.ChaseSpeed = 5f;
            watchman.ViewDistance = 11f;
            watchman.ViewAngle = 120f;
            watchman.ProximityRadius = 6.5f;
            watchman.HearingRadius = 16f;
            watchman.LoseSightTime = 3.2f;
            watchman.ScanAngle = 60f;
            watchman.ScanSpeed = 30f;

            // Camera: cannot move or chase, but sees far and calls everybody in.
            GuardProfile camera = CreateProfile("Profile_SecurityCamera");
            camera.DisplayName = "Security Camera";
            camera.BodyColor = new Color32(0x3A, 0x40, 0x4A, 0xFF);
            camera.ViewDistance = 17f;
            camera.ViewAngle = 42f;
            camera.ProximityRadius = 1f;
            camera.HearingRadius = 0f;
            camera.NoiseSensitivity = 0f;
            camera.AlertRadius = 30f;
            camera.CanChase = false;

            foreach (GuardProfile profile in new[] { patrol, sentry, watchman, camera }) EditorUtility.SetDirty(profile);
        }

        private static GuardProfile CreateProfile(string name)
        {
            string path = $"{StealthPaths.Settings}/{name}.asset";
            GuardProfile profile = AssetDatabase.LoadAssetAtPath<GuardProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<GuardProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            return profile;
        }

        // ----- sound library ----------------------------------------------------------------------

        private static void CreateSoundLibrary()
        {
            string path = $"{StealthPaths.Settings}/SoundLibrary.asset";
            SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);

            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, path);
            }

            List<SoundLibrary.Entry> entries = new List<SoundLibrary.Entry>
            {
                Entry(SoundId.StoneThrow, 0.7f, "stone_throw"),
                Entry(SoundId.StoneImpact, 0.9f, "stone_impact"),
                Entry(SoundId.PickupStone, 0.7f, "pickup_stone"),
                Entry(SoundId.PickupIntel, 0.8f, "pickup_intel"),
                Entry(SoundId.HideEnter, 0.8f, "hide_enter"),
                Entry(SoundId.HideExit, 0.8f, "hide_exit"),
                Entry(SoundId.SwitchToggle, 0.8f, "switch_toggle"),
                Entry(SoundId.GuardSuspicious, 0.9f, "guard_suspicious"),
                Entry(SoundId.GuardAlert, 1f, "guard_alert"),
                Entry(SoundId.GuardLost, 0.8f, "guard_lost"),
                Entry(SoundId.CameraBeep, 0.9f, "camera_beep"),
                Entry(SoundId.Win, 0.9f, "win_jingle"),
                Entry(SoundId.Lose, 0.9f, "lose_sting"),
                Entry(SoundId.UiClick, 0.7f, "ui_click"),
                Entry(SoundId.UiHover, 0.5f, "ui_hover")
            };

            library.SetEntries(entries.ToArray());
            // Three variations per surface: two alternating clips are audibly mechanical.
            library.SetFootsteps(
                Clips("footstep_1", "footstep_2", "footstep_3"),
                Clips("footstep_gravel_1", "footstep_gravel_2", "footstep_gravel_3"),
                Clips("footstep_metal_1", "footstep_metal_2", "footstep_metal_3"),
                Clips("footstep_soft_1", "footstep_soft_2", "footstep_soft_3"));

            library.Ambient = Clip("ambient_loop");
            library.Tension = Clip("tension_loop");
            library.Chase = Clip("chase_loop");

            EditorUtility.SetDirty(library);
        }

        private static SoundLibrary.Entry Entry(SoundId id, float volume, params string[] clipNames)
        {
            return new SoundLibrary.Entry
            {
                Id = id,
                Volume = volume,
                Clips = Clips(clipNames)
            };
        }

        public static AudioClip Clip(string name)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{StealthPaths.Audio}/{name}.wav");
            if (clip == null) Debug.LogWarning($"[Stealth] Audio clip '{name}' not found.");
            return clip;
        }

        private static AudioClip[] Clips(params string[] names)
        {
            List<AudioClip> clips = new List<AudioClip>();
            foreach (string name in names)
            {
                AudioClip clip = Clip(name);
                if (clip != null) clips.Add(clip);
            }

            return clips.ToArray();
        }

        public static Sprite LoadSprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{StealthPaths.Sprites}/{name}.png");
            if (sprite == null) Debug.LogWarning($"[Stealth] Sprite '{name}' not found.");
            return sprite;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
