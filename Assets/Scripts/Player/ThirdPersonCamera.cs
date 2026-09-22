using UnityEngine;
using Stealth.Core;

namespace Stealth.Player
{
    /// <summary>
    /// Orbit camera behind the player. It also defines "forward" for the motor, and it pulls itself in
    /// with a sphere cast so walls never end up between the camera and the character.
    /// </summary>
    [DisallowMultipleComponent]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 1.55f, 0f);

        [Header("Orbit")]
        [SerializeField, Min(1f)] private float _distance = 5.2f;
        [SerializeField, Min(1f)] private float _aimDistance = 3.2f;
        [SerializeField] private float _shoulderOffset = 0.6f;
        [SerializeField] private float _aimShoulderOffset = 0.95f;
        [SerializeField] private float _minPitch = -25f;
        [SerializeField] private float _maxPitch = 68f;
        [SerializeField, Min(1f)] private float _followSharpness = 16f;

        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField, Min(0.05f)] private float _collisionRadius = 0.28f;

        [Tooltip("The camera never comes closer than this, so it cannot end up inside the character.")]
        [SerializeField, Min(0.5f)] private float _minDistance = 1.7f;

        private PlayerController _player;
        private PlayerInputReader _input;
        private float _yaw;
        private float _pitch = 16f;
        private float _currentDistance;
        private float _currentShoulder;

        private void Start()
        {
            _player = PlayerController.Instance;
            if (_target == null && _player != null) _target = _player.transform;
            if (_player != null) _input = _player.Input;

            _yaw = _target != null ? _target.eulerAngles.y : 0f;
            _currentDistance = _distance;
            _currentShoulder = _shoulderOffset;

            ApplyImmediate();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            GameManager game = GameManager.Instance;
            bool playing = game == null || game.IsPlaying;

            if (playing && _input != null)
            {
                _yaw += _input.Look.x;
                _pitch = Mathf.Clamp(_pitch + _input.Look.y, _minPitch, _maxPitch);
            }

            bool aiming = _player != null && _player.Thrower != null && _player.Thrower.IsAiming;
            float targetDistance = aiming ? _aimDistance : _distance;
            float targetShoulder = aiming ? _aimShoulderOffset : _shoulderOffset;

            float blend = 1f - Mathf.Exp(-_followSharpness * Mathf.Max(Time.deltaTime, 0.0001f));
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, blend);
            _currentShoulder = Mathf.Lerp(_currentShoulder, targetShoulder, blend);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = PivotPosition();
            Vector3 desired = pivot + rotation * new Vector3(_currentShoulder, 0f, -_currentDistance);

            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, ResolveCollision(pivot, desired), blend), rotation);
        }

        private Vector3 PivotPosition()
        {
            Vector3 offset = _pivotOffset;

            // Follow the crouch so the camera does not float above the character.
            if (_player != null && _player.Stance != null)
            {
                offset.y *= Mathf.Lerp(0.72f, 1f, _player.Stance.StandFactor);
            }

            return _target.position + offset;
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 direction = desired - pivot;
            float distance = direction.magnitude;
            if (distance < 0.01f) return desired;

            direction /= distance;
            if (Physics.SphereCast(pivot, _collisionRadius, direction, out RaycastHit hit, distance,
                    _collisionMask, QueryTriggerInteraction.Ignore))
            {
                return pivot + direction * Mathf.Max(_minDistance, hit.distance - 0.1f);
            }

            return desired;
        }

        private void ApplyImmediate()
        {
            if (_target == null) return;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = PivotPosition();
            Vector3 desired = pivot + rotation * new Vector3(_currentShoulder, 0f, -_currentDistance);
            transform.SetPositionAndRotation(ResolveCollision(pivot, desired), rotation);
        }
    }
}
