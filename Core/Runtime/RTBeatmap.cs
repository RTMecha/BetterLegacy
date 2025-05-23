using System.Collections.Generic;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime
{
    /// <summary>
    /// Caches the current beatmap game states. (E.G. usually stuff that should only be assigned on level start, such as game speed, challenge mode, etc.)
    /// <br></br>Can also include other stuff that doesn't fit in <see cref="RTLevel"/>, such as hit data.
    /// </summary>
    public class RTBeatmap : Exists
    {
        /// <summary>
        /// The current runtime beatmap.
        /// </summary>
        public static RTBeatmap Current { get; set; } = new RTBeatmap();

        /// <summary>
        /// Cached challenge mode.
        /// </summary>
        public ChallengeMode challengeMode = ChallengeMode.Normal;

        /// <summary>
        /// Cached game speed.
        /// </summary>
        public GameSpeed gameSpeed = GameSpeed.X1_0;

        /// <summary>
        /// Resets the current beatmap cache.
        /// </summary>
        public void Reset()
        {
            hits.Clear();
            deaths.Clear();
            boosts.Clear();

            challengeMode = CoreConfig.Instance.ChallengeModeSetting.Value;
            gameSpeed = CoreConfig.Instance.GameSpeedSetting.Value;

            lives = challengeMode.Lives;
        }

        /// <summary>
        /// Total time a user has been in a level.
        /// </summary>
        public RTTimer levelTimer;

        /// <summary>
        /// Amount of time the game has been paused.
        /// </summary>
        public RTTimer pausedTimer;

        /// <summary>
        /// Used for the no music achievement.
        /// </summary>
        public int CurrentMusicVolume { get; set; }

        #region Game State Checks

        /// <summary>
        /// Players take damage but lose health and don't die.
        /// </summary>
        public bool IsPractice => challengeMode == ChallengeMode.Practice;

        /// <summary>
        /// Players take damage and can die if health hits zero.
        /// </summary>
        public bool IsNormal => challengeMode == ChallengeMode.Normal;

        /// <summary>
        /// Players take damage and only have 1 life. When they die, restart the level.
        /// </summary>
        public bool Is1Life => challengeMode == ChallengeMode.OneLife;

        /// <summary>
        /// Players take damage and only have 1 health. When they die, restart the level.
        /// </summary>
        public bool IsNoHit => challengeMode == ChallengeMode.OneHit;

        /// <summary>
        /// If the player is invincible.
        /// </summary>
        public bool Invincible => CoreHelper.InEditor ? (EditorManager.inst.isEditing || RTPlayer.ZenModeInEditor) : !challengeMode.Damageable;

        /// <summary>
        /// The current pitch setting.
        /// </summary>
        public float Pitch => CoreHelper.InEditor || CoreHelper.InStory ? 1f : gameSpeed.Pitch;

        #endregion

        #region Player Conditions

        /// <summary>
        /// Amount of lives left until the level restarts.
        /// </summary>
        public int lives = -1;

        /// <summary>
        /// If the player is out of lives.
        /// </summary>
        public bool OutOfLives => lives <= 0;

        /// <summary>
        /// Data points representing the times the players got hit.
        /// </summary>
        public List<PlayerDataPoint> hits = new List<PlayerDataPoint>();

        /// <summary>
        /// Data points representing the times the players died.
        /// </summary>
        public List<PlayerDataPoint> deaths = new List<PlayerDataPoint>();

        /// <summary>
        /// Data points representing the times the players boosted.
        /// </summary>
        public List<PlayerDataPoint> boosts = new List<PlayerDataPoint>();

        /// <summary>
        /// If a player has been hit on a tick.
        /// </summary>
        public bool playerHit = false;

        /// <summary>
        /// If a player has died on a tick.
        /// </summary>
        public bool playerDied = false;

        #endregion
    }
}
