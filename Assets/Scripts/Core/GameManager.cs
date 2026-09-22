using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stealth.Core
{
    /// <summary>
    /// Owns the win / lose / pause flow of a mission and the run statistics.
    /// Gameplay systems never talk to each other through this class: they only report what happened
    /// (spotted, caught, extraction reached) and the manager decides the outcome.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Rules")]
        [Tooltip("Basic rule of the assignment (Nota 4): the mission fails the moment a guard detects the player. " +
                 "Left off because the intermediate rule (Nota 7) replaces it - the player only fails when a guard reaches them.")]
        [SerializeField] private bool _loseWhenSpotted;

        [Tooltip("Seconds of feedback (sting, red flash) before the end screen takes over.")]
        [SerializeField] private float _endScreenDelay = 1.1f;

        public GameState State { get; private set; } = GameState.Playing;
        public MissionStats Stats { get; } = new MissionStats();
        public bool IsPlaying => State == GameState.Playing;

        /// <summary>How long the feedback plays before the end screen appears.</summary>
        public float EndScreenDelay => _endScreenDelay;

        /// <summary>Raised on every state change (playing / paused / won / lost).</summary>
        public event Action<GameState> StateChanged;

        /// <summary>Raised the moment a guard switches to full detection. Used for stingers and screen flashes.</summary>
        public event Action Spotted;

        /// <summary>Raised when any counter in <see cref="Stats"/> changes.</summary>
        public event Action<MissionStats> StatsChanged;

        private bool _outcomeDecided;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            CursorService.Capture();
        }

        private void Update()
        {
            if (State == GameState.Playing) Stats.TimeSeconds += Time.deltaTime;
        }

        // ----- reports coming from gameplay systems ---------------------------------------------

        /// <summary>A guard just reached full detection.</summary>
        public void ReportSpotted()
        {
            if (!IsPlaying) return;

            Stats.TimesSpotted++;
            StatsChanged?.Invoke(Stats);
            Spotted?.Invoke();

            if (_loseWhenSpotted) ReportPlayerCaught();
        }

        /// <summary>A chasing guard reached the player: mission failed.</summary>
        public void ReportPlayerCaught()
        {
            if (!IsPlaying || _outcomeDecided) return;

            _outcomeDecided = true;
            StartCoroutine(FinishMission(GameState.Lost));
        }

        /// <summary>The player entered the extraction zone: mission complete.</summary>
        public void ReportGoalReached()
        {
            if (!IsPlaying || _outcomeDecided) return;

            _outcomeDecided = true;
            StartCoroutine(FinishMission(GameState.Won));
        }

        public void ReportStoneThrown()
        {
            Stats.StonesThrown++;
            StatsChanged?.Invoke(Stats);
        }

        public void ReportIntelCollected()
        {
            Stats.IntelCollected++;
            StatsChanged?.Invoke(Stats);
        }

        /// <summary>Called by every intel pickup while it wakes up, so the HUD knows how many exist.</summary>
        public void RegisterIntel()
        {
            Stats.IntelTotal++;
            StatsChanged?.Invoke(Stats);
        }

        // ----- flow -----------------------------------------------------------------------------

        private IEnumerator FinishMission(GameState result)
        {
            SetState(result);
            yield return new WaitForSecondsRealtime(_endScreenDelay);

            Time.timeScale = 0f;
            CursorService.Release();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;

            Time.timeScale = 0f;
            CursorService.Release();
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;

            Time.timeScale = 1f;
            CursorService.Capture();
            SetState(GameState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Level);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            CursorService.Release();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(GameState state)
        {
            if (State == state) return;

            State = state;
            StateChanged?.Invoke(State);
        }
    }
}
