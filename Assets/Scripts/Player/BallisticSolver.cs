using UnityEngine;

namespace Stealth.Player
{
    /// <summary>Classic projectile maths: which launch velocity drops a stone onto a chosen spot.</summary>
    public static class BallisticSolver
    {
        /// <summary>
        /// Solves the flat (fast) arc for a fixed launch speed. Returns false when the target is out of range,
        /// in which case the caller should fall back to a simple 45 degree lob.
        /// </summary>
        public static bool TrySolve(Vector3 start, Vector3 target, float speed, out Vector3 velocity)
        {
            Vector3 delta = target - start;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float distance = flat.magnitude;
            float height = delta.y;
            float gravity = Mathf.Abs(Physics.gravity.y);

            if (distance < 0.05f || speed < 0.1f || gravity < 0.01f)
            {
                velocity = Vector3.up * speed;
                return false;
            }

            float speedSqr = speed * speed;
            float discriminant = speedSqr * speedSqr - gravity * (gravity * distance * distance + 2f * height * speedSqr);
            if (discriminant < 0f)
            {
                // Out of range: throw as far as possible, at 45 degrees.
                velocity = (flat.normalized + Vector3.up).normalized * speed;
                return false;
            }

            float angle = Mathf.Atan((speedSqr - Mathf.Sqrt(discriminant)) / (gravity * distance));
            velocity = flat.normalized * (speed * Mathf.Cos(angle)) + Vector3.up * (speed * Mathf.Sin(angle));
            return true;
        }

        /// <summary>Samples a ballistic path, stopping at the first thing it hits. Used for the aim preview.</summary>
        public static int SimulatePath(Vector3 start, Vector3 velocity, LayerMask mask, Vector3[] buffer,
            float step = 0.06f, float radius = 0.1f)
        {
            int count = 0;
            Vector3 position = start;
            Vector3 currentVelocity = velocity;

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[count++] = position;

                Vector3 next = position + currentVelocity * step;
                Vector3 segment = next - position;
                float length = segment.magnitude;

                if (length > 0.0001f && Physics.SphereCast(position, radius, segment / length, out RaycastHit hit,
                        length, mask, QueryTriggerInteraction.Ignore))
                {
                    buffer[count - 1] = hit.point;
                    break;
                }

                currentVelocity += Physics.gravity * step;
                position = next;
            }

            return count;
        }
    }
}
