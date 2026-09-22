using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Stealth.Core;

namespace Stealth.UI
{
    /// <summary>Title screen: start the mission, read the controls, quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _controlsPanel;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(Play);
            if (_controlsButton != null) _controlsButton.onClick.AddListener(ToggleControls);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
            if (_controlsPanel != null) _controlsPanel.SetActive(false);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            CursorService.Release();
        }

        public void Play() => SceneManager.LoadScene(SceneNames.Level);

        public void ToggleControls()
        {
            if (_controlsPanel != null) _controlsPanel.SetActive(!_controlsPanel.activeSelf);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
