using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Container class for lobby information.
    /// </summary>
    public static class LobbyInfo
    {
        public static OnlineLevelSort OnlineLevelSort { get; set; }

        public static bool OnlineLevelAscend { get; set; }

        public static OnlineLevelCollectionSort OnlineLevelCollectionSort { get; set; }

        public static bool OnlineLevelCollectionAscend { get; set; }

        public static LevelSort LocalLevelSort { get; set; }

        public static bool LocalLevelAscend { get; set; }

        public static QuerySort SteamWorkshopSort { get; set; }

        public static LevelSort SteamLevelSort { get; set; }

        public static bool SteamLevelAscend { get; set; }

        public static Dictionary<string, bool> HostJSONFileTriggers { get; set; } = new Dictionary<string, bool>();
    }
}
