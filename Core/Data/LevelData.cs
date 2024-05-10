using SimpleJSON;
using BaseLevelData = DataManager.GameData.BeatmapData.LevelData;

namespace BetterLegacy.Core.Data
{
    public class LevelData : BaseLevelData
    {
        public LevelData()
        {
            modVersion = LegacyPlugin.ModVersion.ToString();
        }

        public string modVersion;

        public static LevelData Parse(JSONNode jn)
        {
            var levelData = new LevelData();
            levelData.levelVersion = jn["level_version"];
            levelData.modVersion = jn["mod_version"];
            return levelData;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["level_version"] = levelVersion;
            jn["mod_version"] = modVersion;
            return jn;
        }
    }
}
