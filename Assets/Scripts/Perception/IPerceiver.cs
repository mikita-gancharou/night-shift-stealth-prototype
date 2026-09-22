using System.Collections.Generic;
using UnityEngine;

namespace Stealth.Perception
{
    /// <summary>
    /// Read-only view of an observer for systems that only want to display its state:
    /// the on-screen detection arrows, the music director and the HUD banner.
    /// </summary>
    public interface IPerceiver
    {
        Vector3 EyePosition { get; }
        AwarenessLevel Level { get; }

        /// <summary>Detection fill, 0 to 1.</summary>
        float Detection01 { get; }

        /// <summary>True while this observer is actively hunting the player.</summary>
        bool IsHunting { get; }
    }

    /// <summary>All active observers of the scene. Registration happens in OnEnable / OnDisable.</summary>
    public static class PerceiverRegistry
    {
        private static readonly List<IPerceiver> Items = new List<IPerceiver>(16);

        public static IReadOnlyList<IPerceiver> All => Items;

        public static void Register(IPerceiver perceiver)
        {
            if (perceiver != null && !Items.Contains(perceiver)) Items.Add(perceiver);
        }

        public static void Unregister(IPerceiver perceiver) => Items.Remove(perceiver);

        /// <summary>Highest awareness anybody in the level currently has. Drives music and the HUD banner.</summary>
        public static AwarenessLevel MaxLevel
        {
            get
            {
                AwarenessLevel max = AwarenessLevel.Unaware;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] != null && Items[i].Level > max) max = Items[i].Level;
                }

                return max;
            }
        }

        /// <summary>Highest detection fill in the level, used for the "being seen" meter.</summary>
        public static float MaxDetection01
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] != null && Items[i].Detection01 > max) max = Items[i].Detection01;
                }

                return max;
            }
        }

        public static bool AnyHunting
        {
            get
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] != null && Items[i].IsHunting) return true;
                }

                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Items.Clear();
    }
}
