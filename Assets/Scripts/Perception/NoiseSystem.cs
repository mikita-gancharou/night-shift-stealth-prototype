using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stealth.Perception
{
    public enum NoiseKind
    {
        Footstep,
        Impact,
        Machine,
        Voice
    }

    /// <summary>A single sound event travelling through the level.</summary>
    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly float Intensity;
        public readonly NoiseKind Kind;
        public readonly bool CausedByPlayer;

        public NoiseEvent(Vector3 position, float radius, float intensity, NoiseKind kind, bool causedByPlayer)
        {
            Position = position;
            Radius = radius;
            Intensity = Mathf.Clamp01(intensity);
            Kind = kind;
            CausedByPlayer = causedByPlayer;
        }
    }

    /// <summary>Anything that reacts to sound: guards, and security cameras with a microphone.</summary>
    public interface INoiseListener
    {
        Vector3 EarPosition { get; }
        float HearingRadius { get; }
        void OnNoiseHeard(NoiseEvent noise);
    }

    /// <summary>
    /// Tiny broadcast bus for sound. Emitters (footsteps, thrown stones, switches) do not know who listens,
    /// listeners do not know who makes noise - the level stays easy to extend.
    /// </summary>
    public static class NoiseSystem
    {
        private static readonly List<INoiseListener> Listeners = new List<INoiseListener>(16);

        /// <summary>Raised for every noise, mostly so effects and debug views can visualise it.</summary>
        public static event Action<NoiseEvent> NoiseEmitted;

        public static void Register(INoiseListener listener)
        {
            if (listener != null && !Listeners.Contains(listener)) Listeners.Add(listener);
        }

        public static void Unregister(INoiseListener listener)
        {
            Listeners.Remove(listener);
        }

        public static void Emit(NoiseEvent noise)
        {
            if (noise.Radius <= 0f || noise.Intensity <= 0f) return;

            for (int i = 0; i < Listeners.Count; i++)
            {
                INoiseListener listener = Listeners[i];
                if (listener == null) continue;

                float audibleRange = Mathf.Min(noise.Radius, listener.HearingRadius);
                if ((listener.EarPosition - noise.Position).sqrMagnitude <= audibleRange * audibleRange)
                {
                    listener.OnNoiseHeard(noise);
                }
            }

            NoiseEmitted?.Invoke(noise);
        }

        public static void Emit(Vector3 position, float radius, float intensity, NoiseKind kind, bool causedByPlayer = true)
        {
            Emit(new NoiseEvent(position, radius, intensity, kind, causedByPlayer));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Listeners.Clear();
            NoiseEmitted = null;
        }
    }
}
