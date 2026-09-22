using UnityEngine;

namespace Stealth.Enemies
{
    /// <summary>
    /// Tuning data for one kind of observer, stored as an asset so the level can mix several archetypes
    /// (slow patrol, long sighted sentry, wall camera) without duplicating a single line of code.
    /// </summary>
    [CreateAssetMenu(fileName = "GuardProfile", menuName = "Stealth/Guard Profile")]
    public class GuardProfile : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName = "Patrol Guard";
        public Color BodyColor = new Color(0.55f, 0.6f, 0.68f);

        [Header("Movement (m/s)")]
        [Min(0.2f)] public float PatrolSpeed = 1.9f;
        [Min(0.2f)] public float InvestigateSpeed = 3.1f;
        [Min(0.2f)] public float ChaseSpeed = 4.7f;
        [Min(30f)] public float AngularSpeed = 420f;
        [Min(1f)] public float Acceleration = 14f;

        [Header("Sight")]
        [Min(1f)] public float ViewDistance = 14f;
        [Range(10f, 360f)] public float ViewAngle = 90f;

        [Header("Close range (SphereCollider zone)")]
        [Min(0.5f)] public float ProximityRadius = 4.5f;

        [Header("Hearing")]
        [Min(1f)] public float HearingRadius = 18f;
        [Tooltip("How much of a noise's intensity is added to the detection meter.")]
        [Range(0f, 1f)] public float NoiseSensitivity = 0.3f;

        [Header("Behaviour")]
        [Tooltip("Seconds the guard stands still looking at something suspicious before deciding.")]
        [Min(0.5f)] public float SuspiciousDuration = 3f;
        [Min(0.5f)] public float InvestigateLookTime = 2.6f;
        [Min(0.5f)] public float SearchDuration = 7f;
        [Tooltip("Seconds without line of sight before a chase turns into a search.")]
        [Min(0.2f)] public float LoseSightTime = 2.5f;
        [Min(0.3f)] public float CatchDistance = 1.4f;

        [Header("Alarm")]
        [Tooltip("Guards inside this radius are told to come over when the player is detected.")]
        [Min(0f)] public float AlertRadius = 24f;

        [Tooltip("Cameras cannot leave their mount: they only raise the alarm.")]
        public bool CanChase = true;

        [Header("Sentry")]
        [Tooltip("Guards without a patrol route sweep their head left and right by this angle.")]
        [Range(0f, 180f)] public float ScanAngle = 70f;
        [Min(1f)] public float ScanSpeed = 26f;
    }
}
