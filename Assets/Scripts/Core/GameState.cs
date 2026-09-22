namespace Stealth.Core
{
    /// <summary>High level state of a mission. Owned by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        Playing,
        Paused,
        Won,
        Lost
    }
}
