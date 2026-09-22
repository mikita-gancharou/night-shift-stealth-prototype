using UnityEngine;

namespace Stealth.Visuals
{
    /// <summary>Slow spin plus a soft pulse, used to make the extraction point read from across the level.</summary>
    public class Beacon : MonoBehaviour
    {
        [SerializeField] private Transform _spinner;
        [SerializeField] private Transform _pulseRoot;
        [SerializeField] private Light _light;

        [SerializeField] private float _spinSpeed = 42f;
        [SerializeField] private float _pulseSpeed = 1.8f;
        [SerializeField] private float _pulseAmount = 0.12f;
        [SerializeField] private float _minIntensity = 1.1f;
        [SerializeField] private float _maxIntensity = 2.3f;

        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (_pulseRoot != null) _baseScale = _pulseRoot.localScale;
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;

            if (_spinner != null) _spinner.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
            if (_pulseRoot != null) _pulseRoot.localScale = _baseScale * (1f + wave * _pulseAmount);
            if (_light != null) _light.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, wave);
        }
    }
}
