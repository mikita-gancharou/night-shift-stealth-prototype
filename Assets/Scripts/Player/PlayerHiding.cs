using System;
using UnityEngine;
using Stealth.Audio;
using Stealth.Interaction;
using Stealth.Perception;

namespace Stealth.Player
{
    /// <summary>
    /// Handles sitting inside a hiding spot. While hidden the player cannot be perceived at all,
    /// and every guard that was chasing is told to give up - which is exactly the escape valve the
    /// assignment asks for.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHiding : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerStance _stance;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private GameObject _visualRoot;

        public bool IsHidden { get; private set; }
        public HidingSpot CurrentSpot { get; private set; }

        /// <summary>Raised with the new state whenever the player hides or comes back out.</summary>
        public event Action<bool> HiddenChanged;

        public bool TryHide(HidingSpot spot)
        {
            if (IsHidden || spot == null) return false;

            CurrentSpot = spot;
            IsHidden = true;

            _motor.Halt();
            _motor.MovementEnabled = false;
            _stance.SetCrouch(true);

            if (_rigidbody != null) _rigidbody.isKinematic = true;
            transform.SetPositionAndRotation(spot.HidePoint, spot.transform.rotation);
            if (_visualRoot != null) _visualRoot.SetActive(false);

            AudioManager.PlaySfx(SoundId.HideEnter, transform.position);

            // Everyone hunting the player loses the trail right now.
            StealthTargetLocator.NotifyHidden();
            HiddenChanged?.Invoke(true);
            return true;
        }

        public void LeaveHiding()
        {
            if (!IsHidden) return;

            HidingSpot spot = CurrentSpot;
            CurrentSpot = null;
            IsHidden = false;

            if (_rigidbody != null) _rigidbody.isKinematic = false;
            if (spot != null) transform.position = spot.ExitPoint;
            if (_visualRoot != null) _visualRoot.SetActive(true);

            _motor.MovementEnabled = true;
            AudioManager.PlaySfx(SoundId.HideExit, transform.position);
            HiddenChanged?.Invoke(false);
        }

        public void Toggle(HidingSpot spot)
        {
            if (IsHidden) LeaveHiding();
            else TryHide(spot);
        }
    }
}
