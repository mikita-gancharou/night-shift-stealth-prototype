using UnityEngine;
using Stealth.Audio;
using Stealth.Core;
using Stealth.Player;

namespace Stealth.Interaction
{
    /// <summary>
    /// Optional objective: documents hidden in the riskiest corners of the level. They do not gate the exit,
    /// they only improve the end screen rank, which gives confident players a reason to take detours.
    /// </summary>
    public class IntelPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject _visualRoot;

        private bool _collected;

        public string Prompt => "Take intel";

        public Vector3 AnchorPosition => transform.position + Vector3.up * 0.5f;

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.RegisterIntel();
        }

        public bool CanInteract(PlayerController player) => !_collected;

        public void Interact(PlayerController player)
        {
            if (_collected) return;

            _collected = true;
            AudioManager.PlaySfx(SoundId.PickupIntel, transform.position);

            if (GameManager.Instance != null) GameManager.Instance.ReportIntelCollected();
            if (_visualRoot != null) _visualRoot.SetActive(false);

            enabled = false;
        }
    }
}
