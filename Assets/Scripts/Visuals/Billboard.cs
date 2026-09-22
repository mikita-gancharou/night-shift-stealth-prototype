using UnityEngine;

namespace Stealth.Visuals
{
    /// <summary>Keeps world space UI (alert icons, speech bubbles) turned towards the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private bool _keepUpright = true;

        private Camera _camera;

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            Vector3 forward = transform.position - _camera.transform.position;
            if (_keepUpright) forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
