using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>Walks back to the nearest point of the patrol route and hands control back to <see cref="PatrolState"/>.</summary>
    public class ReturnState : GuardState
    {
        public ReturnState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Return;

        public override void Enter()
        {
            Guard.Voice.SayReturn();
            Guard.SetSpeed(Profile.PatrolSpeed);

            if (Guard.HasRoute)
            {
                Guard.PatrolIndex = Guard.Route.ClosestIndex(Guard.transform.position);
                Guard.MoveTo(Guard.Route.PositionOf(Guard.PatrolIndex));
            }
            else
            {
                Guard.MoveTo(Guard.HomePosition);
            }
        }

        public override void Tick(float deltaTime)
        {
            if (TryStartChase()) return;

            if (Perception.HasStimulus)
            {
                Guard.ChangeState(GuardStateId.Investigate);
                return;
            }

            if (Perception.Level >= AwarenessLevel.Suspicious)
            {
                Guard.ChangeState(GuardStateId.Suspicious);
                return;
            }

            if (Guard.ReachedDestination(0.7f)) Guard.ChangeState(GuardStateId.Patrol);
        }
    }
}
