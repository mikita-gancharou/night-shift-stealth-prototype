using System;
using System.Collections.Generic;

namespace Stealth.Enemies.States
{
    /// <summary>Minimal state machine: owns the states, runs the current one and reports changes.</summary>
    public class GuardStateMachine
    {
        private readonly Dictionary<GuardStateId, GuardState> _states = new Dictionary<GuardStateId, GuardState>();

        public GuardState Current { get; private set; }
        public GuardStateId CurrentId { get; private set; } = GuardStateId.Patrol;
        public float TimeInCurrentState { get; private set; }

        /// <summary>Raised as (previous, current) after a transition.</summary>
        public event Action<GuardStateId, GuardStateId> StateChanged;

        public void Add(GuardState state)
        {
            if (state != null) _states[state.Id] = state;
        }

        public void Change(GuardStateId id)
        {
            if (!_states.TryGetValue(id, out GuardState next)) return;
            if (Current == next) return;

            GuardStateId previous = CurrentId;
            Current?.Exit();

            Current = next;
            CurrentId = id;
            TimeInCurrentState = 0f;
            Current.Enter();

            StateChanged?.Invoke(previous, id);
        }

        public void Tick(float deltaTime)
        {
            if (Current == null) return;

            TimeInCurrentState += deltaTime;
            Current.Tick(deltaTime);
        }
    }
}
