using TMPro;
using UnityEngine;

namespace Stealth.Visuals
{
    /// <summary>Short floating line above a guard. Fades itself out after the given duration.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 4f;

        private float _timer;

        private void Awake()
        {
            if (_group != null) _group.alpha = 0f;
        }

        private void Update()
        {
            if (_group == null) return;

            if (_timer > 0f)
            {
                _timer -= Time.deltaTime;
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, _fadeSpeed * Time.deltaTime);
            }
            else
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, _fadeSpeed * Time.deltaTime);
            }
        }

        public void Show(string text, float duration)
        {
            if (_label != null) _label.text = text;
            _timer = duration;
        }
    }
}
