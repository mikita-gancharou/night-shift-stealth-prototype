using UnityEngine;
using Stealth.Enemies.States;
using Stealth.Perception;
using Stealth.Visuals;

namespace Stealth.Enemies
{
    /// <summary>
    /// Everything the player reads off a guard at a glance: the colour of its view cone, the icon above its
    /// head and the tint of its lamp. Colour coding is shared by guards and cameras so the language stays consistent.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuardVisuals : MonoBehaviour
    {
        [SerializeField] private VisionCone _cone;
        [SerializeField] private AlertIcon _icon;
        [SerializeField] private Light _lamp;
        [SerializeField] private Renderer[] _accentRenderers;

        [Header("Colours")]
        [SerializeField] private Color _calm = new Color(0.45f, 0.85f, 1f);
        [SerializeField] private Color _suspicious = new Color(1f, 0.83f, 0.25f);
        [SerializeField] private Color _alerted = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color _detected = new Color(1f, 0.18f, 0.18f);

        private MaterialPropertyBlock _block;

        public Color ColorFor(AwarenessLevel level)
        {
            switch (level)
            {
                case AwarenessLevel.Suspicious: return _suspicious;
                case AwarenessLevel.Alerted: return _alerted;
                case AwarenessLevel.Detected: return _detected;
                default: return _calm;
            }
        }

        public void Apply(AwarenessLevel level, float detection01, GuardStateId state)
        {
            Color color = ColorFor(level);

            if (_cone != null) _cone.SetColor(color);
            if (_lamp != null) _lamp.color = Color.Lerp(_lamp.color, color, 0.25f);
            if (_icon != null) _icon.Apply(level, detection01, color);

            if (_accentRenderers == null || _accentRenderers.Length == 0) return;

            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < _accentRenderers.Length; i++)
            {
                Renderer renderer = _accentRenderers[i];
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor("_Color", color);
                _block.SetColor("_EmissionColor", color * (level == AwarenessLevel.Unaware ? 0.6f : 1.6f));
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
