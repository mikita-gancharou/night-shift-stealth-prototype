using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Stealth.Core;
using Stealth.Enemies;
using Stealth.Enemies.States;
using Stealth.Interaction;
using Stealth.Level;
using Stealth.Perception;
using Stealth.Player;

namespace Stealth.Tests
{
    /// <summary>
    /// End to end checks of the mission rules on the real level scene: win by reaching the extraction point,
    /// lose by being caught, escape a chase by hiding, and pull a guard off its route with a noise.
    /// </summary>
    public class MissionFlowTests
    {
        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Level, LoadSceneMode.Single);

            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator LevelContainsEveryRequiredPiece()
        {
            Assert.IsNotNull(PlayerController.Instance, "No player in the level.");
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<LevelGoal>(), "No extraction point.");
            Assert.IsNotNull(GameManager.Instance, "No game manager.");

            GuardController[] guards = UnityEngine.Object.FindObjectsByType<GuardController>(FindObjectsSortMode.None);
            SecurityCamera[] cameras = UnityEngine.Object.FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
            HidingSpot[] spots = UnityEngine.Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);

            Assert.GreaterOrEqual(guards.Length, 2, "The level needs more than one guard.");
            Assert.GreaterOrEqual(cameras.Length, 1, "The level needs at least one camera.");
            Assert.GreaterOrEqual(spots.Length, 2, "The level needs hiding spots.");

            yield break;
        }

        [UnityTest]
        public IEnumerator ReachingTheExtractionPointWinsTheMission()
        {
            DisableObservers();

            LevelGoal goal = UnityEngine.Object.FindFirstObjectByType<LevelGoal>();
            PlayerController player = PlayerController.Instance;

            Vector3 target = goal.transform.position;
            player.Motor.Teleport(new Vector3(target.x, 0.2f, target.z), Quaternion.identity);

            yield return WaitUntil(() => GameManager.Instance.State == GameState.Won, 4f);

            Assert.AreEqual(GameState.Won, GameManager.Instance.State);
        }

        [UnityTest]
        public IEnumerator GuardSpotsThePlayerStandingInTheOpenAndGivesChase()
        {
            GuardController guard = PrepareSingleGuard();
            PlayerController player = PlayerController.Instance;

            PlaceInFrontOf(guard, player, 5f);

            yield return WaitUntil(() => guard.CurrentState == GuardStateId.Chase, 12f);

            Assert.AreEqual(GuardStateId.Chase, guard.CurrentState, "The guard should have started chasing.");
            Assert.GreaterOrEqual(GameManager.Instance.Stats.TimesSpotted, 1, "Being spotted must be counted.");
        }

        [UnityTest]
        public IEnumerator DetectionClimbsThroughTheIntermediateLevels()
        {
            GuardController guard = PrepareSingleGuard();
            PlayerController player = PlayerController.Instance;

            PlaceInFrontOf(guard, player, 9f);

            bool sawPartialDetection = false;
            float elapsed = 0f;

            while (elapsed < 12f && guard.Perception.Level != AwarenessLevel.Detected)
            {
                if (guard.Perception.Value01 > 0.05f && guard.Perception.Value01 < 0.95f) sawPartialDetection = true;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(sawPartialDetection, "Detection must be progressive, not instant.");
            Assert.AreEqual(AwarenessLevel.Detected, guard.Perception.Level);
        }

        [UnityTest]
        public IEnumerator HidingBreaksTheChase()
        {
            GuardController guard = PrepareSingleGuard();
            PlayerController player = PlayerController.Instance;

            PlaceInFrontOf(guard, player, 5f);
            yield return WaitUntil(() => guard.CurrentState == GuardStateId.Chase, 12f);
            Assert.AreEqual(GuardStateId.Chase, guard.CurrentState, "Pre-condition: the guard must be chasing.");

            HidingSpot spot = NearestHidingSpot(player.transform.position);
            Assert.IsNotNull(spot);
            Assert.IsTrue(player.Hiding.TryHide(spot), "The player should be able to hide.");

            yield return WaitUntil(() => guard.CurrentState != GuardStateId.Chase, 3f);

            Assert.AreNotEqual(GuardStateId.Chase, guard.CurrentState, "Hiding must break the chase.");
            Assert.IsTrue(player.IsHidden);
        }

        [UnityTest]
        public IEnumerator BeingCaughtEndsTheMission()
        {
            GuardController guard = PrepareSingleGuard();
            PlayerController player = PlayerController.Instance;

            PlaceInFrontOf(guard, player, 4f);

            yield return WaitUntil(() => GameManager.Instance.State == GameState.Lost, 20f);

            Assert.AreEqual(GameState.Lost, GameManager.Instance.State, "A guard reaching the player must end the run.");
        }

        [UnityTest]
        public IEnumerator NoisePullsAGuardOffItsRoute()
        {
            GuardController guard = PrepareSingleGuard();
            PlayerController player = PlayerController.Instance;

            // Keep the player out of sight so only the noise can move the guard.
            player.Motor.Teleport(new Vector3(0f, 0.2f, -32f), Quaternion.identity);
            yield return new WaitForSeconds(0.3f);

            Vector3 noisePosition = guard.transform.position + guard.transform.right * 6f;
            NoiseSystem.Emit(noisePosition, 16f, 1f, NoiseKind.Impact);

            yield return WaitUntil(() => guard.CurrentState == GuardStateId.Investigate, 3f);

            Assert.AreEqual(GuardStateId.Investigate, guard.CurrentState,
                "A stone landing nearby must send the guard to investigate.");
        }

        // ----- helpers ----------------------------------------------------------------------------

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                yield return null;
            }
        }

        /// <summary>Leaves a single guard active so the assertions cannot be spoiled by a second one.</summary>
        private static GuardController PrepareSingleGuard(string preferredName = "Guard_Echo")
        {
            GuardController[] guards = UnityEngine.Object.FindObjectsByType<GuardController>(FindObjectsSortMode.None);
            Assert.Greater(guards.Length, 0, "The level has no guards.");

            GuardController chosen = Array.Find(guards, guard => guard.name == preferredName) ?? guards[0];

            foreach (GuardController guard in guards)
            {
                if (guard != chosen) guard.gameObject.SetActive(false);
            }

            foreach (SecurityCamera camera in UnityEngine.Object.FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None))
            {
                camera.gameObject.SetActive(false);
            }

            return chosen;
        }

        private static void DisableObservers()
        {
            foreach (GuardController guard in UnityEngine.Object.FindObjectsByType<GuardController>(FindObjectsSortMode.None))
            {
                guard.gameObject.SetActive(false);
            }

            foreach (SecurityCamera camera in UnityEngine.Object.FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None))
            {
                camera.gameObject.SetActive(false);
            }
        }

        private static void PlaceInFrontOf(GuardController guard, PlayerController player, float distance)
        {
            Vector3 forward = guard.transform.forward;
            Vector3 position = guard.transform.position + forward * distance;
            position.y = 0.2f;

            player.Stance.SetCrouch(false);
            player.Motor.Teleport(position, Quaternion.LookRotation(-forward, Vector3.up));
        }

        private static HidingSpot NearestHidingSpot(Vector3 position)
        {
            HidingSpot best = null;
            float bestDistance = float.MaxValue;

            foreach (HidingSpot spot in UnityEngine.Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
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
