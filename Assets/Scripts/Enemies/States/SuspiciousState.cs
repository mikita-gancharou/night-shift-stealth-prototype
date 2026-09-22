using UnityEngine;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// "Wait... what was that?" - the guard freezes and stares at whatever triggered the meter.
    /// This is the visible middle step of the progressive detection system: the player still has time to
    /// break line of sight before the guard commits.
    /// </summary>
    public class SuspiciousState : GuardState
    {
        public SuspiciousState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Suspicious;

        public override void Enter()
        {
            Guard.StopMoving();
            Guard.Voice.SaySuspicious();
        }

        public override void Tick(float deltaTime)
        {
            if (TryStartChase()) return;

            Vector3 focus = Perception.HasStimulus ? Perception.StimulusPosition : Perception.LastKnownPosition;
            if (Perception.HasLastKnownPosition || Perception.HasStimulus) Guard.FaceTowards(focus, deltaTime);

            if (Perception.HasStimulus || Perception.Level >= AwarenessLevel.Alerted)
            {
                Guard.ChangeState(GuardStateId.Investigate);
                return;
            }

            if (Perception.Level == AwarenessLevel.Unaware || TimeInState >= Profile.SuspiciousDuration)
            {
                Guard.ChangeState(GuardStateId.Return);
            }
        }
    }
}
