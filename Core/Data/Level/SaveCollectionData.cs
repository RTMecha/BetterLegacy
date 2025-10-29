using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Level
{
    public class SaveCollectionData : PAObject<SaveCollectionData>, IAchievementData
    {
        public SaveCollectionData() { }

        public SaveCollectionData(LevelCollection levelCollection)
        {
            ID = levelCollection.id;
            LevelCollectionName = levelCollection.name;
        }

        #region Values

        /// <summary>
        /// If the saved data should write to JSON.
        /// </summary>
        public bool ShouldSerialize => !string.IsNullOrEmpty(ID) && ID != "0" && !ID.Contains("-") && (Completed || UnlockedAchievements != null && !UnlockedAchievements.IsEmpty() || Variables != null && !Variables.IsEmpty());

        /// <summary>
        /// Level collection name for readable display.
        /// </summary>
        public string LevelCollectionName { get; set; }

        /// <summary>
        /// Level collection ID reference.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// If the level collection has been completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// How many times the level collection has been played in.
        /// </summary>
        public int PlayedTimes { get; set; }

        public Dictionary<string, bool> UnlockedAchievements { get; set; }

        /// <summary>
        /// Last time the user played the level collection.
        /// </summary>
        public DateTime? LastPlayed { get; set; }

        /// <summary>
        /// Dictionary of stored variables.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Methods

        public override void CopyData(SaveCollectionData orig, bool newID = true)
        {
            LevelCollectionName = orig.LevelCollectionName;
            ID = orig.ID;
            Completed = orig.Completed;
            PlayedTimes = orig.PlayedTimes;
            UnlockedAchievements = new Dictionary<string, bool>(orig.UnlockedAchievements);
            Variables = new Dictionary<string, string>(orig.Variables);
            LastPlayed = orig.LastPlayed;
        }

        #region Updating

        /// <summary>
        /// Updates several values of the save data.
        /// </summary>
        public void Update()
        {
            LastPlayed = DateTime.Now;
        }

        /// <summary>
        /// Updates several values of the save data.
        /// </summary>
        /// <param name="completed">Completed count.</param>
        public void Update(bool completed)
        {
            Completed = completed;
            LastPlayed = DateTime.Now;
        }

        #endregion

        #region Achievements

        public void LockAchievement(Achievement achievement)
        {
            if (achievement && UnlockedAchievements != null && UnlockedAchievements.TryGetValue(achievement.id, out bool unlocked) && unlocked)
            {
                UnlockedAchievements[achievement.id] = false;
                LevelManager.SaveProgress();
                CoreHelper.Log($"Locked achievement {achievement.name}");
            }
        }

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
            LevelCollectionName = jn["n"];
            ID = jn["id"];
            Completed = jn["c"].AsBool;
            PlayedTimes = jn["pt"].AsInt;

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

        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(LevelCollectionName))
                jn["n"] = LevelCollectionName;
            jn["id"] = ID;
            if (Completed)
                jn["c"] = Completed;
            if (PlayedTimes != 0)
                jn["pt"] = PlayedTimes;

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

            return jn;
        }

        #endregion

        public override string ToString() => $"{ID} - {LevelCollectionName}";

        #endregion
    }
}
