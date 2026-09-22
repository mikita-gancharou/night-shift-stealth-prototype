using UnityEngine;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// The guard walks over to the noise (or to the last place something was seen) and looks around.
    /// This is where thrown stones pay off: a guard in this state has left its route.
    /// </summary>
    public class InvestigateState : GuardState
    {
        private Vector3 _destination;
        private bool _looking;
        private float _lookTimer;

        public InvestigateState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Investigate;

        public override void Enter()
        {
            _looking = false;
            _destination = Perception.HasStimulus ? Perception.StimulusPosition : Perception.LastKnownPosition;
            Perception.ConsumeStimulus();

            Guard.SetSpeed(Profile.InvestigateSpeed);
            Guard.MoveTo(_destination);
            Guard.Voice.SayInvestigate();
        }

        public override void Tick(float deltaTime)
        {
            if (TryStartChase()) return;

            // A fresh noise while walking: change course.
            if (Perception.HasStimulus)
            {
                _destination = Perception.StimulusPosition;
                Perception.ConsumeStimulus();
                _looking = false;
                Guard.SetSpeed(Profile.InvestigateSpeed);
                Guard.MoveTo(_destination);
                return;
            }

            if (!_looking)
            {
                if (!Guard.ReachedDestination(0.8f)) return;

                _looking = true;
                _lookTimer = Profile.InvestigateLookTime;
                Guard.StopMoving();
                BeginScan(Guard.transform.eulerAngles.y);
                return;
            }

            ScanTick(deltaTime);
            _lookTimer -= deltaTime;
            if (_lookTimer > 0f) return;

            // Still uneasy: sweep the area. Calm again: back to work.
            Guard.ChangeState(Perception.Level >= AwarenessLevel.Suspicious ? GuardStateId.Search : GuardStateId.Return);
        }
    }
}
