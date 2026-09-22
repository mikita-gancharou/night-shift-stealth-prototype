using System;
using UnityEngine;
using UnityEngine.AI;
using Stealth.Core;
using Stealth.Enemies.States;
using Stealth.Perception;

namespace Stealth.Enemies
{
    /// <summary>
    /// The guard itself: it owns the navigation agent, the senses and the state machine, and exposes small
    /// helpers ("move there", "look at that") that the states use. All decisions live in the state classes.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class GuardController : MonoBehaviour, IPerceiver, IAlertReceiver
    {
        [Header("Configuration")]
        [SerializeField] private GuardProfile _profile;

        [Header("Parts")]
        [SerializeField] private GuardPerception _perception;
        [SerializeField] private PatrolRoute _route;
        [SerializeField] private GuardVisuals _visuals;
        [SerializeField] private GuardVoice _voice;
        [SerializeField] private Transform _eye;

        private readonly GuardStateMachine _machine = new GuardStateMachine();
        private NavMeshAgent _agent;
        private bool _caught;

        /// <summary>Index of the patrol stop the guard is heading to. Shared by the patrol and return states.</summary>
        [NonSerialized] public int PatrolIndex;

        /// <summary>+1 or -1, only meaningful for ping-pong routes.</summary>
        [NonSerialized] public int PatrolDirection = 1;

        public GuardProfile Profile => _profile;
        public GuardPerception Perception => _perception;
        public PatrolRoute Route => _route;
        public GuardVoice Voice => _voice;
        public GuardVisuals Visuals => _visuals;
        public NavMeshAgent Agent => _agent;

        public bool HasRoute => _route != null && _route.Count > 0;
        public Vector3 HomePosition { get; private set; }
        public float HomeYaw { get; private set; }

        public GuardStateId CurrentState => _machine.CurrentId;
        public float TimeInCurrentState => _machine.TimeInCurrentState;
        public IStealthTarget Target => StealthTargetLocator.Current;

        /// <summary>Raised as (previous, current) whenever the guard changes behaviour.</summary>
        public event Action<GuardStateId, GuardStateId> StateChanged;

        // ----- unity ------------------------------------------------------------------------------

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_voice == null) _voice = gameObject.AddComponent<GuardVoice>();

            ApplyProfile();
            BuildStateMachine();
        }

        private void OnEnable()
        {
            PerceiverRegistry.Register(this);
            AlertNetwork.Register(this);
        }

        private void OnDisable()
        {
            PerceiverRegistry.Unregister(this);
            AlertNetwork.Unregister(this);
        }

        private void Start()
        {
            HomePosition = transform.position;
            HomeYaw = transform.eulerAngles.y;

            if (HasRoute) PatrolIndex = _route.ClosestIndex(transform.position);
            _machine.Change(GuardStateId.Patrol);
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game != null && !game.IsPlaying)
            {
                StopMoving();
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            _perception.Tick(deltaTime);
            _machine.Tick(deltaTime);

            if (_visuals != null) _visuals.Apply(_perception.Level, _perception.Value01, _machine.CurrentId);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyProfile();
        }

        // ----- setup ------------------------------------------------------------------------------

        /// <summary>Pushes the profile numbers into the agent and the sensors.</summary>
        public void ApplyProfile()
        {
            if (_profile == null) return;

            NavMeshAgent agent = _agent != null ? _agent : GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = _profile.PatrolSpeed;
                agent.angularSpeed = _profile.AngularSpeed;
                agent.acceleration = _profile.Acceleration;
                agent.stoppingDistance = 0.2f;
            }

            if (_perception != null)
            {
                if (_perception.Vision != null) _perception.Vision.ApplyShape(_profile.ViewDistance, _profile.ViewAngle);
                if (_perception.Proximity != null) _perception.Proximity.ApplyRadius(_profile.ProximityRadius);
                _perception.ApplySettings(_profile.HearingRadius, _profile.NoiseSensitivity);
            }
        }

        private void BuildStateMachine()
        {
            _machine.Add(new PatrolState(this));
            _machine.Add(new SuspiciousState(this));
            _machine.Add(new InvestigateState(this));
            _machine.Add(new ChaseState(this));
            _machine.Add(new SearchState(this));
            _machine.Add(new ReturnState(this));

            _machine.StateChanged += (previous, current) => StateChanged?.Invoke(previous, current);
        }

        // ----- helpers used by the states ---------------------------------------------------------

        public void ChangeState(GuardStateId id) => _machine.Change(id);

        public void SetSpeed(float speed)
        {
            if (_agent != null) _agent.speed = speed;
        }

        public void MoveTo(Vector3 destination)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            _agent.isStopped = false;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
            else
            {
                _agent.SetDestination(destination);
            }
        }

        public void StopMoving()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        public bool ReachedDestination(float tolerance = 0.45f)
        {
            if (_agent == null || !_agent.isOnNavMesh) return true;
            if (_agent.pathPending) return false;

            return _agent.remainingDistance <= Mathf.Max(tolerance, _agent.stoppingDistance + 0.05f);
        }

        public void FaceTowards(Vector3 worldPoint, float deltaTime)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
                _profile.AngularSpeed * deltaTime);
        }

        public void FaceYaw(float yaw, float deltaTime)
        {
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
                _profile.AngularSpeed * deltaTime);
        }

        /// <summary>The chase reached the player: report the failure once.</summary>
        public void CatchTarget()
        {
            if (_caught) return;

            _caught = true;
            StopMoving();

            if (GameManager.Instance != null) GameManager.Instance.ReportPlayerCaught();
        }

        // ----- interfaces -------------------------------------------------------------------------

        public Vector3 EyePosition => _eye != null ? _eye.position : transform.position + Vector3.up * 1.7f;

        public AwarenessLevel Level => _perception != null ? _perception.Level : AwarenessLevel.Unaware;

        public float Detection01 => _perception != null ? _perception.Value01 : 0f;

        public bool IsHunting => _machine.CurrentId == GuardStateId.Chase;

        public Vector3 Position => transform.position;

        public void OnAlertCall(Vector3 position)
        {
            if (IsHunting || _perception == null) return;

            _perception.ReceiveAlert(position);
            if (_machine.CurrentId != GuardStateId.Investigate) ChangeState(GuardStateId.Investigate);
        }
    }
}
