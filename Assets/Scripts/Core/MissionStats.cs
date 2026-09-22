using System;
using UnityEngine;

namespace Stealth.Core
{
    /// <summary>
    /// Numbers collected during a run. Shown on the end screen so the player can compare attempts.
    /// </summary>
    [Serializable]
    public class MissionStats
    {
        public float TimeSeconds;
        public int TimesSpotted;
        public int StonesThrown;
        public int IntelCollected;
        public int IntelTotal;

        /// <summary>Short, readable rank used as the headline of the victory screen.</summary>
        public string Rank
        {
            get
            {
                if (TimesSpotted == 0 && IntelTotal > 0 && IntelCollected >= IntelTotal) return "PERFECT GHOST";
                if (TimesSpotted == 0) return "GHOST";
                if (TimesSpotted <= 2) return "SHADOW";
                return "SLOPPY";
            }
        }

        public string TimeText
        {
            get
            {
                int total = Mathf.FloorToInt(Mathf.Max(0f, TimeSeconds));
                return string.Format("{0:00}:{1:00}", total / 60, total % 60);
            }
        }

        public void Reset()
        {
            TimeSeconds = 0f;
            TimesSpotted = 0;
            StonesThrown = 0;
            IntelCollected = 0;
            IntelTotal = 0;
        }
    }
}
