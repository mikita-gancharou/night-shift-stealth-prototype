using System.Collections.Generic;
using UnityEngine;
using Stealth.Level;

namespace Stealth.Player
{
    /// <summary>
    /// How easy the player is to spot right now: posture, speed and the light they are standing in.
    /// Guards multiply their sight exposure by this value, so crouching in an unlit corner really pays off.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerVisibility : MonoBehaviour
    {
        [Header("Posture")]
        [SerializeField, Min(0.05f)] private float _crouchFactor = 0.55f;
        [SerializeField, Min(0.05f)] private float _standFactor = 1f;
        [SerializeField, Min(0.05f)] private float _sprintFactor = 1.25f;

        [Header("Light")]
        [Tooltip("Multiplier applied when the player is not standing inside any lit zone.")]
        [SerializeField, Min(0.05f)] private float _darknessFactor = 0.6f;

        [SerializeField] private PlayerStance _stance;
        [SerializeField] private PlayerMotor _motor;

        private readonly List<LightZone> _zones = new List<LightZone>(4);

        /// <summary>Final multiplier handed to the guards' vision sensors.</summary>
        public float Multiplier { get; private set; } = 1f;

        /// <summary>0 = pitch dark, 1 = standing in a spotlight. Only used for HUD feedback.</summary>
        public float LightExposure01 { get; private set; }

        public bool InLight => LightExposure01 > 0.01f;

        private void Update()
        {
            float posture = _standFactor;
            if (_stance != null && _stance.IsCrouching) posture = _crouchFactor;
            else if (_motor != null && _motor.IsSprinting) posture = _sprintFactor;

            float light = _darknessFactor;
            LightExposure01 = 0f;

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                LightZone zone = _zones[i];
                if (zone == null)
                {
                    _zones.RemoveAt(i);
                    continue;
                }

                if (!zone.IsLit) continue;

                light = Mathf.Max(light, zone.VisibilityMultiplier);
                LightExposure01 = Mathf.Max(LightExposure01, zone.Brightness01);
            }

            Multiplier = Mathf.Max(0.05f, posture * light);
        }

        private void OnTriggerEnter(Collider other)
        {
            LightZone zone = other.GetComponent<LightZone>();
            if (zone != null && !_zones.Contains(zone)) _zones.Add(zone);
        }

        private void OnTriggerExit(Collider other)
        {
            LightZone zone = other.GetComponent<LightZone>();
            if (zone != null) _zones.Remove(zone);
        }
    }
}
