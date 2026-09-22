using TMPro;
using UnityEngine;
using Stealth.Interaction;
using Stealth.Player;

namespace Stealth.UI
{
    /// <summary>Shows "[E] Hide in container" style prompts whenever something is in reach.</summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _keyName = "E";
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 8f;

        private void Update()
        {
            if (_group == null) return;

            PlayerController player = PlayerController.Instance;
            IInteractable target = player != null && player.Interactor != null ? player.Interactor.Current : null;

            bool visible = target != null;
            if (visible && _label != null) _label.text = $"[{_keyName}]  {target.Prompt}";

            _group.alpha = Mathf.MoveTowards(_group.alpha, visible ? 1f : 0f, _fadeSpeed * Time.deltaTime);
        }
    }
}
