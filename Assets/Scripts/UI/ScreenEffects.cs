using UnityEngine;
using UnityEngine.UI;
using Stealth.Core;
using Stealth.Perception;
using Stealth.Player;

namespace Stealth.UI
{
    /// <summary>
    /// Full screen feedback: the vignette warms up as somebody detects the player, pulses red during a
    /// chase, turns calm while hidden, and a quick flash marks the exact moment of being spotted.
    /// </summary>
    public class ScreenEffects : MonoBehaviour
    {
        [SerializeField] private Image _vignette;
        [SerializeField] private Image _flash;

        [SerializeField] private Color _detectionColor = new Color(1f, 0.15f, 0.12f);
        [SerializeField] private Color _hiddenColor = new Color(0.1f, 0.8f, 0.55f);
        [SerializeField, Range(0f, 1f)] private float _maxVignetteAlpha = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _hiddenVignetteAlpha = 0.4f;
        [SerializeField, Min(0.1f)] private float _flashFadeSpeed = 2.2f;

        private float _flashAlpha;

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.Spotted += OnSpotted;
            SetAlpha(_vignette, 0f);
            SetAlpha(_flash, 0f);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Spotted -= OnSpotted;
        }

        private void Update()
        {
            UpdateVignette();
            UpdateFlash();
        }

        private void UpdateVignette()
        {
            if (_vignette == null) return;

            PlayerController player = PlayerController.Instance;
            bool hidden = player != null && player.IsHidden;

            float detection = PerceiverRegistry.MaxDetection01;
            bool hunting = PerceiverRegistry.AnyHunting;

            Color color = hidden ? _hiddenColor : _detectionColor;
            float alpha;

            if (hidden)
            {
                alpha = _hiddenVignetteAlpha;
            }
            else if (hunting)
            {
                // Pulse while being chased so the danger never becomes wallpaper.
                alpha = Mathf.Lerp(_maxVignetteAlpha * 0.55f, _maxVignetteAlpha,
                    (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f);
            }
            else
            {
                alpha = detection * _maxVignetteAlpha * 0.7f;
            }

            Color current = _vignette.color;
            Color target = new Color(color.r, color.g, color.b, alpha);
            _vignette.color = Color.Lerp(current, target, 8f * Time.unscaledDeltaTime);
        }

        private void UpdateFlash()
        {
            if (_flash == null || _flashAlpha <= 0f) return;

            _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, _flashFadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(_flash, _flashAlpha);
        }

        private void OnSpotted()
        {
            _flashAlpha = 0.26f;
            SetAlpha(_flash, _flashAlpha);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null) return;

            Color color = image.color;
            image.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}
