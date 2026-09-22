using UnityEngine;
using Stealth.Perception;

namespace Stealth.Visuals
{
    /// <summary>
    /// Draws the guard's field of view on the ground as a procedural fan. Each edge of the fan comes from a
    /// raycast, so the shape wraps around walls exactly like the sensor does - the player can read the
    /// danger zone instead of guessing it.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        [SerializeField] private VisionSensor _sensor;

        [Tooltip("Height the probing rays are cast at: chest level, so low crates do not cut the cone.")]
        [SerializeField, Min(0.05f)] private float _probeHeight = 1.5f;

        [Tooltip("Height the mesh is drawn at, just above the floor.")]
        [SerializeField, Min(0.01f)] private float _drawHeight = 0.06f;

        [SerializeField, Range(8, 120)] private int _rayCount = 56;
        [SerializeField, Min(0.02f)] private float _refreshInterval = 0.05f;
        [SerializeField] private Color _color = new Color(0.45f, 0.85f, 1f, 0.22f);

        [Tooltip("Alpha multiplier at the far edge of the cone, for a soft fade out.")]
        [SerializeField, Range(0f, 1f)] private float _edgeFade = 0.15f;

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float _timer;

        private Vector3[] _vertices;
        private Color[] _colors;
        private int[] _triangles;
        private int _cachedRayCount = -1;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();

            _mesh = new Mesh { name = "VisionCone" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().mesh = _mesh;

            ApplyColor();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        private void LateUpdate()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _timer = _refreshInterval;
            Rebuild();
        }

        public void SetColor(Color color)
        {
            if (_color == color) return;

            _color = color;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (_renderer == null) return;

            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor("_Color", _color);
            _renderer.SetPropertyBlock(_block);
        }

        private void Rebuild()
        {
            if (_sensor == null || _mesh == null) return;

            EnsureBuffers();

            Transform eye = _sensor.Eye;
            Vector3 origin = new Vector3(eye.position.x, eye.position.y, eye.position.z);
            Vector3 probeOrigin = new Vector3(origin.x, transform.position.y + _probeHeight, origin.z);

            float distance = _sensor.ViewDistance;
            float half = _sensor.ViewAngle * 0.5f;
            float step = _sensor.ViewAngle / (_rayCount - 1);
            float baseYaw = eye.eulerAngles.y;

            // Local space of this object, which sits at the guard's feet.
            _vertices[0] = Vector3.up * _drawHeight;
            _colors[0] = Color.white;

            for (int i = 0; i < _rayCount; i++)
            {
                float yaw = baseYaw - half + step * i;
                Vector3 direction = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));

                float hitDistance = distance;
                if (Physics.Raycast(probeOrigin, direction, out RaycastHit hit, distance,
                        _sensor.ObstacleMask, QueryTriggerInteraction.Ignore))
                {
                    hitDistance = hit.distance;
                }

                Vector3 worldPoint = probeOrigin + direction * hitDistance;
                Vector3 local = transform.InverseTransformPoint(new Vector3(worldPoint.x, transform.position.y, worldPoint.z));
                _vertices[i + 1] = new Vector3(local.x, _drawHeight, local.z);
                _colors[i + 1] = new Color(1f, 1f, 1f, _edgeFade);
            }

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }

        private void EnsureBuffers()
        {
            if (_cachedRayCount == _rayCount && _vertices != null) return;

            _cachedRayCount = _rayCount;
            _vertices = new Vector3[_rayCount + 1];
            _colors = new Color[_rayCount + 1];
            _triangles = new int[(_rayCount - 1) * 3];

            for (int i = 0; i < _rayCount - 1; i++)
            {
                _triangles[i * 3] = 0;
                _triangles[i * 3 + 1] = i + 1;
                _triangles[i * 3 + 2] = i + 2;
            }
        }
    }
}
