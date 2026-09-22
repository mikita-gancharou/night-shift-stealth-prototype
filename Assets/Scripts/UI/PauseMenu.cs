using UnityEngine;
using UnityEngine.UI;
using Stealth.Core;

namespace Stealth.UI
{
    /// <summary>Escape menu. The manager owns the pause state, this script only mirrors it.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
            if (_menuButton != null) _menuButton.onClick.AddListener(MainMenu);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
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
            if (_panel != null) _panel.SetActive(state == GameState.Paused);
        }

        public void Resume() => GameManager.Instance?.Resume();

        public void Restart() => GameManager.Instance?.RestartLevel();

        public void MainMenu() => GameManager.Instance?.LoadMainMenu();

        public void Quit() => GameManager.Instance?.QuitGame();
    }
}
