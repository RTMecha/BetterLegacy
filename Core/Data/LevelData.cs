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
        public bool lockBoost = false;
        public float speedMultiplier = 1f;
        public int gameMode = 0;
        public float jumpGravity = 1f;
        public float jumpIntensity = 1f;
        public int maxJumpCount = 10;

        public static LevelData Parse(JSONNode jn)
        {
            var levelData = new LevelData();
            levelData.levelVersion = jn["level_version"];
            levelData.modVersion = jn["mod_version"];

            if (!string.IsNullOrEmpty(jn["lock_boost"]))
                levelData.lockBoost = jn["lock_boost"].AsBool;
            
            if (!string.IsNullOrEmpty(jn["speed_multiplier"]))
                levelData.speedMultiplier = jn["speed_multiplier"].AsFloat;
            
            if (!string.IsNullOrEmpty(jn["gamemode"]))
                levelData.gameMode = jn["gamemode"].AsInt;

            if (!string.IsNullOrEmpty(jn["jump_gravity"]))
                levelData.jumpGravity = jn["jump_gravity"].AsFloat;
            
            if (!string.IsNullOrEmpty(jn["jump_gravity"]))
                levelData.jumpIntensity = jn["jump_intensity"].AsFloat;

            if (!string.IsNullOrEmpty(jn["max_jump"]))
                levelData.maxJumpCount = jn["max_jump"].AsInt;

            return levelData;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["level_version"] = "4.1.16";
            jn["mod_version"] = modVersion;

            if (lockBoost)
                jn["lock_boost"] = lockBoost.ToString();

            if (speedMultiplier != 1f)
                jn["speed_multiplier"] = speedMultiplier.ToString();

            if (gameMode != 0)
                jn["gamemode"] = gameMode.ToString();

            if (jumpGravity != 1f)
                jn["jump_gravity"] = jumpGravity.ToString();

            if (jumpIntensity != 1f)
                jn["jump_intensity"] = jumpIntensity.ToString();

            if (showIntro)
                jn["show_intro"] = showIntro.ToString(); // this will be reversed since the default unmodded value is false

            if (maxJumpCount != 10)
                jn["max_jump"] = maxJumpCount.ToString();

            return jn;
        }
    }
}
