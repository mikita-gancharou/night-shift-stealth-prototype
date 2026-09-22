using UnityEngine;
using Stealth.Audio;
using Stealth.Player;

namespace Stealth.Interaction
{
    /// <summary>Ammo for the distraction mechanic. Refills after a while so the level never softlocks.</summary>
    public class StonePile : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(1)] private int _stonesPerPickup = 3;
        [SerializeField, Min(0f)] private float _refillSeconds = 25f;
        [SerializeField] private GameObject _visualRoot;

        private float _refillTimer;

        public bool IsAvailable => _refillTimer <= 0f;

        public string Prompt => "Pick up stones";

        public Vector3 AnchorPosition => transform.position + Vector3.up * 0.6f;

        private void Update()
        {
            if (_refillTimer <= 0f) return;

            _refillTimer -= Time.deltaTime;
            if (_refillTimer <= 0f && _visualRoot != null) _visualRoot.SetActive(true);
        }

        public bool CanInteract(PlayerController player)
        {
            if (!IsAvailable || player == null || player.Thrower == null) return false;

            return player.Thrower.Stones < player.Thrower.MaxStones;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;

            int added = player.Thrower.AddStones(_stonesPerPickup);
            if (added <= 0) return;

            AudioManager.PlaySfx(SoundId.PickupStone, transform.position);

            _refillTimer = _refillSeconds;
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    }
}
