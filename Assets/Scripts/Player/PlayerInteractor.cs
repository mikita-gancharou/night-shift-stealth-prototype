using System;
using UnityEngine;
using Stealth.Interaction;

namespace Stealth.Player
{
    /// <summary>
    /// Finds the best interactable around the player (hiding spots, stone piles, intel, light switches)
    /// and forwards the interact key to it. The HUD listens to <see cref="CurrentChanged"/> for the prompt.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField, Min(0.5f)] private float _radius = 2.4f;
        [SerializeField] private LayerMask _interactableMask;

        public IInteractable Current { get; private set; }

        /// <summary>Raised with the new candidate (or null) whenever the interaction target changes.</summary>
        public event Action<IInteractable> CurrentChanged;

        private void Update()
        {
            IInteractable best = FindBest();
            if (ReferenceEquals(best, Current)) return;

            Current = best;
            CurrentChanged?.Invoke(Current);
        }

        public void TryInteract()
        {
            if (Current == null) return;
            if (!Current.CanInteract(_player)) return;

            Current.Interact(_player);
        }

        private IInteractable FindBest()
        {
            // While hidden the only sensible action is leaving the spot again.
            if (_player != null && _player.IsHidden) return _player.Hiding.CurrentSpot;

            Collider[] candidates = Physics.OverlapSphere(transform.position, _radius, _interactableMask,
                QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < candidates.Length; i++)
            {
                IInteractable interactable = candidates[i].GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(_player)) continue;

                Vector3 toTarget = candidates[i].bounds.center - transform.position;
                float distance = toTarget.magnitude;

                // Prefer what the player is facing, then what is closest.
                float facing = Vector3.Dot(transform.forward, toTarget.normalized);
                float score = distance - facing * 1.2f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
