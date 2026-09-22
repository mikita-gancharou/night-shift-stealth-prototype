using UnityEngine;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// Base class of the guard state machine. Every state owns its own transitions, so adding a behaviour
    /// means adding a file instead of growing one giant update method.
    /// </summary>
    public abstract class GuardState
    {
        protected readonly GuardController Guard;

        private float _scanPhase;
        private float _scanCenterYaw;

        protected GuardState(GuardController guard)
        {
            Guard = guard;
        }

        public abstract GuardStateId Id { get; }

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }

        protected GuardProfile Profile => Guard.Profile;
        protected GuardPerception Perception => Guard.Perception;
        protected float TimeInState => Guard.TimeInCurrentState;

        /// <summary>Shared exit used by every non-chase state: full detection always wins.</summary>
        protected bool TryStartChase()
        {
            if (Perception.Level != AwarenessLevel.Detected) return false;

            Guard.ChangeState(Profile.CanChase ? GuardStateId.Chase : GuardStateId.Investigate);
            return true;
        }

        /// <summary>Starts a left / right head sweep centred on the given yaw.</summary>
        protected void BeginScan(float centerYaw)
        {
            _scanCenterYaw = centerYaw;
            _scanPhase = 0f;
        }

        /// <summary>Advances the sweep. Call every frame while the guard is standing still and looking around.</summary>
        protected void ScanTick(float deltaTime)
        {
            _scanPhase += deltaTime * Profile.ScanSpeed * Mathf.Deg2Rad;
            float offset = Mathf.Sin(_scanPhase) * Profile.ScanAngle * 0.5f;
            Guard.FaceYaw(_scanCenterYaw + offset, deltaTime);
        }
    }
}
