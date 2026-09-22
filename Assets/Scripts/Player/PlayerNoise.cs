using System.Collections.Generic;
using UnityEngine;
using Stealth.Audio;
using Stealth.Level;
using Stealth.Perception;

namespace Stealth.Player
{
    /// <summary>
    /// Turns movement into sound: footstep audio plus <see cref="NoiseSystem"/> events that guards can hear.
    /// Crouch-walking is silent, walking is a small bubble, sprinting is a large one, and noisy surfaces
    /// (gravel, metal walkways) multiply the radius.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerNoise : MonoBehaviour
    {
        [Header("Loudness by stance (0 - 1)")]
        [SerializeField, Range(0f, 1f)] private float _crouchLoudness = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _walkLoudness = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _sprintLoudness = 1f;

        [Header("Noise radius (m)")]
        [SerializeField, Min(0f)] private float _crouchRadius = 0f;
        [SerializeField, Min(0f)] private float _walkRadius = 6f;
        [SerializeField, Min(0f)] private float _sprintRadius = 12f;

        [Header("Footsteps")]
        [SerializeField, Min(0.1f)] private float _crouchStepInterval = 0.62f;
        [SerializeField, Min(0.1f)] private float _walkStepInterval = 0.46f;
        [SerializeField, Min(0.1f)] private float _sprintStepInterval = 0.31f;

        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerStance _stance;

        private readonly List<SurfaceZone> _surfaces = new List<SurfaceZone>(4);
        private float _stepTimer;

        /// <summary>Current loudness, 0 to 1. Read by the guards' proximity sensors.</summary>
        public float NoiseLevel { get; private set; }

        /// <summary>Radius of the last emitted step, used by the HUD noise ring.</summary>
        public float NoiseRadius { get; private set; }

        public SurfaceKind CurrentSurface { get; private set; } = SurfaceKind.Concrete;

        private void Update()
        {
            RefreshSurface();

            bool moving = _motor != null && _motor.IsMoving && _motor.IsGrounded && _motor.MovementEnabled;
            if (!moving)
            {
                NoiseLevel = 0f;
                NoiseRadius = 0f;
                _stepTimer = 0f;
                return;
            }

            bool crouching = _stance != null && _stance.IsCrouching;
            bool sprinting = _motor.IsSprinting;

            float loudness = crouching ? _crouchLoudness : sprinting ? _sprintLoudness : _walkLoudness;
            float radius = crouching ? _crouchRadius : sprinting ? _sprintRadius : _walkRadius;
            float interval = crouching ? _crouchStepInterval : sprinting ? _sprintStepInterval : _walkStepInterval;

            float surfaceMultiplier = SurfaceMultiplier();
            NoiseLevel = Mathf.Clamp01(loudness * surfaceMultiplier);
            NoiseRadius = radius * surfaceMultiplier;

            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;

            _stepTimer = interval;
            Step(crouching ? StepIntensity.Crouch : sprinting ? StepIntensity.Sprint : StepIntensity.Walk);
        }

        private void Step(StepIntensity intensity)
        {
            AudioManager.PlayFootstep(transform.position, CurrentSurface, intensity);

            if (NoiseRadius <= 0.01f || NoiseLevel <= 0.01f) return;

            NoiseSystem.Emit(transform.position, NoiseRadius, NoiseLevel, NoiseKind.Footstep);
        }

        private float SurfaceMultiplier()
        {
            float multiplier = 1f;
            for (int i = 0; i < _surfaces.Count; i++)
            {
                if (_surfaces[i] != null) multiplier = Mathf.Max(multiplier, _surfaces[i].NoiseMultiplier);
            }

            return multiplier;
        }

        private void RefreshSurface()
        {
            SurfaceKind kind = SurfaceKind.Concrete;
            float loudest = 0f;

            for (int i = _surfaces.Count - 1; i >= 0; i--)
            {
                SurfaceZone zone = _surfaces[i];
                if (zone == null)
                {
                    _surfaces.RemoveAt(i);
                    continue;
                }

                if (zone.NoiseMultiplier > loudest)
                {
                    loudest = zone.NoiseMultiplier;
                    kind = zone.Surface;
                }
            }

            CurrentSurface = kind;
        }

        private void OnTriggerEnter(Collider other)
        {
            SurfaceZone zone = other.GetComponent<SurfaceZone>();
            if (zone != null && !_surfaces.Contains(zone)) _surfaces.Add(zone);
        }

        private void OnTriggerExit(Collider other)
        {
            SurfaceZone zone = other.GetComponent<SurfaceZone>();
            if (zone != null) _surfaces.Remove(zone);
        }
    }
}
