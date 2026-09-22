using System;
using UnityEngine;
using Stealth.Perception;

namespace Stealth.Enemies
{
    /// <summary>
    /// Merges the three senses of one observer into a single detection value:
    /// sight (raycast cone), close range presence (sphere zone) and hearing (noise events).
    /// The state machine only reads the result, it never talks to the sensors directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuardPerception : MonoBehaviour, INoiseListener
    {
        [SerializeField] private VisionSensor _vision;
        [SerializeField] private ProximitySensor _proximity;
        [SerializeField] private DetectionMeter _meter = new DetectionMeter();

        [Header("Hearing")]
        [SerializeField, Min(1f)] private float _hearingRadius = 18f;
        [SerializeField, Range(0f, 1f)] private float _noiseSensitivity = 0.3f;

        [Tooltip("How loud a sound has to be before the guard walks over to check it. Quiet steps only " +
                 "raise suspicion, sprinting, gravel and thrown stones pull the guard off its route.")]
        [SerializeField, Range(0f, 1f)] private float _investigateThreshold = 0.5f;

        public DetectionMeter Meter => _meter;
        public VisionSensor Vision => _vision;
        public ProximitySensor Proximity => _proximity;

        public AwarenessLevel Level => _meter.Level;
        public float Value01 => _meter.Value;

        /// <summary>True while the guard has an actual line of sight on the player right now.</summary>
        public bool HasVisual { get; private set; }

        public float TimeSinceVisual { get; private set; } = float.MaxValue;

        /// <summary>Best guess of where the player is. Guards walk here when they lose sight.</summary>
        public Vector3 LastKnownPosition { get; private set; }

        public bool HasLastKnownPosition { get; private set; }

        /// <summary>Set by a noise or a radio call: a place worth checking, even without ever seeing anyone.</summary>
        public bool HasStimulus { get; private set; }

        public Vector3 StimulusPosition { get; private set; }

        public event Action<AwarenessLevel, AwarenessLevel> LevelChanged;

        public float HearingRadius => _hearingRadius;
        public Vector3 EarPosition => transform.position;

        private void OnEnable()
        {
            _meter.LevelChanged += OnMeterLevelChanged;
            NoiseSystem.Register(this);
            StealthTargetLocator.TargetHidden += OnTargetHidden;
        }

        private void OnDisable()
        {
            _meter.LevelChanged -= OnMeterLevelChanged;
            NoiseSystem.Unregister(this);
            StealthTargetLocator.TargetHidden -= OnTargetHidden;
        }

        /// <summary>Called once per frame by the observer that owns this component.</summary>
        public void Tick(float deltaTime)
        {
            IStealthTarget target = StealthTargetLocator.Current;
            float exposure = 0f;
            HasVisual = false;

            if (target != null && !target.IsHidden)
            {
                float sight = _vision != null ? _vision.SampleExposure(target) : 0f;
                float presence = _proximity != null ? _proximity.SampleExposure(target) : 0f;

                exposure = Mathf.Max(sight, presence);
                HasVisual = sight > 0f;

                if (exposure > 0f)
                {
                    LastKnownPosition = target.Center;
                    HasLastKnownPosition = true;
                }
            }

            TimeSinceVisual = HasVisual ? 0f : TimeSinceVisual + deltaTime;
            _meter.Tick(exposure, deltaTime);
        }

        /// <summary>Pins the meter at full while a guard keeps eyes on the player during a chase.</summary>
        public void HoldDetected() => _meter.Fill();

        public void ConsumeStimulus() => HasStimulus = false;

        public void SetStimulus(Vector3 position)
        {
            HasStimulus = true;
            StimulusPosition = position;
            LastKnownPosition = position;
            HasLastKnownPosition = true;
        }

        /// <summary>Radio call: the guard is told where the player was seen and rushes there already alerted.</summary>
        public void ReceiveAlert(Vector3 position)
        {
            SetStimulus(position);
            if (_meter.Value < _meter.AlertedThreshold) _meter.Drop(_meter.AlertedThreshold + 0.05f);
        }

        /// <summary>The player slipped into a hiding spot: drop the trail but keep a little suspicion.</summary>
        public void ForgetTarget()
        {
            HasVisual = false;
            HasStimulus = false;
            TimeSinceVisual = float.MaxValue;
            _meter.Drop(_meter.SuspiciousThreshold * 0.5f);
        }

        public void OnNoiseHeard(NoiseEvent noise)
        {
            if (!isActiveAndEnabled) return;

            _meter.AddInstant(noise.Intensity * _noiseSensitivity);

            if (noise.Intensity >= _investigateThreshold)
            {
                // Loud enough to be worth walking over: a sprint, gravel underfoot, a thrown stone.
                SetStimulus(noise.Position);
                return;
            }

            // Quiet steps only make the guard wary and give it a rough direction.
            LastKnownPosition = noise.Position;
            HasLastKnownPosition = true;
        }

        public void ApplySettings(float hearingRadius, float noiseSensitivity)
        {
            _hearingRadius = hearingRadius;
            _noiseSensitivity = noiseSensitivity;
        }

        private void OnTargetHidden()
        {
            ForgetTarget();
        }

        private void OnMeterLevelChanged(AwarenessLevel previous, AwarenessLevel current)
        {
            LevelChanged?.Invoke(previous, current);
        }
    }
}
