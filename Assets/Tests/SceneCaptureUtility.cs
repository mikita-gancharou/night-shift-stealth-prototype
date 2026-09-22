using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Stealth.Core;
using Stealth.Enemies;
using Stealth.Enemies.States;
using Stealth.Interaction;
using Stealth.Level;
using Stealth.Player;

namespace Stealth.Tests
{
    /// <summary>
    /// Not a check: a helper that plays the level and writes PNG screenshots to Logs/Screenshots.
    /// Marked explicit so it never runs as part of the normal test pass. Handy for reviewing the look of
    /// the game without opening the editor:
    ///   Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Stealth.Tests.SceneCaptureUtility
    /// </summary>
    [Explicit("Screenshot helper, run it on demand.")]
    public class SceneCaptureUtility
    {
        private const int Width = 1600;
        private const int Height = 900;

        private static string OutputFolder
        {
            get
            {
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screenshots"));
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        [UnityTest]
        public IEnumerator CaptureLevelShots()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Level, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSeconds(0.6f);

            PlayerController player = PlayerController.Instance;
            Camera main = Camera.main;
            Canvas hud = FindHud();

            // 1. Top down map of the whole depot, with the night mood temporarily lifted for readability.
            yield return CaptureOverview();

            // 2. The view the player starts with.
            yield return CaptureFrame(main, hud, "02_start.png");

            // 3. A patrol guard seen from cover: its cone and badge are visible.
            GuardController patrol = FindGuard("Guard_Charlie");
            if (patrol != null)
            {
                Vector3 spot = patrol.transform.position + patrol.transform.forward * 11f + patrol.transform.right * 2f;
                spot.y = 0.2f;
                player.Motor.Teleport(spot, Quaternion.LookRotation(-patrol.transform.forward, Vector3.up));
                player.Stance.SetCrouch(true);
                yield return new WaitForSeconds(0.8f);
                yield return CaptureFrame(main, hud, "03_guard_cone.png");

                // 4. Full detection: red cone, banner, screen vignette.
                player.Stance.SetCrouch(false);
                float elapsed = 0f;
                while (elapsed < 14f && patrol.CurrentState != GuardStateId.Chase)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Let the "spotted" flash fade so the shot shows the sustained chase state.
                yield return new WaitForSeconds(0.7f);
                yield return CaptureFrame(main, hud, "04_detected.png");
            }

            // 5. Hidden inside a container: the chase breaks and the HUD turns calm.
            HidingSpot spot2 = NearestSpot(player.transform.position);
            if (spot2 != null)
            {
                player.Hiding.TryHide(spot2);
                yield return new WaitForSeconds(1.2f);
                yield return CaptureFrame(main, hud, "05_hidden.png");
                player.Hiding.LeaveHiding();
                yield return new WaitForSeconds(0.4f);
            }

            // 6. Aiming a stone: trajectory preview and landing marker, from an open spot.
            player.Motor.Teleport(new Vector3(-6f, 0.2f, 4f), Quaternion.Euler(0f, 20f, 0f));
            yield return new WaitForSeconds(0.5f);

            // The controller mirrors the real mouse every frame, so switch it off while faking the aim.
            player.enabled = false;
            player.Thrower.SetAiming(true);
            yield return new WaitForSeconds(0.5f);
            yield return CaptureFrame(main, hud, "06_aiming.png");
            player.Thrower.SetAiming(false);
            player.enabled = true;

            // 7. Victory screen.
            LevelGoal goal = Object.FindFirstObjectByType<LevelGoal>();
            if (goal != null)
            {
                Vector3 target = goal.transform.position;
                player.Motor.Teleport(new Vector3(target.x, 0.2f, target.z - 3f), Quaternion.identity);
                yield return new WaitForSeconds(0.4f);
                yield return CaptureFrame(main, hud, "07_extraction.png");

                player.Motor.Teleport(new Vector3(target.x, 0.2f, target.z), Quaternion.identity);
                float elapsed = 0f;
                while (elapsed < 4f && GameManager.Instance.State != GameState.Won)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(1.6f);
                yield return CaptureFrame(main, hud, "08_victory.png");
            }

            Debug.Log("[Stealth] Screenshots written to " + OutputFolder);
            Assert.Pass();
        }

        private static IEnumerator CaptureOverview()
        {
            bool fog = RenderSettings.fog;
            Color ambient = RenderSettings.ambientLight;
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.52f);

            GameObject go = new GameObject("CaptureCamera");
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.04f);
            camera.farClipPlane = 200f;
            go.transform.SetPositionAndRotation(new Vector3(0f, 80f, 2f), Quaternion.Euler(90f, 0f, 0f));

            yield return null;
            Capture(camera, "01_overview.png", Width, Width);

            Object.DestroyImmediate(go);
            RenderSettings.fog = fog;
            RenderSettings.ambientLight = ambient;
        }

        private static IEnumerator CaptureFrame(Camera camera, Canvas hud, string fileName)
        {
            // Screen space overlay canvases are not part of a camera render, so borrow the camera for a frame.
            RenderMode previous = RenderMode.ScreenSpaceOverlay;
            if (hud != null)
            {
                previous = hud.renderMode;
                hud.renderMode = RenderMode.ScreenSpaceCamera;
                hud.worldCamera = camera;
                hud.planeDistance = 1f;
            }

            // Note: WaitForEndOfFrame never resumes in batch mode, so step a normal frame instead.
            yield return null;
            Capture(camera, fileName, Width, Height);

            if (hud != null) hud.renderMode = previous;
        }

        private static void Capture(Camera camera, string fileName, int width, int height)
        {
            if (camera == null) return;

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 2;

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            File.WriteAllBytes(Path.Combine(OutputFolder, fileName), texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
        }

        private static Canvas FindHud()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas;
            }

            return null;
        }

        private static GuardController FindGuard(string name)
        {
            foreach (GuardController guard in Object.FindObjectsByType<GuardController>(FindObjectsSortMode.None))
            {
                if (guard.name == name) return guard;
            }

            return null;
        }

        private static HidingSpot NearestSpot(Vector3 position)
        {
            HidingSpot best = null;
            float bestDistance = float.MaxValue;

            foreach (HidingSpot spot in Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(position, spot.HidePoint);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = spot;
            }

            return best;
        }
    }
}
