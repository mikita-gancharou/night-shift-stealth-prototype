using UnityEngine;

namespace Stealth.Perception
{
    /// <summary>
    /// What an observer needs to know about the thing it is hunting.
    /// The AI depends on this interface instead of on the player scripts, so sensors can be tested
    /// with a dummy target and the player implementation can change freely.
    /// </summary>
    public interface IStealthTarget
    {
        Transform Transform { get; }

        /// <summary>Chest height position, used for distances and for "look at" rotations.</summary>
        Vector3 Center { get; }

        /// <summary>True while the target sits inside a hiding spot and cannot be perceived at all.</summary>
        bool IsHidden { get; }

        /// <summary>Posture and lighting multiplier: crouching in the dark is well below 1, sprinting in a spotlight is above.</summary>
        float VisibilityMultiplier { get; }

        /// <summary>How loud the target is right now, 0 (still or crouch-walking) to 1 (sprinting on gravel).</summary>
        float NoiseLevel { get; }

        /// <summary>
        /// Fills <paramref name="buffer"/> with world space points to raycast against (head, chest, feet)
        /// and returns how many were written. Multiple points are what makes low cover work: crouching
        /// pulls the head point below a crate while the feet stay hidden behind it.
        /// </summary>
        int GetSamplePoints(Vector3[] buffer);
    }

    /// <summary>Keeps the single stealth target of the scene reachable without expensive scene searches.</summary>
    public static class StealthTargetLocator
    {
        public static IStealthTarget Current { get; private set; }

        /// <summary>
        /// Raised when the target slips into a hiding spot. Observers use it to drop the hunt immediately
        /// instead of waiting for their detection meter to drain.
        /// </summary>
        public static event System.Action TargetHidden;

        public static void Register(IStealthTarget target) => Current = target;

        public static void Unregister(IStealthTarget target)
        {
            if (ReferenceEquals(Current, target)) Current = null;
        }

        public static void NotifyHidden() => TargetHidden?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            TargetHidden = null;
        }
    }
}
