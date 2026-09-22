using UnityEngine;

namespace Stealth.Level
{
    /// <summary>
    /// The lit volume under a lamp. While the player stands inside one, guards see them noticeably faster.
    /// If the zone belongs to a <see cref="LightGroup"/> that has been switched off, the zone stops counting.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LightZone : MonoBehaviour
    {
        [Tooltip("Visibility multiplier applied to the player inside this zone.")]
        [SerializeField, Min(0.1f)] private float _visibilityMultiplier = 1.15f;

        [Tooltip("Only used for HUD feedback, 1 = bright spotlight.")]
        [SerializeField, Range(0f, 1f)] private float _brightness = 1f;

        [SerializeField] private LightGroup _group;

        public bool IsLit => _group == null || _group.IsOn;
        public float VisibilityMultiplier => _visibilityMultiplier;
        public float Brightness01 => IsLit ? _brightness : 0f;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
