using UnityEngine;
using UnityEngine.AI;
using Stealth.Audio;
using Stealth.Perception;

namespace Stealth.Enemies.States
{
    /// <summary>
    /// Lost the player: sweep a few spots around the last known position before giving up.
    /// Gives the player a tense window to slip away instead of an instant reset.
    /// </summary>
    public class SearchState : GuardState
    {
        private const float SearchRadius = 6f;

        private float _timer;
        private bool _looking;
        private float _lookTimer;

        public SearchState(GuardController guard) : base(guard)
        {
        }

        public override GuardStateId Id => GuardStateId.Search;

        public override void Enter()
        {
            _timer = Profile.SearchDuration;
            _looking = false;

            Guard.SetSpeed(Profile.InvestigateSpeed);
            Guard.MoveTo(Perception.LastKnownPosition);
            Guard.Voice.SaySearch();
        }

        public override void Tick(float deltaTime)
        {
            if (TryStartChase()) return;

            if (Perception.HasStimulus)
            {
                Guard.ChangeState(GuardStateId.Investigate);
                return;
            }

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                Guard.Voice.SayGiveUp();
                AudioManager.PlaySfx(SoundId.GuardLost, Guard.transform.position);
                Guard.ChangeState(GuardStateId.Return);
                return;
            }

            if (_looking)
            {
                ScanTick(deltaTime);
                _lookTimer -= deltaTime;
                if (_lookTimer > 0f) return;

                _looking = false;
                Guard.SetSpeed(Profile.InvestigateSpeed);
                Guard.MoveTo(PickSearchPoint());
                return;
            }

            if (!Guard.ReachedDestination(0.9f)) return;

            _looking = true;
            _lookTimer = 1.4f;
            Guard.StopMoving();
            BeginScan(Guard.transform.eulerAngles.y);
        }

        /// <summary>A random reachable spot around the last known position.</summary>
        private Vector3 PickSearchPoint()
        {
            Vector3 center = Perception.LastKnownPosition;

            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * SearchRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) return hit.position;
            }

            return center;
        }
    }
}
