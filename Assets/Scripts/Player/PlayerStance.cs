using System;
using UnityEngine;

namespace Stealth.Player
{
    /// <summary>
    /// Standing / crouching posture. Crouching shrinks the capsule and the visual body, which is what
    /// makes low cover work: the head sample point drops behind crates that a standing player is seen over.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [DisallowMultipleComponent]
    public class PlayerStance : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;

        [SerializeField, Min(0.5f)] private float _standHeight = 1.8f;
        [SerializeField, Min(0.4f)] private float _crouchHeight = 1.05f;
        [SerializeField, Min(0.5f)] private float _transitionSpeed = 8f;

        public bool IsCrouching { get; private set; }

        /// <summary>Smoothed capsule height, between crouch and stand height.</summary>
        public float CurrentHeight { get; private set; }

        public float StandHeight => _standHeight;
        public float CrouchHeight => _crouchHeight;

        /// <summary>0 while fully crouched, 1 while fully standing. Used by visibility and by the HUD.</summary>
        public float StandFactor => Mathf.InverseLerp(_crouchHeight, _standHeight, CurrentHeight);

        public event Action<bool> CrouchChanged;

        private CapsuleCollider _capsule;
        private Vector3 _visualBaseScale = Vector3.one;

        private void Awake()
        {
            _capsule = GetComponent<CapsuleCollider>();
            CurrentHeight = _standHeight;
            if (_visualRoot != null) _visualBaseScale = _visualRoot.localScale;
            ApplyHeight();
        }

        private void Update()
        {
            float target = IsCrouching ? _crouchHeight : _standHeight;
            if (Mathf.Approximately(CurrentHeight, target)) return;

            CurrentHeight = Mathf.MoveTowards(CurrentHeight, target, _transitionSpeed * Time.deltaTime);
            ApplyHeight();
        }

        public void Toggle() => SetCrouch(!IsCrouching);

        public void SetCrouch(bool crouching)
        {
            if (IsCrouching == crouching) return;

            IsCrouching = crouching;
            CrouchChanged?.Invoke(IsCrouching);
        }

        private void ApplyHeight()
        {
            _capsule.height = CurrentHeight;
            _capsule.center = new Vector3(0f, CurrentHeight * 0.5f, 0f);

            if (_visualRoot != null)
            {
                float scale = CurrentHeight / _standHeight;
                _visualRoot.localScale = new Vector3(_visualBaseScale.x, _visualBaseScale.y * scale, _visualBaseScale.z);
            }
        }
    }
}
