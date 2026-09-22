using UnityEngine;
using Stealth.Player;
using Stealth.UI;

namespace Stealth.Level
{
    /// <summary>Volume that teaches one mechanic the first time the player walks through it.</summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField, TextArea(2, 4)] private string _message = "Hold C to crouch.";
        [SerializeField, Min(1f)] private float _duration = 5f;
        [SerializeField] private bool _onlyOnce = true;

        private bool _shown;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_shown && _onlyOnce) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            _shown = true;
            HintDisplay.ShowHint(_message, _duration);
        }
    }
}
