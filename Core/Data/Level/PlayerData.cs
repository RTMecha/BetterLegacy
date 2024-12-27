using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Represents the data of a played level.
    /// </summary>
    public class PlayerData
    {
        public PlayerData() { }

        #region Properties

        /// <summary>
        /// Level name for readable display.
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Level ID reference.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// If the level has been completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Amount of times the player has been hit.
        /// </summary>
        public int Hits { get; set; } = -1;

        /// <summary>
        /// Amount of times the player has died.
        /// </summary>
        public int Deaths { get; set; } = -1;

        /// <summary>
        /// Amount of times the player has boosted.
        /// </summary>
        public int Boosts { get; set; } = -1;

        /// <summary>
        /// How many times the level has been played in.
        /// </summary>
        public int PlayedTimes { get; set; }

        /// <summary>
        /// How long the user has spent in the level.
        /// </summary>
        public float TimeInLevel { get; set; }

        /// <summary>
        /// Percentage progress through the song.
        /// </summary>
        public float Percentage { get; set; }

        /// <summary>
        /// Length of the level.
        /// </summary>
        public float LevelLength { get; set; }

        /// <summary>
        /// If the level has been unlocked and is accessible in menus.
        /// </summary>
        public bool Unlocked { get; set; }

        /// <summary>
        /// All unlocked custom achievements. TODO: is this necessary? or should it be something else...
        /// </summary>
        public List<Achievement> UnlockedAchievements { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Updates several values of the player data.
        /// </summary>
        public void Update()
        {
            if (Hits > GameManager.inst.hits.Count)
                Hits = GameManager.inst.hits.Count;

            if (Deaths > GameManager.inst.deaths.Count)
                Deaths = GameManager.inst.deaths.Count;

            var l = AudioManager.inst.CurrentAudioSource.clip.length;
            if (LevelLength != l)
                LevelLength = l;

            float calc = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length * 100f;

            if (Percentage < calc)
                Percentage = calc;
        }

        /// <summary>
        /// Updates several values of the player data.
        /// </summary>
        /// <param name="deaths">Death count.</param>
        /// <param name="hits">Hit count.</param>
        /// <param name="boosts">Boost count.</param>
        /// <param name="completed">Completed count.</param>
        public void Update(int deaths, int hits, int boosts, bool completed)
        {
            if (Deaths == -1 || Deaths > deaths)
                Deaths = deaths;
            if (Hits == -1 || Hits > hits)
                Hits = hits;
            if (Boosts == -1 || Boosts > boosts)
                Boosts = boosts;
            Completed = completed;

            try
            {
                if (!AudioManager.inst.CurrentAudioSource.clip)
                    return;

                var length = AudioManager.inst.CurrentAudioSource.clip.length;
                if (LevelLength != length)
                    LevelLength = length;

                float calc = AudioManager.inst.CurrentAudioSource.time / length * 100f;

                if (Percentage < calc)
                    Percentage = calc;

                TimeInLevel = LevelManager.timeInLevel;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Parses a <see cref="PlayerData"/> from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed player data.</returns>
        public static PlayerData Parse(JSONNode jn) => new PlayerData
        {
            LevelName = jn["n"],
            ID = jn["id"],
            Completed = jn["c"].AsBool,
            Hits = jn["h"].AsInt,
            Deaths = jn["d"].AsInt,
            Boosts = jn["b"].AsInt,
            PlayedTimes = jn["pt"].AsInt,
            TimeInLevel = jn["t"].AsFloat,
            Percentage = jn["p"].AsFloat,
            LevelLength = jn["l"].AsFloat,
            Unlocked = jn["u"].AsBool,
        };

        /// <summary>
        /// Converts the player data to a JSON node.
        /// </summary>
        /// <returns>Returns a JSON representing the player data.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            if (!string.IsNullOrEmpty(LevelName))
                jn["n"] = LevelName;
            jn["id"] = ID;
            jn["c"] = Completed;
            jn["h"] = Hits;
            jn["d"] = Deaths;
            jn["b"] = Boosts;
            if (PlayedTimes != 0)
                jn["pt"] = PlayedTimes;
            if (TimeInLevel != 0f)
                jn["t"] = TimeInLevel;
            if (Percentage != 0f)
                jn["p"] = Percentage;
            if (LevelLength != 0f)
                jn["l"] = LevelLength;
            if (Unlocked)
                jn["u"] = Unlocked;

            return jn;
        }

        public override string ToString() => $"{ID} - Hits: {Hits} Deaths: {Deaths}";

        #endregion
    }
}
