using System;
using UnityEngine;

namespace Stealth.Perception
{
    /// <summary>
    /// The progressive detection value of a single observer (guard or camera).
    /// It fills while the observer is exposed to the player and drains again after a short grace period,
    /// which is what turns detection into a gradual, readable process instead of an instant switch.
    ///
    /// Plain serializable class on purpose: it holds no scene references, so it can be unit tested.
    /// </summary>
    [Serializable]
    public class DetectionMeter
    {
        [Header("Speed")]
        [Tooltip("Meter units per second at an exposure of 1 (full body, mid range, standing in the light).")]
        [SerializeField, Min(0.01f)] private float _riseSpeed = 0.85f;

        [Tooltip("Meter units per second while nothing is exposed.")]
        [SerializeField, Min(0.01f)] private float _decaySpeed = 0.32f;

        [Tooltip("Seconds the meter holds its value after losing the target before it starts draining.")]
        [SerializeField, Min(0f)] private float _decayDelay = 1.2f;

        [Header("Thresholds")]
        [SerializeField, Range(0.01f, 0.9f)] private float _suspiciousThreshold = 0.15f;
        [SerializeField, Range(0.02f, 0.99f)] private float _alertedThreshold = 0.55f;

        /// <summary>Current fill, 0 = unaware, 1 = fully detected.</summary>
        public float Value { get; private set; }

        public AwarenessLevel Level { get; private set; } = AwarenessLevel.Unaware;

        /// <summary>Raised as (previous, current) whenever the meter crosses a threshold.</summary>
        public event Action<AwarenessLevel, AwarenessLevel> LevelChanged;

        public float SuspiciousThreshold => _suspiciousThreshold;
        public float AlertedThreshold => _alertedThreshold;

        private float _timeSinceExposure;

        /// <summary>
        /// Advances the meter. <paramref name="exposure"/> is 0 when the player cannot be perceived and
        /// grows above 1 when they are close, lit and moving.
        /// </summary>
        public void Tick(float exposure, float deltaTime)
        {
            if (exposure > 0f)
            {
                _timeSinceExposure = 0f;
                Value += _riseSpeed * exposure * deltaTime;
            }
            else
            {
                _timeSinceExposure += deltaTime;
                if (_timeSinceExposure >= _decayDelay) Value -= _decaySpeed * deltaTime;
            }

            Value = Mathf.Clamp01(Value);
            RefreshLevel();
        }

        /// <summary>Instant bump used by discrete stimuli such as a stone landing nearby.</summary>
        public void AddInstant(float amount)
        {
            Value = Mathf.Clamp01(Value + amount);
            _timeSinceExposure = 0f;
            RefreshLevel();
        }

        /// <summary>Pins the meter to full while a guard keeps eyes on the player.</summary>
        public void Fill()
        {
            Value = 1f;
            _timeSinceExposure = 0f;
            RefreshLevel();
        }

        /// <summary>Drops the meter back to a value just under "alerted", used when the player hides.</summary>
        public void Drop(float to = 0f)
        {
            Value = Mathf.Clamp01(to);
            _timeSinceExposure = 0f;
            RefreshLevel();
        }

        public void Reset()
        {
            Value = 0f;
            _timeSinceExposure = 0f;
            RefreshLevel();
        }

        private void RefreshLevel()
        {
            AwarenessLevel next;
            if (Value >= 1f) next = AwarenessLevel.Detected;
            else if (Value >= _alertedThreshold) next = AwarenessLevel.Alerted;
            else if (Value >= _suspiciousThreshold) next = AwarenessLevel.Suspicious;
            else next = AwarenessLevel.Unaware;

            if (next == Level) return;

            AwarenessLevel previous = Level;
            Level = next;
            LevelChanged?.Invoke(previous, next);
        }
    }
}
