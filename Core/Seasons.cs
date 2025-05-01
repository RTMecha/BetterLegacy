using System;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Library of seasonal events.
    /// </summary>
    public static class Seasons
    {
        /// <summary>
        /// Gets the current season.
        /// </summary>
        public static Season Current
        {
            get
            {
                var now = DateTime.Now;
                var month = now.Month;
                var day = now.Day;

                if (month == 4 && day == 1)
                    return Season.AprilFools;
                if (month == 6 && day == 15)
                    return Season.PAAnniversary;
                if (month == 11 && day == 4)
                    return Season.PAOriginAnniversary;
                if (month == 9 && day == 11)
                    return Season.ExampleBirthday;

                return Season.None;
            }
        }

        /// <summary>
        /// jokes on you, I FIXED THE BUG
        /// </summary>
        public static bool IsAprilFools => Current == Season.AprilFools;

        /// <summary>
        /// For the Project Arrhythmia (release) Anniversary.
        /// </summary>
        public static bool IsPAAnniversary => Current == Season.PAAnniversary;

        /// <summary>
        /// For the Project Arrhythmia (concept / origin) anniversary.
        /// </summary>
        public static bool IsPAOriginAnniversary => Current == Season.PAOriginAnniversary;

        /// <summary>
        /// For Example's birthday. Yes, that is literally the day his code was created.
        /// </summary>
        public static bool IsExampleBirthday => Current == Season.ExampleBirthday;

        /// <summary>
        /// Years since the date time.
        /// </summary>
        /// <returns>Returns the number of years since the date.</returns>
        public static int YearsSince(this DateTime instance) => RTMath.Distance(instance.Year, DateTime.Now.Year);

        /// <summary>
        /// Years since the date time.
        /// </summary>
        /// <param name="compare">Date time to compare to.</param>
        /// <returns>Returns the number of years since the date.</returns>
        public static int YearsSince(this DateTime instance, DateTime compare) => RTMath.Distance(instance.Year, compare.Year);

        /// <summary>
        /// Converts a season to the associated date time.
        /// </summary>
        /// <returns>Returns the date time associated with the season.</returns>
        public static DateTime ToDateTime(this Season season) => season switch
        {
            Season.AprilFools =>              new DateTime(0000, 04, 01),
            Season.PAAnniversary =>           new DateTime(2019, 06, 15),
            Season.PAOriginAnniversary =>     new DateTime(2013, 11, 04),
            Season.ExampleBirthday =>         new DateTime(2023, 09, 11),
            _ => DateTime.Now,
        };
    }

    /// <summary>
    /// Represents a seasonal event.
    /// </summary>
    public enum Season
    {
        None,
        AprilFools,
        PAAnniversary,
        PAOriginAnniversary,
        ExampleBirthday,
    }
}
