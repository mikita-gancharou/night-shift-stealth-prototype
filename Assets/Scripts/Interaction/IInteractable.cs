using UnityEngine;
using Stealth.Player;

namespace Stealth.Interaction
{
    /// <summary>
    /// Anything the player can use with the interact key. Keeping this an interface means the interactor
    /// does not care whether it is a hiding spot, a stone pile, a document or a breaker switch.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Verb shown in the HUD prompt, e.g. "Hide" or "Take stones".</summary>
        string Prompt { get; }

        /// <summary>Where the on-screen marker should sit.</summary>
        Vector3 AnchorPosition { get; }

        bool CanInteract(PlayerController player);

        void Interact(PlayerController player);
    }
}
