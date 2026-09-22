namespace Stealth.Perception
{
    /// <summary>
    /// Steps of the progressive detection system. A guard climbs through these levels instead of
    /// flipping straight from "nothing" to "chase".
    /// </summary>
    public enum AwarenessLevel
    {
        /// <summary>Nothing noticed, the guard keeps doing its routine.</summary>
        Unaware = 0,

        /// <summary>Something registered: the guard stops and looks, but does not leave its post yet.</summary>
        Suspicious = 1,

        /// <summary>Convinced something is there: the guard walks over to check it out.</summary>
        Alerted = 2,

        /// <summary>The player has been identified: chase.</summary>
        Detected = 3
    }
}
