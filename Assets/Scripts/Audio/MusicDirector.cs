using UnityEngine;
using Stealth.Core;
using Stealth.Perception;

namespace Stealth.Audio
{
    /// <summary>
    /// Three looping layers that cross-fade with the highest awareness in the level: calm ambience,
    /// uneasy pulse, full chase. The music is the first feedback the player gets that something changed.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private SoundLibrary _library;
        [SerializeField] private AudioSource _ambient;
        [SerializeField] private AudioSource _tension;
        [SerializeField] private AudioSource _chase;

        [SerializeField, Range(0f, 1f)] private float _ambientVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _tensionVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _chaseVolume = 0.45f;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 1.4f;

        private bool _missionOver;

        private void Start()
        {
            Prepare(_ambient, _library != null ? _library.Ambient : null, _ambientVolume);
            Prepare(_tension, _library != null ? _library.Tension : null, 0f);
            Prepare(_chase, _library != null ? _library.Chase : null, 0f);

            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime * _fadeSpeed;

            if (_missionOver)
            {
                Fade(_ambient, 0f, delta);
                Fade(_tension, 0f, delta);
                Fade(_chase, 0f, delta);
                return;
            }

            AwarenessLevel level = PerceiverRegistry.MaxLevel;
            bool hunting = PerceiverRegistry.AnyHunting;

            float ambientTarget = hunting ? 0f : _ambientVolume;
            float tensionTarget = !hunting && level >= AwarenessLevel.Suspicious ? _tensionVolume : 0f;
            float chaseTarget = hunting ? _chaseVolume : 0f;

            Fade(_ambient, ambientTarget, delta);
            Fade(_tension, tensionTarget, delta);
            Fade(_chase, chaseTarget, delta);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Won && state != GameState.Lost) return;

            _missionOver = true;
            AudioManager.PlaySfx2D(state == GameState.Won ? SoundId.Win : SoundId.Lose);
        }

        private static void Prepare(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null) return;

            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            if (clip != null) source.Play();
        }

        private static void Fade(AudioSource source, float target, float delta)
        {
            if (source == null) return;

            source.volume = Mathf.MoveTowards(source.volume, target, delta);
        }
    }
}
