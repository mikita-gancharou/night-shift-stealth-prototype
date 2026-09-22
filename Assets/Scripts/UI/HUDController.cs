using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Stealth.Core;
using Stealth.Enemies;
using Stealth.Level;
using Stealth.Perception;
using Stealth.Player;

namespace Stealth.UI
{
    /// <summary>
    /// Reads the game state once per frame and mirrors it on screen: objective, timer, stance, how visible
    /// and how loud the player currently is, how many stones are left and what the guards are doing.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField] private TextMeshProUGUI _objectiveLabel;
        [SerializeField] private TextMeshProUGUI _intelLabel;
        [SerializeField] private LevelGoal _goal;

        [Header("Run")]
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private TextMeshProUGUI _spottedLabel;

        [Header("Player state")]
        [SerializeField] private TextMeshProUGUI _stanceLabel;
        [SerializeField] private TextMeshProUGUI _stoneLabel;
        [SerializeField] private Image _visibilityFill;
        [SerializeField] private Image _noiseFill;

        [Header("Aiming")]
        [SerializeField] private Image _crosshair;

        [Header("Threat")]
        [SerializeField] private Image _detectionFill;
        [SerializeField] private CanvasGroup _bannerGroup;
        [SerializeField] private TextMeshProUGUI _bannerLabel;

        [Header("Colours")]
        [SerializeField] private Color _calm = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color _suspicious = new Color(1f, 0.83f, 0.25f);
        [SerializeField] private Color _alerted = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color _detected = new Color(1f, 0.25f, 0.25f);
        [SerializeField] private Color _hidden = new Color(0.4f, 0.95f, 0.7f);

        private void Start()
        {
            if (_objectiveLabel != null && _goal != null) _objectiveLabel.text = _goal.ObjectiveText;
        }

        private void Update()
        {
            UpdateMission();
            UpdatePlayer();
            UpdateThreat();
        }

        private void UpdateMission()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            MissionStats stats = game.Stats;
            if (_timerLabel != null) _timerLabel.text = stats.TimeText;
            if (_spottedLabel != null) _spottedLabel.text = $"SPOTTED  {stats.TimesSpotted}";
            if (_intelLabel != null) _intelLabel.text = $"INTEL  {stats.IntelCollected}/{Mathf.Max(stats.IntelTotal, stats.IntelCollected)}";
        }

        private void UpdatePlayer()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null) return;

            if (_stanceLabel != null)
            {
                string stance = player.IsHidden ? "HIDDEN"
                    : player.Stance.IsCrouching ? "CROUCHED"
                    : player.Motor.IsSprinting ? "SPRINTING"
                    : "STANDING";
                _stanceLabel.text = stance;
                _stanceLabel.color = player.IsHidden ? _hidden : Color.white;
            }

            if (_stoneLabel != null && player.Thrower != null)
            {
                _stoneLabel.text = $"{player.Thrower.Stones}/{player.Thrower.MaxStones}";
            }

            if (_crosshair != null && player.Thrower != null)
            {
                bool aiming = player.Thrower.IsAiming && !player.IsHidden;
                if (_crosshair.enabled != aiming) _crosshair.enabled = aiming;
            }

            if (_visibilityFill != null)
            {
                float visibility = player.IsHidden ? 0f : Mathf.Clamp01(player.VisibilityMultiplier / 1.4f);
                _visibilityFill.fillAmount = Mathf.Lerp(_visibilityFill.fillAmount, visibility, 10f * Time.deltaTime);
                _visibilityFill.color = Color.Lerp(_calm, _detected, visibility);
            }

            if (_noiseFill != null)
            {
                float noise = player.IsHidden ? 0f : Mathf.Clamp01(player.NoiseLevel);
                _noiseFill.fillAmount = Mathf.Lerp(_noiseFill.fillAmount, noise, 10f * Time.deltaTime);
                _noiseFill.color = Color.Lerp(_calm, _alerted, noise);
            }
        }

        private void UpdateThreat()
        {
            float detection = PerceiverRegistry.MaxDetection01;
            AwarenessLevel level = PerceiverRegistry.MaxLevel;
            bool hunting = PerceiverRegistry.AnyHunting;
            PlayerController player = PlayerController.Instance;

            if (_detectionFill != null)
            {
                _detectionFill.fillAmount = detection;
                _detectionFill.color = ColorFor(level);
            }

            if (_bannerLabel == null || _bannerGroup == null) return;

            string text;
            Color color;

            if (hunting)
            {
                text = "SPOTTED - RUN";
                color = _detected;
            }
            else if (level == AwarenessLevel.Alerted)
            {
                text = "SEARCHING";
                color = _alerted;
            }
            else if (level == AwarenessLevel.Suspicious)
            {
                text = "SOMETHING HEARD YOU";
                color = _suspicious;
            }
            else if (player != null && player.IsHidden)
            {
                text = "HIDDEN";
                color = _hidden;
            }
            else
            {
                text = string.Empty;
                color = Color.white;
            }

            _bannerLabel.text = text;
            _bannerLabel.color = color;

            float target = string.IsNullOrEmpty(text) ? 0f : 1f;
            _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, target, 5f * Time.deltaTime);
        }

        private Color ColorFor(AwarenessLevel level)
        {
            switch (level)
            {
                case AwarenessLevel.Suspicious: return _suspicious;
                case AwarenessLevel.Alerted: return _alerted;
                case AwarenessLevel.Detected: return _detected;
                default: return _calm;
            }
        }
    }
}
