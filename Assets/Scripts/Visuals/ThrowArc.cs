using UnityEngine;
using Stealth.Player;

namespace Stealth.Visuals
{
    /// <summary>Preview of the stone's flight path plus a marker where it will land.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowArc : MonoBehaviour
    {
        [SerializeField] private Transform _landingMarker;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField, Range(8, 96)] private int _resolution = 48;

        private LineRenderer _line;
        private Vector3[] _points;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _points = new Vector3[_resolution];
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_line != null && _line.enabled != visible) _line.enabled = visible;
            if (_landingMarker != null && _landingMarker.gameObject.activeSelf != visible)
            {
                _landingMarker.gameObject.SetActive(visible);
            }
        }

        public void Draw(Vector3 origin, Vector3 velocity, Vector3 aimPoint)
        {
            if (_line == null || !_line.enabled) return;

            int count = BallisticSolver.SimulatePath(origin, velocity, _collisionMask, _points);
            _line.positionCount = count;
            _line.SetPositions(_points);

            if (_landingMarker == null) return;

            Vector3 landing = count > 0 ? _points[count - 1] : aimPoint;
            _landingMarker.position = landing + Vector3.up * 0.03f;
        }
    }
}
