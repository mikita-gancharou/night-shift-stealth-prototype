using UnityEngine;

namespace Stealth.Perception
{
    /// <summary>
    /// Line of sight sensor: a view cone plus a <see cref="Physics.Raycast"/> per sample point, so distance,
    /// field of view AND obstacles between the observer and the player all matter.
    /// This is the primary detection zone required by the assignment.
    /// </summary>
    [DisallowMultipleComponent]
    public class VisionSensor : MonoBehaviour
    {
        [Header("Shape")]
        [Tooltip("Where the rays start. Usually a small transform at head height looking forward.")]
        [SerializeField] private Transform _eye;

        [SerializeField, Min(0.5f)] private float _viewDistance = 14f;
        [SerializeField, Range(5f, 360f)] private float _viewAngle = 90f;

        [Header("Layers")]
        [Tooltip("Everything that blocks sight: walls, containers, crates.")]
        [SerializeField] private LayerMask _obstacleMask;

        [Tooltip("The layer the player is on.")]
        [SerializeField] private LayerMask _targetMask;

        [Header("Exposure curve")]
        [Tooltip("Exposure produced when the target stands right in front of the observer.")]
        [SerializeField, Min(0.1f)] private float _nearExposure = 2f;

        [Tooltip("Exposure produced at the very edge of the view distance.")]
        [SerializeField, Min(0.05f)] private float _farExposure = 0.45f;

        private readonly Vector3[] _samples = new Vector3[4];

        public Transform Eye => _eye != null ? _eye : transform;
        public float ViewDistance => _viewDistance;
        public float ViewAngle => _viewAngle;
        public LayerMask ObstacleMask => _obstacleMask;

        /// <summary>Last point of the target that was actually visible. Only meaningful right after a positive sample.</summary>
        public Vector3 LastVisiblePoint { get; private set; }

        /// <summary>
        /// Returns 0 when the target cannot be seen, otherwise how strongly it is exposed.
        /// The value scales with distance and with the target's own visibility (crouching, light, speed).
        /// </summary>
        public float SampleExposure(IStealthTarget target)
        {
            if (target == null || target.IsHidden) return 0f;

            Transform eye = Eye;
            Vector3 origin = eye.position;
            Vector3 forward = eye.forward;

            int count = target.GetSamplePoints(_samples);
            float bestDistanceFactor = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 point = _samples[i];
                Vector3 toPoint = point - origin;
                float distance = toPoint.magnitude;

                // 1. Distance check.
                if (distance > _viewDistance || distance <= 0.001f) continue;

                // 2. Field of view check, measured on the horizontal plane so height differences never
                //    push a target out of the cone that is drawn on the ground.
                Vector3 flatToPoint = new Vector3(toPoint.x, 0f, toPoint.z);
                Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
                if (flatToPoint.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f)
                {
                    if (Vector3.Angle(flatForward, flatToPoint) > _viewAngle * 0.5f) continue;
                }

                // 3. Obstacle check: the ray must reach the player without hitting the level first.
                Vector3 direction = toPoint / distance;
                if (Physics.Raycast(origin, direction, out RaycastHit hit, distance + 0.05f,
                        _obstacleMask | _targetMask, QueryTriggerInteraction.Ignore))
                {
                    bool hitTarget = ((1 << hit.collider.gameObject.layer) & _targetMask) != 0;
                    if (!hitTarget) continue;
                }

                float distanceFactor = Mathf.Lerp(_nearExposure, _farExposure, distance / _viewDistance);
                if (distanceFactor > bestDistanceFactor)
                {
                    bestDistanceFactor = distanceFactor;
                    LastVisiblePoint = point;
                }
            }

            if (bestDistanceFactor <= 0f) return 0f;

            return bestDistanceFactor * Mathf.Max(0f, target.VisibilityMultiplier);
        }

        /// <summary>Plain visibility question used by the AI states, e.g. "can I still see the spot I am walking to?".</summary>
        public bool HasLineOfSight(Vector3 worldPoint)
        {
            Vector3 origin = Eye.position;
            Vector3 toPoint = worldPoint - origin;
            float distance = toPoint.magnitude;
            if (distance > _viewDistance) return false;

            return !Physics.Raycast(origin, toPoint / distance, distance - 0.1f, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        public void ApplyShape(float viewDistance, float viewAngle)
        {
            _viewDistance = viewDistance;
            _viewAngle = viewAngle;
        }

        /// <summary>Wires the sensor up from code (used by spawners and by the automated tests).</summary>
        public void Configure(Transform eye, LayerMask obstacleMask, LayerMask targetMask)
        {
            _eye = eye;
            _obstacleMask = obstacleMask;
            _targetMask = targetMask;
        }

        private void OnDrawGizmosSelected()
        {
            Transform eye = Eye;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(eye.position, _viewDistance);

            Vector3 left = Quaternion.Euler(0f, -_viewAngle * 0.5f, 0f) * eye.forward;
            Vector3 right = Quaternion.Euler(0f, _viewAngle * 0.5f, 0f) * eye.forward;
            Gizmos.DrawLine(eye.position, eye.position + left * _viewDistance);
            Gizmos.DrawLine(eye.position, eye.position + right * _viewDistance);
        }
    }
}
