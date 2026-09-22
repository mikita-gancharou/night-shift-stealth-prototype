using UnityEngine;
using Stealth.Level;

namespace Stealth.Audio
{
    /// <summary>
    /// Small pooled sound player. Gameplay code calls the static helpers and never worries about
    /// AudioSources; if no manager exists (unit tests, bare scenes) the calls simply do nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundLibrary _library;
        [SerializeField, Min(1)] private int _poolSize = 10;
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.9f;
        [SerializeField, Min(1f)] private float _maxDistance = 34f;

        [Header("Footsteps")]
        [Tooltip("Footsteps fire twice a second, so they are mixed well below the one-shot cues.")]
        [SerializeField, Range(0f, 1f)] private float _crouchStepVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _walkStepVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _sprintStepVolume = 0.9f;

        private AudioSource[] _pool;
        private AudioSource _uiSource;
        private int _next;

        public SoundLibrary Library => _library;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildPool()
        {
            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject holder = new GameObject($"SfxSource_{i}");
                holder.transform.SetParent(transform, false);

                AudioSource source = holder.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3f;
                source.maxDistance = _maxDistance;
                _pool[i] = source;
            }

            GameObject uiHolder = new GameObject("UiSource");
            uiHolder.transform.SetParent(transform, false);
            _uiSource = uiHolder.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f;
            _uiSource.ignoreListenerPause = true;
        }

        // ----- static helpers ---------------------------------------------------------------------

        public static void PlaySfx(SoundId id, Vector3 position, float volumeScale = 1f)
        {
            Instance?.PlayAt(id, position, volumeScale);
        }

        public static void PlaySfx2D(SoundId id, float volumeScale = 1f)
        {
            Instance?.PlayUi(id, volumeScale);
        }

        public static void PlayFootstep(Vector3 position, SurfaceKind surface, StepIntensity intensity)
        {
            AudioManager manager = Instance;
            if (manager == null || manager._library == null) return;

            AudioClip clip = manager._library.GetFootstep(surface, intensity == StepIntensity.Crouch);
            if (clip == null) return;

            float volume = intensity switch
            {
                StepIntensity.Crouch => manager._crouchStepVolume,
                StepIntensity.Sprint => manager._sprintStepVolume,
                _ => manager._walkStepVolume
            };

            // Steps land twice a second: without a spread on level and pitch they turn into a machine gun.
            manager.PlayClipAt(clip, position, volume * Random.Range(0.86f, 1.14f), Random.Range(0.92f, 1.08f));
        }

        // ----- instance ---------------------------------------------------------------------------

        private void PlayAt(SoundId id, Vector3 position, float volumeScale)
        {
            if (_library == null) return;

            AudioClip clip = _library.GetClip(id, out float volume);
            if (clip == null) return;

            PlayClipAt(clip, position, volume * volumeScale, Random.Range(0.97f, 1.04f));
        }

        private void PlayUi(SoundId id, float volumeScale)
        {
            if (_library == null || _uiSource == null) return;

            AudioClip clip = _library.GetClip(id, out float volume);
            if (clip == null) return;

            _uiSource.pitch = 1f;
            _uiSource.PlayOneShot(clip, volume * volumeScale * _masterVolume);
        }

        private void PlayClipAt(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (_pool == null || _pool.Length == 0) return;

            AudioSource source = _pool[_next];
            _next = (_next + 1) % _pool.Length;

            source.transform.position = position;
            source.clip = clip;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume * _masterVolume);
            source.Play();
        }
    }
}
