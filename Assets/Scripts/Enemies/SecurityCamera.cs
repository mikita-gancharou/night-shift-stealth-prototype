using UnityEngine;
using Stealth.Audio;
using Stealth.Core;
using Stealth.Enemies.States;
using Stealth.Perception;

namespace Stealth.Enemies
{
    /// <summary>
    /// A second kind of enemy with a completely different detection zone: a long, narrow cone that sweeps
    /// left and right from a fixed mount. It cannot chase - instead it calls every guard in radio range,
    /// so walking through its beam turns a quiet corner into a busy one.
    /// </summary>
    [DisallowMultipleComponent]
    public class SecurityCamera : MonoBehaviour, IPerceiver
    {
        [Header("Configuration")]
        [SerializeField] private GuardProfile _profile;

        [Header("Parts")]
        [SerializeField] private GuardPerception _perception;
        [SerializeField] private Transform _head;
        [SerializeField] private GuardVisuals _visuals;

        [Header("Sweep")]
        [SerializeField, Range(0f, 170f)] private float _sweepAngle = 80f;
        [SerializeField, Min(1f)] private float _sweepSpeed = 22f;
        [SerializeField, Min(0.5f)] private float _alarmCooldown = 7f;

        private float _baseYaw;
        private float _phase;
        private float _cooldown;

        private void Awake()
        {
            if (_head == null) _head = transform;
            _baseYaw = _head.localEulerAngles.y;

            ApplyProfile();
        }

        private void OnEnable() => PerceiverRegistry.Register(this);

        private void OnDisable() => PerceiverRegistry.Unregister(this);

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game != null && !game.IsPlaying) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            _perception.Tick(deltaTime);
            if (_cooldown > 0f) _cooldown -= deltaTime;

            switch (_perception.Level)
            {
                case AwarenessLevel.Detected:
                    TrackTarget(deltaTime);
                    RaiseAlarm();
                    break;

                case AwarenessLevel.Alerted:
                case AwarenessLevel.Suspicious:
                    TrackTarget(deltaTime);
                    break;

                default:
                    Sweep(deltaTime);
                    break;
            }

            if (_visuals != null) _visuals.Apply(_perception.Level, _perception.Value01, GuardStateId.Patrol);
        }

        public void ApplyProfile()
        {
            if (_profile == null || _perception == null) return;

            if (_perception.Vision != null) _perception.Vision.ApplyShape(_profile.ViewDistance, _profile.ViewAngle);
            if (_perception.Proximity != null) _perception.Proximity.ApplyRadius(_profile.ProximityRadius);
            _perception.ApplySettings(_profile.HearingRadius, _profile.NoiseSensitivity);
        }

        private void Sweep(float deltaTime)
        {
            _phase += deltaTime * _sweepSpeed * Mathf.Deg2Rad;
            float offset = Mathf.Sin(_phase) * _sweepAngle * 0.5f;
            _head.localRotation = Quaternion.Euler(_head.localEulerAngles.x, _baseYaw + offset, 0f);
        }

        private void TrackTarget(float deltaTime)
        {
            if (!_perception.HasLastKnownPosition) return;

            Vector3 direction = _perception.LastKnownPosition - _head.position;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _head.rotation = Quaternion.RotateTowards(_head.rotation, target, 120f * deltaTime);
        }

        private void RaiseAlarm()
        {
            if (_cooldown > 0f) return;

            _cooldown = _alarmCooldown;
            AudioManager.PlaySfx(SoundId.CameraBeep, transform.position);

            if (GameManager.Instance != null) GameManager.Instance.ReportSpotted();
            AlertNetwork.Broadcast(_perception.LastKnownPosition, _profile != null ? _profile.AlertRadius : 25f);
        }

        // ----- IPerceiver -------------------------------------------------------------------------

        public Vector3 EyePosition => _head != null ? _head.position : transform.position;

        public AwarenessLevel Level => _perception != null ? _perception.Level : AwarenessLevel.Unaware;

        public float Detection01 => _perception != null ? _perception.Value01 : 0f;

        public bool IsHunting => Level == AwarenessLevel.Detected;
    }
}
