using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Level
{
    public class SaveCollectionData : PAObject<SaveCollectionData>, IPacket, IAchievementData
    {
        #region Constructors

        public SaveCollectionData() { }

        public SaveCollectionData(LevelCollection levelCollection)
        {
            ID = levelCollection.id;
            LevelCollectionName = levelCollection.name;
        }

        #endregion

        #region Values

        /// <summary>
        /// If the saved data should write to JSON.
        /// </summary>
        public override bool ShouldSerialize => !string.IsNullOrEmpty(ID) && ID != "0" && !ID.Contains("-") && (Completed || UnlockedAchievements != null && !UnlockedAchievements.IsEmpty() || Variables != null && !Variables.IsEmpty());

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

        #region Functions

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

        #region Packet

        public void ReadPacket(NetworkReader reader)
        {
            ID = reader.ReadString();
            LevelCollectionName = reader.ReadString();
            Completed = reader.ReadBoolean();
            PlayedTimes = reader.ReadInt32();
            UnlockedAchievements = reader.ReadDictionary(() => reader.ReadString(), () => reader.ReadBoolean());
            Variables = reader.ReadDictionary(() => reader.ReadString(), () => reader.ReadString());
            var hasLastPlayed = reader.ReadBoolean();
            if (hasLastPlayed)
                LastPlayed = new DateTime(reader.ReadInt64());
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(ID);
            writer.Write(LevelCollectionName);
            writer.Write(Completed);
            writer.Write(PlayedTimes);
            writer.Write(UnlockedAchievements,
                writeKey: key => writer.Write(key),
                writeValue: value => writer.Write(value));
            writer.Write(Variables,
                writeKey: key => writer.Write(key),
                writeValue: value => writer.Write(value));
            var hasLastPlayed = LastPlayed.HasValue;
            writer.Write(hasLastPlayed);
            if (hasLastPlayed)
                writer.Write(LastPlayed.Value.Ticks);
        }

        #endregion

        #region Updating

        /// <summary>
        /// Updates the current state of the save.
        /// </summary>
        public void UpdateState()
        {
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
