using System;
using UnityEngine;
using Stealth.Audio;
using Stealth.Core;
using Stealth.Level;
using Stealth.Visuals;

namespace Stealth.Player
{
    /// <summary>
    /// The distraction mechanic: aim with the right mouse button, throw a stone with the left one.
    /// Where the stone lands it makes noise, and guards close enough to hear it walk over to investigate.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerThrower : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Stone _stonePrefab;
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private ThrowArc _arc;

        [Header("Throw")]
        [SerializeField, Min(1f)] private float _throwSpeed = 13f;
        [SerializeField, Min(2f)] private float _maxAimDistance = 30f;
        [SerializeField] private LayerMask _aimMask;
        [SerializeField, Min(0.05f)] private float _cooldown = 0.45f;

        [Header("Ammo")]
        [SerializeField, Min(0)] private int _stones = 3;
        [SerializeField, Min(1)] private int _maxStones = 6;

        public int Stones => _stones;
        public int MaxStones => _maxStones;
        public bool IsAiming { get; private set; }
        public bool CanThrow => _stones > 0 && _cooldownTimer <= 0f;

        public event Action<int> StonesChanged;
        public event Action<bool> AimingChanged;

        private Camera _camera;
        private float _cooldownTimer;

        private void Start()
        {
            _camera = Camera.main;
            StonesChanged?.Invoke(_stones);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            if (_arc != null) _arc.SetVisible(IsAiming && CanThrow);
            if (IsAiming && _arc != null && CanThrow)
            {
                Vector3 velocity = ComputeVelocity(out Vector3 target);
                _arc.Draw(Origin, velocity, target);
            }
        }

        public void SetAiming(bool aiming)
        {
            if (IsAiming == aiming) return;

            IsAiming = aiming;
            AimingChanged?.Invoke(aiming);
        }

        public bool TryThrow()
        {
            if (!CanThrow || _stonePrefab == null) return false;

            Vector3 velocity = ComputeVelocity(out _);
            Stone stone = Instantiate(_stonePrefab, Origin, Quaternion.identity);
            stone.Launch(velocity);

            _stones--;
            _cooldownTimer = _cooldown;
            StonesChanged?.Invoke(_stones);

            AudioManager.PlaySfx(SoundId.StoneThrow, Origin);
            if (GameManager.Instance != null) GameManager.Instance.ReportStoneThrown();
            return true;
        }

        /// <summary>Returns how many stones were actually taken (the pouch has a limit).</summary>
        public int AddStones(int amount)
        {
            int before = _stones;
            _stones = Mathf.Clamp(_stones + amount, 0, _maxStones);

            int added = _stones - before;
            if (added > 0) StonesChanged?.Invoke(_stones);
            return added;
        }

        private Vector3 Origin => _throwOrigin != null ? _throwOrigin.position : transform.position + Vector3.up * 1.4f;

        private Vector3 ComputeVelocity(out Vector3 aimPoint)
        {
            aimPoint = ResolveAimPoint();
            BallisticSolver.TrySolve(Origin, aimPoint, _throwSpeed, out Vector3 velocity);
            return velocity;
        }

        /// <summary>Where the camera is pointing, projected onto the level.</summary>
        private Vector3 ResolveAimPoint()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return transform.position + transform.forward * 8f;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, _maxAimDistance, _aimMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return ray.GetPoint(_maxAimDistance * 0.6f);
        }
    }
}
