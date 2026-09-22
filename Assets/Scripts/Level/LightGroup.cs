using System;
using UnityEngine;

namespace Stealth.Level
{
    /// <summary>
    /// A set of lamps controlled together by a breaker switch, with the lit zone that belongs to them.
    /// Turning the group off is a stealth tool: the zone stops adding visibility to the player.
    /// </summary>
    public class LightGroup : MonoBehaviour
    {
        [SerializeField] private Light[] _lights;
        [SerializeField] private Renderer[] _lampRenderers;
        [SerializeField] private Color _emissiveOn = new Color(1f, 0.92f, 0.7f);
        [SerializeField] private bool _startOn = true;

        public bool IsOn { get; private set; } = true;

        public event Action<bool> Changed;

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            IsOn = _startOn;
            Apply();
        }

        public void Toggle() => SetOn(!IsOn);

        public void SetOn(bool on)
        {
            if (IsOn == on) return;

            IsOn = on;
            Apply();
            Changed?.Invoke(IsOn);
        }

        private void Apply()
        {
            if (_lights != null)
            {
                for (int i = 0; i < _lights.Length; i++)
                {
                    if (_lights[i] != null) _lights[i].enabled = IsOn;
                }
            }

            if (_lampRenderers == null) return;

            _block ??= new MaterialPropertyBlock();
            Color emission = IsOn ? _emissiveOn : Color.black;

            for (int i = 0; i < _lampRenderers.Length; i++)
            {
                Renderer renderer = _lampRenderers[i];
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor("_EmissionColor", emission);
                _block.SetColor("_Color", IsOn ? emission : new Color(0.2f, 0.2f, 0.22f));
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
