using UnityEngine;
using Stealth.Audio;
using Stealth.Visuals;

namespace Stealth.Enemies
{
    /// <summary>
    /// The guard's reaction lines. Short barks above the head tell the player exactly what the AI is
    /// thinking, which is the cheapest readability win in a stealth game.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuardVoice : MonoBehaviour
    {
        [SerializeField] private SpeechBubble _bubble;

        [SerializeField] private string[] _suspiciousLines = { "Hm?", "What was that?", "Did something move?" };
        [SerializeField] private string[] _investigateLines = { "Better check that.", "Hello? Who's there?", "Over there..." };
        [SerializeField] private string[] _alertLines = { "Intruder!", "Hey! Stop!", "Got you!" };
        [SerializeField] private string[] _searchLines = { "Where did they go?", "Come out...", "I know you're here." };
        [SerializeField] private string[] _giveUpLines = { "Must have been nothing.", "Hm. Nothing here.", "Jumpy tonight." };
        [SerializeField] private string[] _returnLines = { "Back to it.", "All clear." };

        public void SaySuspicious()
        {
            Say(_suspiciousLines, 1.6f);
            AudioManager.PlaySfx(SoundId.GuardSuspicious, transform.position);
        }

        public void SayInvestigate() => Say(_investigateLines, 1.8f);

        public void SayAlert() => Say(_alertLines, 2f);

        public void SaySearch() => Say(_searchLines, 2f);

        public void SayGiveUp() => Say(_giveUpLines, 2f);

        public void SayReturn() => Say(_returnLines, 1.5f);

        private void Say(string[] lines, float duration)
        {
            if (_bubble == null || lines == null || lines.Length == 0) return;

            _bubble.Show(lines[Random.Range(0, lines.Length)], duration);
        }
    }
}
