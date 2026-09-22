using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Stealth.Core;

namespace Stealth.UI
{
    /// <summary>Victory / failure panel with the run summary and the rank earned.</summary>
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _subtitle;
        [SerializeField] private TextMeshProUGUI _stats;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;

        [SerializeField] private Color _winColor = new Color(0.45f, 1f, 0.7f);
        [SerializeField] private Color _loseColor = new Color(1f, 0.35f, 0.35f);

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_retryButton != null) _retryButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
            if (_menuButton != null) _menuButton.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Won && state != GameState.Lost) return;

            StartCoroutine(ShowAfterDelay(state));
        }

        private IEnumerator ShowAfterDelay(GameState state)
        {
            GameManager game = GameManager.Instance;
            float delay = game != null ? game.EndScreenDelay : 1f;
            yield return new WaitForSecondsRealtime(delay);

            Show(state);
        }

        private void Show(GameState state)
        {
            if (_panel != null) _panel.SetActive(true);

            GameManager game = GameManager.Instance;
            MissionStats stats = game != null ? game.Stats : new MissionStats();
            bool won = state == GameState.Won;

            if (_title != null)
            {
                _title.text = won ? "MISSION COMPLETE" : "CAUGHT";
                _title.color = won ? _winColor : _loseColor;
            }

            if (_subtitle != null)
            {
                _subtitle.text = won ? $"RANK:  {stats.Rank}" : "A guard got their hands on you.";
                _subtitle.color = won ? _winColor : _loseColor;
            }

            if (_stats != null)
            {
                _stats.text =
                    $"Time            {stats.TimeText}\n" +
                    $"Times spotted   {stats.TimesSpotted}\n" +
                    $"Stones thrown   {stats.StonesThrown}\n" +
                    $"Intel recovered {stats.IntelCollected}/{Mathf.Max(stats.IntelTotal, stats.IntelCollected)}";
            }
        }
    }
}
