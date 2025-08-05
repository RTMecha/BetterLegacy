using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Controls level behavior.
    /// </summary>
    public class LevelData : PAObject<LevelData>
    {
        public LevelData() => modVersion = LegacyPlugin.ModVersion.ToString();

        #region Values

        /// <summary>
        /// The version of the game this level was made in.
        /// </summary>
        public string levelVersion = ProjectArrhythmia.GAME_VERSION;

        /// <summary>
        /// The version of the BetterLegacy mod this level was made in.
        /// </summary>
        public string modVersion;

        /// <summary>
        /// The time the level should start at.
        /// </summary>
        public float levelStartOffset;
        public float LevelStartOffset { get => Mathf.Clamp(levelStartOffset, 0.1f, float.MaxValue); set => levelStartOffset = Mathf.Clamp(value, 0.1f, float.MaxValue); }

        /// <summary>
        /// If the song should reverse at all when all players are dead.
        /// </summary>
        public bool reverse = true;

        /// <summary>
        /// If the intro sequence should display.
        /// </summary>
        public bool hideIntro;

        /// <summary>
        /// If <see cref="Configs.CoreConfig.ReplayLevel"/> setting should be ignored and not replay the level in the background.
        /// </summary>
        public bool forceReplayLevelOff;

        #region Player Conditions

        /// <summary>
        /// If players should respawn immediately when they die and not wait for other players to die.
        /// </summary>
        public bool respawnImmediately = false;

        /// <summary>
        /// If the boost should be locked.
        /// </summary>
        public bool lockBoost = false;

        /// <summary>
        /// Default speed multiplier.
        /// </summary>
        public float speedMultiplier = 1f;

        /// <summary>
        /// Default game mode.
        /// </summary>
        public int gameMode = 0;

        /// <summary>
        /// Gravity drag.
        /// </summary>
        public float floatDrag = 2f;

        /// <summary>
        /// Jump gravity.
        /// </summary>
        public float jumpGravity = 1f;

        /// <summary>
        /// Jump intensity.
        /// </summary>
        public float jumpIntensity = 1f;

        /// <summary>
        /// Maximum jump count.
        /// </summary>
        public int maxJumpCount = 10;

        /// <summary>
        /// Maximum jump boost count.
        /// </summary>
        public int maxJumpBoostCount = 1;

        /// <summary>
        /// Maximum health.
        /// </summary>
        public int maxHealth = 3;

        /// <summary>
        /// If the players' speed should multiply by the pitch.
        /// </summary>
        public bool multiplyPlayerSpeed = true;

        /// <summary>
        /// If custom player models are allowed.
        /// </summary>
        public bool allowCustomPlayerModels = true;

        /// <summary>
        /// If player models can control the player core.
        /// </summary>
        public bool allowPlayerModelControls = false;

        /// <summary>
        /// If players should spawn at the start of the level.
        /// </summary>
        public bool spawnPlayers = true;

        #endregion

        #region Limit

        /// <summary>
        /// If player properties should be limited.
        /// </summary>
        public bool limitPlayer = true;

        /// <summary>
        /// Move speed range.
        /// </summary>
        public Vector2 limitMoveSpeed = new Vector2(20f, 20f);

        /// <summary>
        /// Boost speed range.
        /// </summary>
        public Vector2 limitBoostSpeed = new Vector2(85f, 85f);

        /// <summary>
        /// Boost cooldown range.
        /// </summary>
        public Vector2 limitBoostCooldown = new Vector2(0.1f, 0.1f);

        /// <summary>
        /// Boost minimum time range.
        /// </summary>
        public Vector2 limitBoostMinTime = new Vector2(0.07f, 0.07f);

        /// <summary>
        /// Boost maximum time range.
        /// </summary>
        public Vector2 limitBoostMaxTime = new Vector2(0.18f, 0.18f);

        /// <summary>
        /// Hit cooldown range.
        /// </summary>
        public Vector2 limitHitCooldown = new Vector2(0.001f, 2.5f);

        #endregion

        #region End Level

        /// <summary>
        /// Offset from the levels' end.
        /// </summary>
        public float levelEndOffset = 0.1f;
        public float LevelEndOffset { get => Mathf.Clamp(levelEndOffset, 0.1f, float.MaxValue); set => levelEndOffset = Mathf.Clamp(value, 0.1f, float.MaxValue); }

        /// <summary>
        /// If the level should automatically end when the song reaches the end.
        /// </summary>
        public bool autoEndLevel = true;

        /// <summary>
        /// Function to run when the level ends in the Arcade.
        /// </summary>
        public EndLevelFunction endLevelFunc;

        /// <summary>
        /// End level function data.
        /// </summary>
        public string endLevelData;

        /// <summary>
        /// If level progress should be updated.
        /// </summary>
        public bool endLevelUpdateProgress = true;

        #endregion

        #endregion

        #region Methods

        public override void CopyData(LevelData orig, bool newID = true)
        {
            if (!orig)
                return;

            levelVersion = orig.levelVersion;
            modVersion = orig.modVersion;

            levelStartOffset = orig.levelStartOffset;
            reverse = orig.reverse;
            hideIntro = orig.hideIntro;

            respawnImmediately = orig.allowCustomPlayerModels;
            lockBoost = orig.lockBoost;
            speedMultiplier = orig.speedMultiplier;
            gameMode = orig.gameMode;
            jumpGravity = orig.jumpGravity;
            jumpIntensity = orig.jumpIntensity;
            maxJumpCount = orig.maxJumpCount;
            maxJumpBoostCount = orig.maxJumpBoostCount;
            maxHealth = orig.maxHealth;
            forceReplayLevelOff = orig.forceReplayLevelOff;
            multiplyPlayerSpeed = orig.multiplyPlayerSpeed;
            allowCustomPlayerModels = orig.allowCustomPlayerModels;
            allowPlayerModelControls = orig.allowPlayerModelControls;
            spawnPlayers = orig.spawnPlayers;
            limitPlayer = orig.limitPlayer;

            limitMoveSpeed = orig.limitMoveSpeed;
            limitBoostSpeed = orig.limitBoostSpeed;
            limitBoostCooldown = orig.limitBoostCooldown;
            limitBoostMinTime = orig.limitBoostMinTime;
            limitBoostMaxTime = orig.limitBoostMaxTime;
            limitHitCooldown = orig.limitHitCooldown;

            levelEndOffset = orig.levelEndOffset;
            autoEndLevel = orig.autoEndLevel;
            endLevelFunc = orig.endLevelFunc;
            endLevelData = orig.endLevelData;
            endLevelUpdateProgress = orig.endLevelUpdateProgress;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["level_version"] != null)
                levelVersion = jn["level_version"];
            else
                levelVersion = ProjectArrhythmia.GameVersion.ToString();

            if (jn["mod_version"] != null)
                modVersion = jn["mod_version"];
            else
                modVersion = LegacyPlugin.ModVersion.ToString();

            if (jn["show_intro"] != null)
                hideIntro = jn["show_intro"].AsBool;
            
            if (jn["hide_intro"] != null)
                hideIntro = jn["hide_intro"].AsBool;

            if (jn["respawn_now"] != null)
                respawnImmediately = jn["respawn_now"].AsBool;

            if (jn["lock_boost"] != null)
                lockBoost = jn["lock_boost"].AsBool;

            if (jn["speed_multiplier"] != null)
                speedMultiplier = jn["speed_multiplier"].AsFloat;

            if (jn["gamemode"] != null)
                gameMode = jn["gamemode"].AsInt;

            if (jn["jump_gravity"] != null)
                jumpGravity = jn["jump_gravity"].AsFloat;

            if (jn["jump_intensity"] != null)
                jumpIntensity = jn["jump_intensity"].AsFloat;

            if (jn["max_jump"] != null)
                maxJumpCount = jn["max_jump"].AsInt;

            if (jn["max_jump_boost"] != null)
                maxJumpBoostCount = jn["max_jump_boost"].AsInt;

            if (jn["max_health"] != null)
                maxHealth = jn["max_health"].AsInt;

            if (jn["force_replay_level_off"] != null)
                forceReplayLevelOff = jn["force_replay_level_off"].AsBool;

            if (jn["multiply_player_speed"] != null)
                multiplyPlayerSpeed = jn["multiply_player_speed"].AsBool;

            if (jn["allow_custom_player_models"] != null)
                allowCustomPlayerModels = jn["allow_custom_player_models"].AsBool;
            
            if (jn["allow_player_model_controls"] != null)
                allowPlayerModelControls = jn["allow_player_model_controls"].AsBool;

            if (jn["spawn_players"] != null)
                spawnPlayers = jn["spawn_players"].AsBool;

            if (jn["limit_player"] != null)
                limitPlayer = jn["limit_player"].AsBool;
            else if (jn["mod_version"] != null)
                limitPlayer = false;

            if (jn["limit_move_speed"] != null)
                limitMoveSpeed = Parser.TryParse(jn["limit_move_speed"], new Vector2(20f, 20f));
            if (jn["limit_boost_speed"] != null)
                limitBoostSpeed = Parser.TryParse(jn["limit_boost_speed"], new Vector2(85f, 85f));
            if (jn["limit_boost_cooldown"] != null)
                limitBoostCooldown = Parser.TryParse(jn["limit_boost_cooldown"], new Vector2(0.1f, 0.1f));
            if (jn["limit_boost_min_time"] != null)
                limitBoostMinTime = Parser.TryParse(jn["limit_boost_min_time"], new Vector2(0.07f, 0.07f));
            if (jn["limit_boost_max_time"] != null)
                limitBoostMaxTime = Parser.TryParse(jn["limit_boost_max_time"], new Vector2(0.18f, 0.18f));
            if (jn["limit_hit_cooldown"] != null)
                limitHitCooldown = Parser.TryParse(jn["limit_hit_cooldown"], new Vector2(2.5f, 2.5f));

            if (jn["level_start_offset"] != null)
                LevelStartOffset = Parser.TryParse(jn["level_start_offset"], 0f);
            
            if (jn["level_end_offset"] != null)
                LevelEndOffset = Parser.TryParse(jn["level_end_offset"], 0.1f);

            if (jn["auto_end_level"] != null)
                autoEndLevel = jn["auto_end_level"].AsBool;

            if (jn["end_level_func"] != null)
                endLevelFunc = (EndLevelFunction)jn["end_level_func"].AsInt;

            if (!string.IsNullOrEmpty(jn["end_level_data"]))
                endLevelData = jn["end_level_data"];

            if (jn["end_level_update_progress"] != null)
                endLevelUpdateProgress = jn["end_level_update_progress"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["level_version"] = ProjectArrhythmia.GAME_VERSION;
            jn["mod_version"] = modVersion;

            if (hideIntro)
                jn["hide_intro"] = hideIntro;

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
            
            if (allowPlayerModelControls)
                jn["allow_player_model_controls"] = allowPlayerModelControls;
            
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

            if (LevelStartOffset != 0f)
                jn["level_start_offset"] = LevelStartOffset;
            
            if (LevelEndOffset != 0.1f)
                jn["level_end_offset"] = LevelEndOffset;

            if (!autoEndLevel)
                jn["auto_end_level"] = autoEndLevel;

            if (endLevelFunc != EndLevelFunction.EndLevelMenu)
                jn["end_level_func"] = (int)endLevelFunc;

            if (!string.IsNullOrEmpty(endLevelData))
                jn["end_level_data"] = endLevelData;

            if (!endLevelUpdateProgress)
                jn["end_level_update_progress"] = endLevelUpdateProgress;

            return jn;
        }

        #endregion
    }
}
