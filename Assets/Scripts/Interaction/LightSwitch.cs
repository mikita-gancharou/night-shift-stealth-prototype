using UnityEngine;
using Stealth.Audio;
using Stealth.Level;
using Stealth.Perception;
using Stealth.Player;

namespace Stealth.Interaction
{
    /// <summary>
    /// Breaker box: kills the lamps of a <see cref="LightGroup"/> so a lit route becomes sneakable.
    /// Flipping it also makes a small noise, so it is not a free action.
    /// </summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private LightGroup _group;
        [SerializeField, Min(0f)] private float _noiseRadius = 5f;
        [SerializeField, Range(0f, 1f)] private float _noiseIntensity = 0.35f;
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private Color _onColor = new Color(1f, 0.75f, 0.2f);
        [SerializeField] private Color _offColor = new Color(0.15f, 0.7f, 1f);

        private MaterialPropertyBlock _block;

        public string Prompt => _group != null && _group.IsOn ? "Cut the power" : "Restore the power";

        public Vector3 AnchorPosition => transform.position + Vector3.up * 0.4f;

        private void Start()
        {
            RefreshIndicator();
        }

        public bool CanInteract(PlayerController player) => _group != null;

        public void Interact(PlayerController player)
        {
            if (_group == null) return;

            _group.Toggle();
            RefreshIndicator();

            AudioManager.PlaySfx(SoundId.SwitchToggle, transform.position);
            NoiseSystem.Emit(transform.position, _noiseRadius, _noiseIntensity, NoiseKind.Machine);
        }

        private void RefreshIndicator()
        {
            if (_indicatorRenderer == null) return;

            _block ??= new MaterialPropertyBlock();
            Color color = _group != null && _group.IsOn ? _onColor : _offColor;

            _indicatorRenderer.GetPropertyBlock(_block);
            _block.SetColor("_Color", color);
            _block.SetColor("_EmissionColor", color * 1.4f);
            _indicatorRenderer.SetPropertyBlock(_block);
        }
    }
}
