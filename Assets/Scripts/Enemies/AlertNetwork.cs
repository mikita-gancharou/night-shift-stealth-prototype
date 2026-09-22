using System.Collections.Generic;
using UnityEngine;

namespace Stealth.Enemies
{
    /// <summary>Anything that can be called in over the radio: guards, and in a bigger game also alarms.</summary>
    public interface IAlertReceiver
    {
        Vector3 Position { get; }

        /// <summary>Somebody reported the player at <paramref name="position"/>.</summary>
        void OnAlertCall(Vector3 position);
    }

    /// <summary>
    /// The guards' radio. When one observer identifies the player (a guard starting a chase, or a camera
    /// catching them on screen) everybody nearby is told where to look, which makes the level react as a
    /// group instead of as isolated enemies.
    /// </summary>
    public static class AlertNetwork
    {
        private static readonly List<IAlertReceiver> Receivers = new List<IAlertReceiver>(16);

        public static void Register(IAlertReceiver receiver)
        {
            if (receiver != null && !Receivers.Contains(receiver)) Receivers.Add(receiver);
        }

        public static void Unregister(IAlertReceiver receiver) => Receivers.Remove(receiver);

        public static void Broadcast(Vector3 position, float radius, IAlertReceiver source = null)
        {
            if (radius <= 0f) return;

            float radiusSqr = radius * radius;
            for (int i = 0; i < Receivers.Count; i++)
            {
                IAlertReceiver receiver = Receivers[i];
                if (receiver == null || ReferenceEquals(receiver, source)) continue;

                if ((receiver.Position - position).sqrMagnitude <= radiusSqr) receiver.OnAlertCall(position);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Receivers.Clear();
    }
}
