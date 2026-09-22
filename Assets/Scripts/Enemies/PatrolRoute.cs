using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stealth.Enemies
{
    /// <summary>
    /// Ordered list of stops a guard walks through. Each stop can hold the guard for a moment and make it
    /// look around, which creates the timing windows the player has to read.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        public enum RouteMode
        {
            Loop,
            PingPong
        }

        [Serializable]
        public class Stop
        {
            public Transform Point;

            [Min(0f)] public float WaitSeconds = 1.5f;

            [Tooltip("Turn on the spot while waiting, as if checking the surroundings.")]
            public bool LookAround;
        }

        [SerializeField] private RouteMode _mode = RouteMode.Loop;
        [SerializeField] private List<Stop> _stops = new List<Stop>();

        public RouteMode Mode => _mode;
        public int Count => _stops.Count;

        public Stop GetStop(int index)
        {
            if (Count == 0) return null;
            return _stops[Mathf.Clamp(index, 0, Count - 1)];
        }

        public Vector3 PositionOf(int index)
        {
            Stop stop = GetStop(index);
            return stop != null && stop.Point != null ? stop.Point.position : transform.position;
        }

        /// <summary>Advances an index, honouring loop / ping-pong. <paramref name="direction"/> is flipped at the ends.</summary>
        public int Advance(int index, ref int direction)
        {
            if (Count <= 1) return 0;

            if (_mode == RouteMode.Loop) return (index + 1) % Count;

            int next = index + direction;
            if (next >= Count || next < 0)
            {
                direction = -direction;
                next = index + direction;
            }

            return Mathf.Clamp(next, 0, Count - 1);
        }

        public int ClosestIndex(Vector3 position)
        {
            int best = 0;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Count; i++)
            {
                float distance = (PositionOf(i) - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        public void SetStops(List<Stop> stops, RouteMode mode)
        {
            _stops = stops;
            _mode = mode;
        }

        private void OnDrawGizmos()
        {
            if (Count == 0) return;

            Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.9f);
            for (int i = 0; i < Count; i++)
            {
                Vector3 current = PositionOf(i);
                Gizmos.DrawWireSphere(current, 0.35f);

                bool last = i == Count - 1;
                if (!last) Gizmos.DrawLine(current, PositionOf(i + 1));
                else if (_mode == RouteMode.Loop && Count > 2) Gizmos.DrawLine(current, PositionOf(0));
            }
        }
    }
}
