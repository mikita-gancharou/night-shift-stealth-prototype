using TMPro;
using UnityEngine;

namespace Stealth.UI
{
    /// <summary>
    /// Short teaching messages triggered by walking into a zone ("crouch behind the crates", "throw a stone").
    /// Keeping them in trigger volumes means the level teaches itself instead of needing a manual.
    /// </summary>
    public class HintDisplay : MonoBehaviour
    {
        public static HintDisplay Instance { get; private set; }

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 3f;

        private float _timer;

        private void Awake()
        {
            Instance = this;
            if (_group != null) _group.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

        public void Show(string message, float duration)
        {
            if (_label != null) _label.text = message;
            _timer = duration;
        }

        public static void ShowHint(string message, float duration)
        {
            if (Instance != null) Instance.Show(message, duration);
        }
    }
}
