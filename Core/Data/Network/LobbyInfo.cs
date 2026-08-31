using System.Collections.Generic;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Container class for lobby information.
    /// </summary>
    public static class LobbyInfo
    {
        public static LobbySettings HostLobbySettings { get; set; }

        public static Dictionary<string, bool> HostJSONFileTriggers { get; set; } = new Dictionary<string, bool>();

        #region Arcade

        public static OnlineLevelSort OnlineLevelSort { get; set; }

        public static bool OnlineLevelAscend { get; set; }

        public static OnlineLevelCollectionSort OnlineLevelCollectionSort { get; set; }

        public static bool OnlineLevelCollectionAscend { get; set; }

        public static LevelSort LocalLevelSort { get; set; }

        public static bool LocalLevelAscend { get; set; }

        public static QuerySort SteamWorkshopSort { get; set; }

        public static LevelSort SteamLevelSort { get; set; }

        public static bool SteamLevelAscend { get; set; }

        #endregion

        #region Editor

        public static List<LevelPanel> HostEditorLevels { get; set; } = new List<LevelPanel>();

        public static bool EditorLevelAscend { get; set; }

        public static LevelSort EditorLevelSort { get; set; }

        #endregion
    }
}
