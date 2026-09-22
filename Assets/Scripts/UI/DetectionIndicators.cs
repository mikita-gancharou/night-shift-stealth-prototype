using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Stealth.Perception;
using Stealth.Player;

namespace Stealth.UI
{
    /// <summary>
    /// Arrows around the screen centre that point at whoever is currently detecting the player, filling up
    /// with their detection meter. It tells the player which direction to break line of sight towards.
    /// </summary>
    public class DetectionIndicators : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private Image _arrowTemplate;
        [SerializeField, Min(1)] private int _maxArrows = 6;
        [SerializeField, Min(20f)] private float _radius = 150f;

        [SerializeField] private Color _suspicious = new Color(1f, 0.83f, 0.25f);
        [SerializeField] private Color _alerted = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color _detected = new Color(1f, 0.25f, 0.25f);

        private readonly List<Image> _arrows = new List<Image>();
        private Camera _camera;

        private void Awake()
        {
            if (_arrowTemplate != null) _arrowTemplate.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_root == null || _arrowTemplate == null) return;

            if (_camera == null) _camera = Camera.main;
            PlayerController player = PlayerController.Instance;
            if (_camera == null || player == null)
            {
                HideFrom(0);
                return;
            }

            IReadOnlyList<IPerceiver> perceivers = PerceiverRegistry.All;
            int used = 0;

            for (int i = 0; i < perceivers.Count && used < _maxArrows; i++)
            {
                IPerceiver perceiver = perceivers[i];
                if (perceiver == null || perceiver.Detection01 <= 0.03f) continue;

                Image arrow = GetArrow(used++);
                PlaceArrow(arrow, player.transform.position, perceiver);
            }

            HideFrom(used);
        }

        private void PlaceArrow(Image arrow, Vector3 playerPosition, IPerceiver perceiver)
        {
            Vector3 toPerceiver = perceiver.EyePosition - playerPosition;
            toPerceiver.y = 0f;

            // Direction relative to where the camera looks, so "up" on screen is "in front of the player".
            Vector3 cameraForward = _camera.transform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.001f || toPerceiver.sqrMagnitude < 0.001f) return;

            float angle = Vector3.SignedAngle(cameraForward.normalized, toPerceiver.normalized, Vector3.up);
            float radians = angle * Mathf.Deg2Rad;

            RectTransform rect = arrow.rectTransform;
            rect.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * _radius;
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle);

            Color color = perceiver.IsHunting ? _detected
                : perceiver.Level == AwarenessLevel.Alerted ? _alerted
                : _suspicious;

            color.a = Mathf.Lerp(0.35f, 1f, perceiver.Detection01);
            arrow.color = color;
            arrow.fillAmount = Mathf.Clamp01(perceiver.Detection01);

            if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);
        }

        private Image GetArrow(int index)
        {
            while (_arrows.Count <= index)
            {
                Image instance = Instantiate(_arrowTemplate, _root);
                instance.gameObject.SetActive(false);
                _arrows.Add(instance);
            }

            return _arrows[index];
        }

        private void HideFrom(int index)
        {
            for (int i = index; i < _arrows.Count; i++)
            {
                if (_arrows[i].gameObject.activeSelf) _arrows[i].gameObject.SetActive(false);
            }
        }
    }
}
