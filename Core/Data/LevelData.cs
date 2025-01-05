using SimpleJSON;
using UnityEngine;
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
        public int maxJumpBoostCount = 1;
        public int maxHealth = 3;
        public bool allowCustomPlayerModels = true;
        public bool spawnPlayers = true;

        public bool limitPlayer = true;
        public Vector2 limitMoveSpeed = new Vector2(20f, 20f);
        public Vector2 limitBoostSpeed = new Vector2(85f, 85f);
        public Vector2 limitBoostCooldown = new Vector2(0.1f, 0.1f);
        public Vector2 limitBoostMinTime = new Vector2(0.07f, 0.07f);
        public Vector2 limitBoostMaxTime = new Vector2(0.18f, 0.18f);
        public Vector2 limitHitCooldown = new Vector2(0.001f, 2.5f);

        public static LevelData Parse(JSONNode jn)
        {
            var levelData = new LevelData();

            if (jn["level_version"] != null)
                levelData.levelVersion = jn["level_version"];
            else
                levelData.levelVersion = ProjectArrhythmia.GameVersion.ToString();

            if (jn["mod_version"] != null)
                levelData.modVersion = jn["mod_version"];
            else
                levelData.modVersion = LegacyPlugin.ModVersion.ToString();

            if (jn["show_intro"] != null)
                levelData.showIntro = jn["show_intro"].AsBool;

            if (!string.IsNullOrEmpty(jn["lock_boost"]))
                levelData.lockBoost = jn["lock_boost"].AsBool;
            
            if (!string.IsNullOrEmpty(jn["speed_multiplier"]))
                levelData.speedMultiplier = jn["speed_multiplier"].AsFloat;
            
            if (!string.IsNullOrEmpty(jn["gamemode"]))
                levelData.gameMode = jn["gamemode"].AsInt;

            if (!string.IsNullOrEmpty(jn["jump_gravity"]))
                levelData.jumpGravity = jn["jump_gravity"].AsFloat;
            
            if (!string.IsNullOrEmpty(jn["jump_intensity"]))
                levelData.jumpIntensity = jn["jump_intensity"].AsFloat;

            if (!string.IsNullOrEmpty(jn["max_jump"]))
                levelData.maxJumpCount = jn["max_jump"].AsInt;

            if (!string.IsNullOrEmpty(jn["max_jump_boost"]))
                levelData.maxJumpBoostCount = jn["max_jump_boost"].AsInt;

            if (!string.IsNullOrEmpty(jn["max_health"]))
                levelData.maxHealth = jn["max_health"].AsInt;

            if (!string.IsNullOrEmpty(jn["allow_custom_player_models"]))
                levelData.allowCustomPlayerModels = jn["allow_custom_player_models"].AsBool;

            if (!string.IsNullOrEmpty(jn["spawn_players"]))
                levelData.spawnPlayers = jn["spawn_players"].AsBool;

            if (!string.IsNullOrEmpty(jn["limit_player"]))
                levelData.limitPlayer = jn["limit_player"].AsBool;
            else if (jn["mod_version"] != null)
                levelData.limitPlayer = false;
            
            if (jn["limit_move_speed"] != null)
                levelData.limitMoveSpeed = Parser.TryParse(jn["limit_move_speed"], new Vector2(20f, 20f));
            if (jn["limit_boost_speed"] != null)
                levelData.limitBoostSpeed = Parser.TryParse(jn["limit_boost_speed"], new Vector2(85f, 85f));
            if (jn["limit_boost_cooldown"] != null)
                levelData.limitBoostCooldown = Parser.TryParse(jn["limit_boost_cooldown"], new Vector2(0.1f, 0.1f));
            if (jn["limit_boost_min_time"] != null)
                levelData.limitBoostMinTime = Parser.TryParse(jn["limit_boost_min_time"], new Vector2(0.07f, 0.07f));
            if (jn["limit_boost_max_time"] != null)
                levelData.limitBoostMaxTime = Parser.TryParse(jn["limit_boost_max_time"], new Vector2(0.18f, 0.18f));
            if (jn["limit_hit_cooldown"] != null)
                levelData.limitHitCooldown = Parser.TryParse(jn["limit_hit_cooldown"], new Vector2(2.5f, 2.5f));

            return levelData;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["level_version"] = ProjectArrhythmia.GAME_VERSION;
            jn["mod_version"] = modVersion;

            if (showIntro)
                jn["show_intro"] = showIntro.ToString(); // this will be reversed since the default unmodded value is false

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

            if (maxJumpCount != 10)
                jn["max_jump"] = maxJumpCount.ToString();
            if (maxJumpBoostCount != 1)
                jn["max_jump_boost"] = maxJumpBoostCount.ToString();

            if (maxHealth != 3)
                jn["max_health"] = maxHealth.ToString();

            if (!allowCustomPlayerModels)
                jn["allow_custom_player_models"] = allowCustomPlayerModels.ToString();
            
            if (!spawnPlayers)
                jn["spawn_players"] = spawnPlayers.ToString();

            jn["limit_player"] = limitPlayer.ToString();

            if (limitMoveSpeed.x != 20f || limitMoveSpeed.y != 20f)
                jn["limit_move_speed"] = limitMoveSpeed.ToJSON();
            if (limitBoostSpeed.x != 85f || limitBoostSpeed.y != 85f)
                jn["limit_boost_speed"] = limitBoostSpeed.ToJSON();
            if (limitBoostCooldown.x != 0.1f || limitBoostCooldown.y != 0.1f)
                jn["limit_boost_cooldown"] = limitBoostCooldown.ToJSON();
            if (limitBoostMinTime.x != 0.07f || limitBoostMinTime.y != 0.07f)
                jn["limit_boost_min_time"] = limitBoostMinTime.ToJSON();
            if (limitBoostMaxTime.x != 0.18f || limitBoostMaxTime.y != 0.18f)
                jn["limit_boost_max_time"] = limitBoostMaxTime.ToJSON();
            if (limitHitCooldown.x != 2.5f || limitHitCooldown.y != 2.5f)
                jn["limit_hit_cooldown"] = limitHitCooldown.ToJSON();

            return jn;
        }
    }
}
