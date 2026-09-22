using UnityEngine;
using Stealth.Audio;
using Stealth.Core;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// The guard has identified the player and runs them down. Reaching the player ends the mission,
    /// losing sight of them (or watching them disappear into a hiding spot) turns the chase into a search.
    /// </summary>
    public class ChaseState : GuardState
    {
        public ChaseState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Chase;

        public override void Enter()
        {
            Guard.SetSpeed(Profile.ChaseSpeed);
            Guard.Voice.SayAlert();
            AudioManager.PlaySfx(SoundId.GuardAlert, Guard.transform.position);

            if (GameManager.Instance != null) GameManager.Instance.ReportSpotted();

            // Call it in over the radio so the rest of the compound converges on the player.
            AlertNetwork.Broadcast(Perception.LastKnownPosition, Profile.AlertRadius, Guard);
        }

        public override void Tick(float deltaTime)
        {
            IStealthTarget target = Guard.Target;

            // Hiding spots break the chase instantly - that is the escape route the level is built around.
            if (target == null || target.IsHidden)
            {
                Guard.ChangeState(GuardStateId.Search);
                return;
            }

            if (Perception.HasVisual)
            {
                Perception.HoldDetected();
            }
            else if (Perception.TimeSinceVisual > Profile.LoseSightTime)
            {
                Guard.ChangeState(GuardStateId.Search);
                return;
            }

            Guard.SetSpeed(Profile.ChaseSpeed);
            Guard.MoveTo(Perception.LastKnownPosition);

            Vector3 toTarget = target.Transform.position - Guard.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= Profile.CatchDistance * Profile.CatchDistance) Guard.CatchTarget();
        }
    }
}
