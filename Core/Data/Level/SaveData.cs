using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Data.Level
{
    /// <summary>
    /// Represents the saved data of a played level.
    /// </summary>
    public class SaveData : PAObject<SaveData>, IAchievementData
    {
        public SaveData() { }

        public SaveData(Level level)
        {
            ID = level.id;
            LevelName = level.metadata?.beatmap?.name;
        }

        #region Values

        /// <summary>
        /// If the saved data should write to JSON.
        /// </summary>
        public override bool ShouldSerialize => !string.IsNullOrEmpty(ID) && ID != "0" && !ID.Contains("-") && (Hits >= 0 || Deaths >= 0 || Boosts >= 0 || Completed || Unlocked || UnlockedAchievements != null && !UnlockedAchievements.IsEmpty() || Variables != null && !Variables.IsEmpty());

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
        /// All unlocked custom achievements.
        /// </summary>
        public Dictionary<string, bool> UnlockedAchievements { get; set; }

        /// <summary>
        /// Last time the user played the level.
        /// </summary>
        public DateTime? LastPlayed { get; set; }

        /// <summary>
        /// Dictionary of stored variables.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Methods

        public override void CopyData(SaveData orig, bool newID = true)
        {
            LevelName = orig.LevelName;
            ID = orig.ID;
            Completed = orig.Completed;
            PlayedTimes = orig.PlayedTimes;
            TimeInLevel = orig.TimeInLevel;
            Percentage = orig.Percentage;
            LevelLength = orig.LevelLength;
            Unlocked = orig.Unlocked;
            Hits = orig.Hits;
            Deaths = orig.Deaths;
            Boosts = orig.Boosts;
            UnlockedAchievements = new Dictionary<string, bool>(orig.UnlockedAchievements);
            Variables = new Dictionary<string, string>(orig.Variables);
            LastPlayed = orig.LastPlayed;
        }

        #region Updating

        /// <summary>
        /// Updates the current state of the save.
        /// </summary>
        public void UpdateState()
        {
            LastPlayed = DateTime.Now;

            if (!AudioManager.inst.CurrentAudioSource || !AudioManager.inst.CurrentAudioSource.clip)
                return;

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
        public void Update()
        {
            if (Hits > RTBeatmap.Current.hits.Count)
                Hits = RTBeatmap.Current.hits.Count;

            if (Deaths > RTBeatmap.Current.deaths.Count)
                Deaths = RTBeatmap.Current.deaths.Count;

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

                TimeInLevel = RTBeatmap.Current.levelTimer.time;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        #endregion

        #region Achievements

        /// <summary>
        /// Locks the achievement, marking it incomplete.
        /// </summary>
        /// <param name="achievement">Achievement to lock.</param>
        public void LockAchievement(Achievement achievement)
        {
            if (achievement && UnlockedAchievements != null && UnlockedAchievements.TryGetValue(achievement.id, out bool unlocked) && unlocked)
            {
                UnlockedAchievements[achievement.id] = false;
                LevelManager.SaveProgress();
                CoreHelper.Log($"Locked achievement {achievement.name}");
            }
        }

        /// <summary>
        /// Locks the achievement, marking it incomplete.
        /// </summary>
        /// <param name="id">ID to find a matching achievement and lock.</param>
        public void LockAchievement(string id)
        {
            var list = LevelManager.CurrentLevel.GetAchievements();

            if (!list.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            LockAchievement(achievement);
        }

        /// <summary>
        /// Unlocks the achievement, marking it complete.
        /// </summary>
        /// <param name="achievement">Achievement to unlock.</param>
        public void UnlockAchievement(Achievement achievement)
        {
            if (!achievement)
                return;

            achievement.unlocked = true;

            bool showAchievement = false;

            if (UnlockedAchievements == null)
                UnlockedAchievements = new Dictionary<string, bool>();

            // don't save locally if the achievement is shared
            if (!achievement.shared && (!UnlockedAchievements.TryGetValue(achievement.id, out bool unlocked) || !unlocked))
            {
                UnlockedAchievements[achievement.id] = true;
                LevelManager.SaveProgress();
                showAchievement = true;
            }

            // if the achievement is shared across the game
            if (achievement.shared && (!AchievementManager.unlockedCustomAchievements.TryGetValue(achievement.id, out bool customUnlocked) || !customUnlocked))
            {
                AchievementManager.unlockedCustomAchievements[achievement.id] = true;
                LegacyPlugin.SaveProfile();
                showAchievement = true;
            }

            if (showAchievement)
                AchievementManager.inst.ShowAchievement(achievement);
        }

        /// <summary>
        /// Unlocks the achievement, marking it complete.
        /// </summary>
        /// <param name="id">ID to find a matching achievement and unlock.</param>
        public void UnlockAchievement(string id)
        {
            var list = LevelManager.CurrentLevel.GetAchievements();

            if (!list.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            UnlockAchievement(achievement);
        }

        /// <summary>
        /// Checks if an achievement with a matching ID exists and if it is unlocked.
        /// </summary>
        /// <param name="id">ID to find a matching achievement.</param>
        /// <returns>Returns true if an achievement is found and it is unlocked, otherwise returns false.</returns>
        public bool AchievementUnlocked(string id)
        {
            var list = LevelManager.CurrentLevel.GetAchievements();

            if (list == null)
                return false;

            if (!list.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return false;
            }

            return achievement.unlocked;
        }

        #endregion

        #region JSON

        public override void ReadJSON(JSONNode jn)
        {
            LevelName = jn["n"];
            ID = jn["id"];
            Completed = jn["c"].AsBool;
            PlayedTimes = jn["pt"].AsInt;
            TimeInLevel = jn["t"].AsFloat;
            Percentage = jn["p"].AsFloat;
            LevelLength = jn["l"].AsFloat;
            Unlocked = jn["u"].AsBool;

            if (jn["h"] != null)
                Hits = jn["h"].AsInt;
            if (jn["d"] != null)
                Deaths = jn["d"].AsInt;
            if (jn["b"] != null)
                Boosts = jn["b"].AsInt;

            if (jn["ach"] != null)
            {
                UnlockedAchievements = new Dictionary<string, bool>();
                for (int i = 0; i < jn["ach"].Count; i++)
                {
                    var unlocked = jn["ach"][i]["u"].AsBool;
                    if (unlocked)
                        UnlockedAchievements[jn["ach"][i]["id"]] = unlocked;
                }
            }

            if (jn["vars"] != null)
            {
                Variables = new Dictionary<string, string>();
                for (int i = 0; i < jn["vars"].Count; i++)
                {
                    var jnVar = jn["vars"][i];
                    if (jnVar["n"] != null)
                        Variables[jnVar["n"]] = jnVar["v"];
                }
            }

            if (jn["lp"] != null)
                LastPlayed = DateTime.ParseExact(jn["lp"], LegacyPlugin.DATE_TIME_FORMAT, null);

        }

        /// <summary>
        /// Parses a <see cref="SaveData"/> from a vanilla version JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed player data.</returns>
        public static SaveData ParseVanilla(JSONNode jn) => new SaveData
        {
            ID = jn["level_data"]["id"],
            Completed = jn["play_data"]["finished"].AsBool,
            Hits = jn["play_data"]["hits"].AsInt,
            Deaths = jn["play_data"]["deaths"].AsInt,
        };

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(LevelName))
                jn["n"] = LevelName;
            jn["id"] = ID;
            if (Completed)
                jn["c"] = Completed;
            if (Hits >= 0)
                jn["h"] = Hits;
            if (Deaths >= 0)
                jn["d"] = Deaths;
            if (Boosts >= 0)
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

            if (UnlockedAchievements != null)
            {
                int num = 0;
                foreach (var keyValuePair in UnlockedAchievements)
                {
                    var ach = Parser.NewJSONObject();
                    ach["id"] = keyValuePair.Key;
                    ach["u"] = keyValuePair.Value;
                    jn["ach"][num] = ach;
                    num++;
                }
            }

            if (Variables != null)
            {
                int index = 0;
                foreach (var variable in Variables)
                {
                    jn["vars"][index]["n"] = variable.Key;
                    jn["vars"][index]["v"] = variable.Value;
                }
            }

            if (LastPlayed is DateTime lastPlayed)
                jn["lp"] = lastPlayed.ToString(LegacyPlugin.DATE_TIME_FORMAT);

            return jn;
        }

        #endregion

        public string GetTimeSinceLastPlayed()
        {
            if (LastPlayed is not DateTime lastPlayed)
                return string.Empty;

            return DateTime.Now.Subtract(lastPlayed).ToString();
        }

        public override string ToString() => $"{ID} - {LevelName} - Hits: {Hits} Deaths: {Deaths}";

        #endregion
    }
}
