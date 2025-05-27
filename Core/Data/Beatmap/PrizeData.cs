using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents unlockable assets that can be obtained under specific conditions.
    /// </summary>
    public class PrizeData : Exists
    {
        /// <summary>
        /// The current PrizeData that is being used by the game to unlock stuff.
        /// </summary>
        public static PrizeData Current { get; set; }

        /// <summary>
        /// List of prizes to be rewarded.
        /// </summary>
        public List<PrizeObject> prizes = new List<PrizeObject>();
    }
}
