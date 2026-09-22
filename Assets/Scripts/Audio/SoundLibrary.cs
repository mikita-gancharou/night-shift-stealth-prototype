using System;
using UnityEngine;
using Stealth.Level;

namespace Stealth.Audio
{
    /// <summary>Every one-shot the game can play, referenced by name instead of by clip.</summary>
    public enum SoundId
    {
        StoneThrow,
        StoneImpact,
        PickupStone,
        PickupIntel,
        HideEnter,
        HideExit,
        SwitchToggle,
        GuardSuspicious,
        GuardAlert,
        GuardLost,
        CameraBeep,
        Win,
        Lose,
        UiClick,
        UiHover
    }

    /// <summary>
    /// Asset that maps sound ids to clips, so designers can swap audio without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Stealth/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SoundId Id;
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        [Header("Footsteps")]
        [SerializeField] private AudioClip[] _concreteSteps = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _gravelSteps = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _metalSteps = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _crouchSteps = Array.Empty<AudioClip>();

        [Header("Music")]
        public AudioClip Ambient;
        public AudioClip Tension;
        public AudioClip Chase;

        public AudioClip GetClip(SoundId id, out float volume)
        {
            volume = 1f;

            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry == null || entry.Id != id || entry.Clips == null || entry.Clips.Length == 0) continue;

                volume = entry.Volume;
                return entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
            }

            return null;
        }

        public AudioClip GetFootstep(SurfaceKind surface, bool crouching)
        {
            if (crouching) return Pick(_crouchSteps);

            switch (surface)
            {
                case SurfaceKind.Gravel: return Pick(_gravelSteps);
                case SurfaceKind.Metal: return Pick(_metalSteps);
                default: return Pick(_concreteSteps);
            }
        }

        public void SetEntries(Entry[] entries) => _entries = entries;

        public void SetFootsteps(AudioClip[] concrete, AudioClip[] gravel, AudioClip[] metal, AudioClip[] crouch)
        {
            _concreteSteps = concrete;
            _gravelSteps = gravel;
            _metalSteps = metal;
            _crouchSteps = crouch;
        }

        private static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
