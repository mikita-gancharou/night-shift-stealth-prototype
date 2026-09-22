using UnityEngine;
using Stealth.Player;

namespace Stealth.Interaction
{
    /// <summary>
    /// A container, locker or dumpster the player can slip into. While inside, no sensor can perceive the
    /// player and every guard drops the chase, so hiding spots are the safety net of the level.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform _hidePoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private string _displayName = "container";

        public Vector3 HidePoint => _hidePoint != null ? _hidePoint.position : transform.position;
        public Vector3 ExitPoint => _exitPoint != null ? _exitPoint.position : transform.position + transform.forward * 1.4f;

        public string Prompt
        {
            get
            {
                PlayerController player = PlayerController.Instance;
                bool inside = player != null && player.IsHidden && player.Hiding.CurrentSpot == this;
                return inside ? "Come out" : "Hide in " + _displayName;
            }
        }

        public Vector3 AnchorPosition => transform.position + Vector3.up * 1.3f;

        public bool CanInteract(PlayerController player)
        {
            if (player == null) return false;

            // Either the player is free to step in, or they are inside this very spot and want to leave.
            return !player.IsHidden || player.Hiding.CurrentSpot == this;
        }

        public void Interact(PlayerController player)
        {
            if (player == null) return;

            player.Hiding.Toggle(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(HidePoint + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f));
            Gizmos.DrawLine(HidePoint, ExitPoint);
        }
    }
}
