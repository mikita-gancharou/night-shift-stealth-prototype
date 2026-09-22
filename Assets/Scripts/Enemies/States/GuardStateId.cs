namespace Stealth.Enemies.States
{
    /// <summary>The behaviours a guard can be in. One class per entry, all in this folder.</summary>
    public enum GuardStateId
    {
        /// <summary>Walking the route (or sweeping the view for a sentry).</summary>
        Patrol,

        /// <summary>Stopped and staring at something odd, deciding whether it is worth walking over.</summary>
        Suspicious,

        /// <summary>Walking to the noise or to the last place something was seen.</summary>
        Investigate,

        /// <summary>Running after the player.</summary>
        Chase,

        /// <summary>Sweeping the area around the last known position after losing the player.</summary>
        Search,

        /// <summary>Walking back to the patrol route.</summary>
        Return
    }
}
