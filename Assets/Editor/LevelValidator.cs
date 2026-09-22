using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Stealth.Enemies;
using Stealth.Interaction;
using Stealth.Level;
using Stealth.Player;

namespace Stealth.EditorTools
{
    /// <summary>
    /// Sanity check for the generated level: every patrol point must sit on the navigation mesh, every
    /// guard must be able to walk its route, and the player must have a path from the spawn to the exit.
    /// Catching this here is far cheaper than discovering a stuck guard while playing.
    /// </summary>
    public static class LevelValidator
    {
        [MenuItem("Stealth/Validate Level", priority = 20)]
        public static void ValidateMenu()
        {
            EditorSceneManager.OpenScene(StealthPaths.LevelScene, OpenSceneMode.Single);
            Validate(out string report);
            Debug.Log(report);
        }

        public static bool Validate(out string report)
        {
            StringBuilder log = new StringBuilder();
            List<string> problems = new List<string>();

            log.AppendLine("[Stealth] Level validation");

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            log.AppendLine($"  NavMesh vertices: {triangulation.vertices.Length}");
            if (triangulation.vertices.Length == 0) problems.Add("NavMesh is empty - guards cannot move.");

            // Player spawn -> extraction point.
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            LevelGoal goal = Object.FindFirstObjectByType<LevelGoal>();

            if (player == null) problems.Add("No player in the scene.");
            if (goal == null) problems.Add("No extraction point in the scene.");

            if (player != null && goal != null)
            {
                if (HasPath(player.transform.position, goal.transform.position, out float distance))
                {
                    log.AppendLine($"  Path spawn -> extraction: ok ({distance:0.0} m of walking)");
                }
                else
                {
                    problems.Add("No walkable path from the player spawn to the extraction point.");
                }
            }

            // Guards and their routes.
            GuardController[] guards = Object.FindObjectsByType<GuardController>(FindObjectsSortMode.None);
            log.AppendLine($"  Guards: {guards.Length}");

            foreach (GuardController guard in guards)
            {
                if (!OnNavMesh(guard.transform.position, out _))
                {
                    problems.Add($"{guard.name}: spawn point is not on the navmesh.");
                }

                PatrolRoute route = guard.Route;
                if (route == null || route.Count == 0)
                {
                    log.AppendLine($"    {guard.name}: stationary sentry");
                    continue;
                }

                for (int i = 0; i < route.Count; i++)
                {
                    Vector3 point = route.PositionOf(i);
                    if (!OnNavMesh(point, out float offset))
                    {
                        problems.Add($"{guard.name}: waypoint {i} at {point} is off the navmesh.");
                    }
                    else if (offset > 0.6f)
                    {
                        problems.Add($"{guard.name}: waypoint {i} is {offset:0.00} m away from walkable ground (tight spot).");
                    }

                    Vector3 next = route.PositionOf((i + 1) % route.Count);
                    if (!HasPath(point, next, out _))
                    {
                        problems.Add($"{guard.name}: no path between waypoint {i} and {(i + 1) % route.Count}.");
                    }
                }

                log.AppendLine($"    {guard.name}: {route.Count} waypoints, route ok");
            }

            // Cameras, hiding spots and pickups.
            SecurityCamera[] cameras = Object.FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
            HidingSpot[] spots = Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);
            IntelPickup[] intel = Object.FindObjectsByType<IntelPickup>(FindObjectsSortMode.None);
            StonePile[] piles = Object.FindObjectsByType<StonePile>(FindObjectsSortMode.None);

            log.AppendLine($"  Cameras: {cameras.Length}   hiding spots: {spots.Length}   intel: {intel.Length}   stone piles: {piles.Length}");

            if (spots.Length < 2) problems.Add("Fewer than two hiding spots.");
            if (cameras.Length < 1) problems.Add("No security camera in the level.");

            foreach (HidingSpot spot in spots)
            {
                if (player != null && !HasPath(player.transform.position, spot.ExitPoint, out _))
                {
                    problems.Add($"{spot.name}: cannot be reached from the spawn.");
                }
            }

            foreach (string problem in problems) log.AppendLine("  PROBLEM: " + problem);
            log.AppendLine(problems.Count == 0 ? "  Result: all checks passed" : $"  Result: {problems.Count} problem(s)");

            report = log.ToString();
            return problems.Count == 0;
        }

        private static bool OnNavMesh(Vector3 position, out float offset)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                offset = Vector3.Distance(position, hit.position);
                return true;
            }

            offset = float.MaxValue;
            return false;
        }

        private static bool HasPath(Vector3 from, Vector3 to, out float length)
        {
            length = 0f;

            if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, 3f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(to, out NavMeshHit toHit, 4f, NavMesh.AllAreas)) return false;

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;

            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return true;
        }
    }
}
