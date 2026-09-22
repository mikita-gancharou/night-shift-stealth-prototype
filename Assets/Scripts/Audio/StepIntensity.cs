namespace Stealth.Audio
{
    /// <summary>
    /// How hard the player is putting their feet down. It picks both the clip set and the level:
    /// crouching is a muffled thump, sprinting is the loudest thing the player can do on purpose.
    /// </summary>
    public enum StepIntensity
    {
        Crouch,
        Walk,
        Sprint
    }
}
