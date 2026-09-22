using UnityEngine;

namespace Stealth.Perception
{
    /// <summary>
    /// Second detection zone of the assignment, built on a trigger <see cref="SphereCollider"/>.
    /// It models close range awareness: a guard does not need to look at you to notice steps right next to them,
    /// but crouch-walking is silent, so the sphere alone will not give you away.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public class ProximitySensor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _radius = 4.5f;

        [Tooltip("The layer the player is on.")]
        [SerializeField] private LayerMask _targetMask;

        [Tooltip("Exposure produced by a target making full noise at the centre of the sphere.")]
        [SerializeField, Min(0.05f)] private float _noiseWeight = 1.3f;

        [Tooltip("Inside this distance the observer notices the target even when it is completely silent.")]
        [SerializeField, Min(0f)] private float _closeContactDistance = 0.9f;

        [SerializeField, Min(0.05f)] private float _closeContactExposure = 0.6f;

        private SphereCollider _sphere;

        /// <summary>True while the player's collider is inside the sphere.</summary>
        public bool TargetInside { get; private set; }

        public float Radius => _radius;

        private void Awake()
        {
            _sphere = GetComponent<SphereCollider>();
            _sphere.isTrigger = true;
            _sphere.radius = _radius;
        }

        private void OnValidate()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.isTrigger = true;
                sphere.radius = _radius;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsTarget(other)) TargetInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsTarget(other)) TargetInside = false;
        }

        private void OnDisable()
        {
            TargetInside = false;
        }

        private bool IsTarget(Collider other)
        {
            return ((1 << other.gameObject.layer) & _targetMask) != 0;
        }

        /// <summary>Exposure contributed by this zone, 0 when the target is outside, hidden or silent.</summary>
        public float SampleExposure(IStealthTarget target)
        {
            if (!TargetInside || target == null || target.IsHidden) return 0f;

            float distance = Vector3.Distance(transform.position, target.Center);
            if (distance <= _closeContactDistance) return _closeContactExposure;

            float noise = Mathf.Clamp01(target.NoiseLevel);
            if (noise <= 0.01f) return 0f;

            // Loud and close gives a strong reading, loud at the rim of the sphere barely registers.
            float closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, _radius));
            return noise * closeness * _noiseWeight;
        }

        /// <summary>Wires the sensor up from code (used by spawners and by the automated tests).</summary>
        public void Configure(LayerMask targetMask)
        {
            _targetMask = targetMask;
        }

        public void ApplyRadius(float radius)
        {
            _radius = radius;
            SphereCollider sphere = _sphere != null ? _sphere : GetComponent<SphereCollider>();
            if (sphere != null) sphere.radius = radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
