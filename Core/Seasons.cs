using System;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Library of seasonal events.
    /// </summary>
    public static class Seasons
    {
        /// <summary>
        /// jokes on you, I FIXED THE BUG
        /// </summary>
        public static bool AprilFools => DateTime.Now.Month == 4 && DateTime.Now.Day == 1;

        /// <summary>
        /// For the Project Arrhythmia (release) Anniversary.
        /// </summary>
        public static bool PAAnniversary => DateTime.Now.Month == 6 && DateTime.Now.Day == 15;
    }
}
