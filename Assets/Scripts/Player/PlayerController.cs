using UnityEngine;
using Stealth.Core;
using Stealth.Perception;

namespace Stealth.Player
{
    /// <summary>
    /// Thin facade over the player systems. It routes input to the right component and exposes the player
    /// to the AI through <see cref="IStealthTarget"/> - the guards never reference this class directly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour, IStealthTarget
    {
        public static PlayerController Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerStance _stance;
        [SerializeField] private PlayerNoise _noise;
        [SerializeField] private PlayerVisibility _visibility;
        [SerializeField] private PlayerHiding _hiding;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private PlayerThrower _thrower;

        public PlayerInputReader Input => _input;
        public PlayerMotor Motor => _motor;
        public PlayerStance Stance => _stance;
        public PlayerNoise Noise => _noise;
        public PlayerVisibility Visibility => _visibility;
        public PlayerHiding Hiding => _hiding;
        public PlayerInteractor Interactor => _interactor;
        public PlayerThrower Thrower => _thrower;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            StealthTargetLocator.Register(this);
        }

        private void OnDisable()
        {
            StealthTargetLocator.Unregister(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;

            if (_input.PausePressed && game != null) game.TogglePause();

            bool playing = game == null || game.IsPlaying;
            _input.GameplayBlocked = !playing;

            if (!playing)
            {
                _motor.SetInput(Vector2.zero, false);
                _thrower.SetAiming(false);
                return;
            }

            if (_hiding.IsHidden)
            {
                _motor.SetInput(Vector2.zero, false);
                _thrower.SetAiming(false);
                if (_input.InteractPressed) _interactor.TryInteract();
                return;
            }

            _motor.SetInput(_input.Move, _input.SprintHeld);

            if (_input.CrouchPressed) _stance.Toggle();
            if (_input.InteractPressed) _interactor.TryInteract();

            _thrower.SetAiming(_input.AimHeld);
            if (_input.AimHeld && _input.ThrowPressed) _thrower.TryThrow();
        }

        // ----- IStealthTarget ---------------------------------------------------------------------

        public Transform Transform => transform;

        public Vector3 Center => transform.position + Vector3.up * (CurrentHeight * 0.55f);

        public bool IsHidden => _hiding != null && _hiding.IsHidden;

        public float VisibilityMultiplier => _visibility != null ? _visibility.Multiplier : 1f;

        public float NoiseLevel => _noise != null ? _noise.NoiseLevel : 0f;

        public int GetSamplePoints(Vector3[] buffer)
        {
            float height = CurrentHeight;
            Vector3 feet = transform.position;

            buffer[0] = feet + Vector3.up * (height - 0.18f); // head
            buffer[1] = feet + Vector3.up * (height * 0.55f); // chest
            buffer[2] = feet + Vector3.up * 0.3f;             // legs
            return 3;
        }

        private float CurrentHeight => _stance != null ? _stance.CurrentHeight : 1.8f;
    }
}
