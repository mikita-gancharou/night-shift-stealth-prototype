using UnityEngine;

namespace Stealth.Level
{
    public enum SurfaceKind
    {
        Concrete,
        Gravel,
        Metal
    }

    /// <summary>
    /// A patch of ground that changes how loud the player is. Gravel and metal walkways force the player
    /// to slow down and crouch instead of running straight through.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SurfaceZone : MonoBehaviour
    {
        [SerializeField] private SurfaceKind _surface = SurfaceKind.Gravel;

        [Tooltip("Multiplies both the footstep noise radius and the loudness of the player.")]
        [SerializeField, Min(0.1f)] private float _noiseMultiplier = 1.7f;

        public SurfaceKind Surface => _surface;
        public float NoiseMultiplier => _noiseMultiplier;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
