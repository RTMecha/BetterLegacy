using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class LevelData : Exists
    {
        public LevelData()
        {
            modVersion = LegacyPlugin.ModVersion.ToString();
        }

        public string levelVersion = "4.1.16";
        public string modVersion;
        public bool lockBoost = false;
        public float speedMultiplier = 1f;
        public int gameMode = 0;
        public float floatDrag = 2f;
        public float jumpGravity = 1f;
        public float jumpIntensity = 1f;
        public int maxJumpCount = 10;
        public int maxJumpBoostCount = 1;
        public int maxHealth = 3;

        public bool forceReplayLevelOff;

        public bool multiplyPlayerSpeed = true;
        public bool allowCustomPlayerModels = true;
        public bool spawnPlayers = true;

        public bool limitPlayer = true;
        public Vector2 limitMoveSpeed = new Vector2(20f, 20f);
        public Vector2 limitBoostSpeed = new Vector2(85f, 85f);
        public Vector2 limitBoostCooldown = new Vector2(0.1f, 0.1f);
        public Vector2 limitBoostMinTime = new Vector2(0.07f, 0.07f);
        public Vector2 limitBoostMaxTime = new Vector2(0.18f, 0.18f);
        public Vector2 limitHitCooldown = new Vector2(0.001f, 2.5f);

        /// <summary>
        /// If the song should reverse at all when all players are dead.
        /// </summary>
        public bool reverse = true;

        public int backgroundColor;

        public bool followPlayer;

        public bool showIntro;

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

            if (jn["lock_boost"] != null)
                levelData.lockBoost = jn["lock_boost"].AsBool;
            
            if (jn["speed_multiplier"] != null)
                levelData.speedMultiplier = jn["speed_multiplier"].AsFloat;
            
            if (jn["gamemode"] != null)
                levelData.gameMode = jn["gamemode"].AsInt;

            if (jn["jump_gravity"] != null)
                levelData.jumpGravity = jn["jump_gravity"].AsFloat;
            
            if (jn["jump_intensity"] != null)
                levelData.jumpIntensity = jn["jump_intensity"].AsFloat;

            if (jn["max_jump"] != null)
                levelData.maxJumpCount = jn["max_jump"].AsInt;

            if (jn["max_jump_boost"] != null)
                levelData.maxJumpBoostCount = jn["max_jump_boost"].AsInt;

            if (jn["max_health"] != null)
                levelData.maxHealth = jn["max_health"].AsInt;

            if (jn["force_replay_level_off"] != null)
                levelData.forceReplayLevelOff = jn["force_replay_level_off"].AsBool;

            if (jn["multiply_player_speed"] != null)
                levelData.multiplyPlayerSpeed = jn["multiply_player_speed"].AsBool;

            if (jn["allow_custom_player_models"] != null)
                levelData.allowCustomPlayerModels = jn["allow_custom_player_models"].AsBool;

            if (jn["spawn_players"] != null)
                levelData.spawnPlayers = jn["spawn_players"].AsBool;

            if (jn["limit_player"] != null)
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
                jn["show_intro"] = showIntro; // this will be reversed since the default unmodded value is false

            if (lockBoost)
                jn["lock_boost"] = lockBoost;

            if (speedMultiplier != 1f)
                jn["speed_multiplier"] = speedMultiplier;

            if (gameMode != 0)
                jn["gamemode"] = gameMode;

            if (jumpGravity != 1f)
                jn["jump_gravity"] = jumpGravity;

            if (jumpIntensity != 1f)
                jn["jump_intensity"] = jumpIntensity;

            if (maxJumpCount != 10)
                jn["max_jump"] = maxJumpCount;
            if (maxJumpBoostCount != 1)
                jn["max_jump_boost"] = maxJumpBoostCount;

            if (maxHealth != 3)
                jn["max_health"] = maxHealth;

            if (forceReplayLevelOff)
                jn["force_replay_level_off"] = forceReplayLevelOff;

            if (!multiplyPlayerSpeed)
                jn["multiply_player_speed"] = multiplyPlayerSpeed;

            if (!allowCustomPlayerModels)
                jn["allow_custom_player_models"] = allowCustomPlayerModels;
            
            if (!spawnPlayers)
                jn["spawn_players"] = spawnPlayers;

            jn["limit_player"] = limitPlayer;

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
