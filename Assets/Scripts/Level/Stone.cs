using UnityEngine;
using Stealth.Audio;
using Stealth.Perception;

namespace Stealth.Level
{
    /// <summary>
    /// The thrown distraction. Its first bounce emits a loud <see cref="NoiseEvent"/>, which is what pulls
    /// guards away from their route.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Stone : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _noiseRadius = 14f;
        [SerializeField, Range(0f, 1f)] private float _noiseIntensity = 1f;
        [SerializeField, Min(0.5f)] private float _lifetime = 8f;
        [SerializeField] private GameObject _impactEffect;

        private Rigidbody _rigidbody;
        private bool _hasLanded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 velocity)
        {
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 8f;
            Destroy(gameObject, _lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            AudioManager.PlaySfx(SoundId.StoneImpact, transform.position);

            if (_hasLanded) return;
            _hasLanded = true;

            NoiseSystem.Emit(transform.position, _noiseRadius, _noiseIntensity, NoiseKind.Impact);

            if (_impactEffect != null)
            {
                GameObject effect = Instantiate(_impactEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
    }
}
