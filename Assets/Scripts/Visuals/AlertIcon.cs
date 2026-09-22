using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Stealth.Perception;

namespace Stealth.Visuals
{
    /// <summary>
    /// The "?" / "!" badge above an enemy. The ring fills with the detection meter, so the player can watch
    /// the progressive detection happen and react before it completes.
    /// </summary>
    public class AlertIcon : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _ring;
        [SerializeField] private TextMeshProUGUI _symbol;
        [SerializeField] private Transform _scaleRoot;

        [SerializeField, Min(0.1f)] private float _fadeSpeed = 6f;
        [SerializeField] private float _detectedPunch = 1.25f;

        private float _targetAlpha;
        private float _punch;

        private void Reset()
        {
            _group = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (_group != null) _group.alpha = 0f;
        }

        private void Update()
        {
            if (_group != null)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
            }

            if (_scaleRoot != null)
            {
                _punch = Mathf.MoveTowards(_punch, 1f, 3f * Time.deltaTime);
                _scaleRoot.localScale = Vector3.one * _punch;
            }
        }

        public void Apply(AwarenessLevel level, float detection01, Color color)
        {
            _targetAlpha = level == AwarenessLevel.Unaware && detection01 <= 0.02f ? 0f : 1f;

            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Clamp01(detection01);
                _fill.color = color;
            }

            if (_ring != null) _ring.color = new Color(color.r, color.g, color.b, 0.45f);

            if (_symbol != null)
            {
                _symbol.text = level == AwarenessLevel.Detected ? "!" : "?";
                _symbol.color = color;
            }

            if (level == AwarenessLevel.Detected && _punch >= 1f) _punch = _detectedPunch;
        }
    }
}
