using UnityEngine;

namespace Stealth.Player
{
    /// <summary>
    /// The only script that touches Unity's input API. Everything else reads these properties,
    /// so rebinding keys or swapping to another input backend stays a one file change.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private KeyCode _crouchKey = KeyCode.C;
        [SerializeField] private KeyCode _crouchKeyAlt = KeyCode.LeftControl;
        [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;

        [Header("Mouse")]
        [SerializeField, Range(0.2f, 8f)] private float _mouseSensitivity = 2.2f;
        [SerializeField] private bool _invertY;

        /// <summary>WASD direction, x = strafe, y = forward.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>Mouse movement of this frame, already scaled by sensitivity.</summary>
        public Vector2 Look { get; private set; }

        public bool SprintHeld { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool AimHeld { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool PausePressed { get; private set; }

        /// <summary>Set while a menu is open or the mission is over: movement input is ignored, pause is not.</summary>
        public bool GameplayBlocked { get; set; }

        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set => _mouseSensitivity = Mathf.Clamp(value, 0.2f, 8f);
        }

        private void Update()
        {
            PausePressed = Input.GetKeyDown(_pauseKey);

            if (GameplayBlocked)
            {
                Move = Vector2.zero;
                Look = Vector2.zero;
                SprintHeld = false;
                CrouchPressed = false;
                InteractPressed = false;
                AimHeld = false;
                ThrowPressed = false;
                return;
            }

            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Move.sqrMagnitude > 1f) Move = Move.normalized;

            float lookY = Input.GetAxis("Mouse Y") * (_invertY ? 1f : -1f);
            Look = new Vector2(Input.GetAxis("Mouse X"), lookY) * _mouseSensitivity;

            SprintHeld = Input.GetKey(_sprintKey);
            CrouchPressed = Input.GetKeyDown(_crouchKey) || Input.GetKeyDown(_crouchKeyAlt);
            InteractPressed = Input.GetKeyDown(_interactKey);
            AimHeld = Input.GetMouseButton(1);
            ThrowPressed = Input.GetMouseButtonDown(0);
        }
    }
}
