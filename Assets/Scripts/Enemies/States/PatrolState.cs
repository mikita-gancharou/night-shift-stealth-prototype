using UnityEngine;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// Default behaviour: walk the route, pause at each stop, and sweep the view when there is no route
    /// (that is what turns a guard into a static sentry).
    /// </summary>
    public class PatrolState : GuardState
    {
        private bool _waiting;
        private float _waitTimer;

        public PatrolState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Patrol;

        public override void Enter()
        {
            Guard.SetSpeed(Profile.PatrolSpeed);
            _waiting = false;

            if (Guard.HasRoute)
            {
                Guard.MoveTo(Guard.Route.PositionOf(Guard.PatrolIndex));
            }
            else
            {
                Guard.StopMoving();
                BeginScan(Guard.HomeYaw);
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

            if (!Guard.HasRoute)
            {
                ScanTick(deltaTime);
                return;
            }

            if (_waiting)
            {
                PatrolRoute.Stop stop = Guard.Route.GetStop(Guard.PatrolIndex);
                if (stop != null && stop.LookAround) ScanTick(deltaTime);

                _waitTimer -= deltaTime;
                if (_waitTimer > 0f) return;

                _waiting = false;
                Guard.PatrolIndex = Guard.Route.Advance(Guard.PatrolIndex, ref Guard.PatrolDirection);
                Guard.SetSpeed(Profile.PatrolSpeed);
                Guard.MoveTo(Guard.Route.PositionOf(Guard.PatrolIndex));
                return;
            }

            if (!Guard.ReachedDestination()) return;

            PatrolRoute.Stop current = Guard.Route.GetStop(Guard.PatrolIndex);
            _waitTimer = current != null ? current.WaitSeconds : 1f;
            _waiting = true;
            Guard.StopMoving();
            BeginScan(Guard.transform.eulerAngles.y);
        }
    }
}
