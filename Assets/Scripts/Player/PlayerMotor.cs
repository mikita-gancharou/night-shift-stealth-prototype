using UnityEngine;

namespace Stealth.Player
{
    /// <summary>
    /// Rigidbody driven character movement (the assignment asks for Unity's physics system).
    /// Input is interpreted relative to the camera, and the body is turned towards the movement direction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        [SerializeField, Min(0.1f)] private float _crouchSpeed = 1.7f;
        [SerializeField, Min(0.1f)] private float _walkSpeed = 3.4f;
        [SerializeField, Min(0.1f)] private float _sprintSpeed = 5.8f;

        [Header("Feel")]
        [SerializeField, Min(1f)] private float _acceleration = 22f;
        [SerializeField, Min(1f)] private float _deceleration = 30f;
        [SerializeField, Min(90f)] private float _turnSpeed = 720f;

        [Header("Ground check")]
        [SerializeField] private LayerMask _groundMask;
        [SerializeField, Min(0.05f)] private float _groundProbeRadius = 0.28f;

        private Rigidbody _rigidbody;
        private PlayerStance _stance;
        private Transform _cameraTransform;

        private Vector2 _moveInput;
        private bool _sprintRequested;

        public bool MovementEnabled { get; set; } = true;
        public bool IsGrounded { get; private set; }

        /// <summary>Horizontal speed in m/s, handy for footsteps, noise and animation.</summary>
        public float PlanarSpeed { get; private set; }

        /// <summary>True while the player asked to sprint and is actually able to.</summary>
        public bool IsSprinting { get; private set; }

        public bool IsMoving => PlanarSpeed > 0.15f;

        public float CurrentMaxSpeed
        {
            get
            {
                if (_stance != null && _stance.IsCrouching) return _crouchSpeed;
                return _sprintRequested ? _sprintSpeed : _walkSpeed;
            }
        }

        /// <summary>Speed as a fraction of the sprint speed, used by the noise model and the HUD.</summary>
        public float SpeedFactor => Mathf.Clamp01(PlanarSpeed / _sprintSpeed);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _stance = GetComponent<PlayerStance>();

            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        /// <summary>Called once per frame by <see cref="PlayerController"/> with the current input.</summary>
        public void SetInput(Vector2 move, bool sprint)
        {
            _moveInput = move;
            _sprintRequested = sprint && (_stance == null || !_stance.IsCrouching);
        }

        private void FixedUpdate()
        {
            IsGrounded = Physics.CheckSphere(transform.position + Vector3.up * _groundProbeRadius,
                _groundProbeRadius * 1.05f, _groundMask, QueryTriggerInteraction.Ignore);

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);

            Vector3 desired = Vector3.zero;
            if (MovementEnabled && _moveInput.sqrMagnitude > 0.0001f)
            {
                desired = CameraRelativeDirection(_moveInput) * CurrentMaxSpeed;
            }

            float rate = desired.sqrMagnitude > planar.sqrMagnitude ? _acceleration : _deceleration;
            planar = Vector3.MoveTowards(planar, desired, rate * Time.fixedDeltaTime);

            _rigidbody.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);
            PlanarSpeed = planar.magnitude;
            IsSprinting = _sprintRequested && PlanarSpeed > _walkSpeed * 0.9f;

            if (planar.sqrMagnitude > 0.04f) FaceDirection(planar);
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (_cameraTransform != null)
            {
                forward = _cameraTransform.forward;
                right = _cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void FaceDirection(Vector3 direction)
        {
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, target, _turnSpeed * Time.fixedDeltaTime));
        }

        /// <summary>Hard stop used when the player enters a hiding spot or the mission ends.</summary>
        public void Halt()
        {
            _moveInput = Vector2.zero;
            PlanarSpeed = 0f;
            IsSprinting = false;

            if (_rigidbody != null)
            {
                Vector3 velocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
            }
        }

        /// <summary>Teleports the body safely (used by hiding spots and by the automated tests).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Halt();
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
