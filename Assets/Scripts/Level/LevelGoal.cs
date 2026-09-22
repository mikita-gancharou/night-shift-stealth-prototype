using UnityEngine;
using Stealth.Core;
using Stealth.Player;

namespace Stealth.Level
{
    /// <summary>Extraction point. Reaching it with the player is the win condition of the mission.</summary>
    [RequireComponent(typeof(Collider))]
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private string _objectiveText = "Reach the extraction point";

        public string ObjectiveText => _objectiveText;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsHidden) return;

            if (GameManager.Instance != null) GameManager.Instance.ReportGoalReached();
        }
    }
}
