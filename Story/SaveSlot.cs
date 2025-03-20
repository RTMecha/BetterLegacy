using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Story
{
    public class SaveSlot
    {
        public SaveSlot()
        {

        }

        public SaveSlot(int slot)
        {
            Slot = slot;
        }

        #region Data

        /// <summary>
        /// The currently saved chapter.
        /// </summary>
        public int ChapterIndex => LoadInt("Chapter", 0);

        /// <summary>
        /// The currently saved level index.
        /// </summary>
        public int LevelSequenceIndex => LoadInt($"DOC{(ChapterIndex + 1).ToString("00")}Progress", 0);

        /// <summary>
        /// Path to the current save slot file.
        /// </summary>
        public string StorySavesPath => $"{RTFile.ApplicationDirectory}profile/story_saves_{RTString.ToStoryNumber(Slot)}{FileFormat.LSS.Dot()}";

        public JSONNode storySavesJSON;

        int slot;

        /// <summary>
        /// The current story save slot.
        /// </summary>
        public int Slot
        {
            get => slot;
            set
            {
                slot = value;
                Load();
            }
        }

        /// <summary>
        /// All level saves in the current story save slot.
        /// </summary>
        public List<PlayerData> Saves { get; set; } = new List<PlayerData>();

        #endregion

        #region Saving

        /// <summary>
        /// Updates the current story levels' player data.
        /// </summary>
        public void UpdateCurrentLevelProgress()
        {
            if (LevelManager.CurrentLevel == null)
                return;

            var level = LevelManager.CurrentLevel;

            CoreHelper.Log($"Setting Player Data");

            // will zen / practice ever be implemented to the story?
            //if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            //    return;

            var makeNewPlayerData = level.playerData == null;
            if (makeNewPlayerData)
                level.playerData = new PlayerData(level);
            level.playerData.LevelName = level.metadata?.beatmap?.name; // update level name

            CoreHelper.Log($"Updating save data\n" +
                $"New Player Data = {makeNewPlayerData}\n" +
                $"Deaths [OLD = {level.playerData.Deaths} > NEW = {GameManager.inst.deaths.Count}]\n" +
                $"Hits: [OLD = {level.playerData.Hits} > NEW = {GameManager.inst.hits.Count}]\n" +
                $"Boosts: [OLD = {level.playerData.Boosts} > NEW = {LevelManager.BoostCount}]");

            level.playerData.Update(GameManager.inst.deaths.Count, GameManager.inst.hits.Count, LevelManager.BoostCount, true);

            if (Saves.TryFindIndex(x => x.ID == level.id, out int saveIndex))
                Saves[saveIndex] = level.playerData;
            else
                Saves.Add(level.playerData);

            SaveProgress();
        }

        /// <summary>
        /// Saves all story level player data.
        /// </summary>
        public void SaveProgress()
        {
            storySavesJSON["lvl"] = new JSONArray();
            for (int i = 0; i < Saves.Count; i++)
                storySavesJSON["lvl"][i] = Saves[i].ToJSON();

            Save();
        }

        /// <summary>
        /// Writes to the current story save slot file.
        /// </summary>
        public void Save()
        {
            try
            {
                RTFile.WriteToFile(StorySavesPath, storySavesJSON.ToString());
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Saves a <see cref="bool"/> value to the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to save.</param>
        /// <param name="value">Value to save.</param>
        public void SaveBool(string name, bool value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["bool"] = value;
            Save();
        }

        /// <summary>
        /// Saves a <see cref="int"/> value to the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to save.</param>
        /// <param name="value">Value to save.</param>
        public void SaveInt(string name, int value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["int"] = value;
            Save();
        }

        /// <summary>
        /// Saves a <see cref="float"/> value to the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to save.</param>
        /// <param name="value">Value to save.</param>
        public void SaveFloat(string name, float value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["float"] = value;
            Save();
        }

        /// <summary>
        /// Saves a <see cref="string"/> value to the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to save.</param>
        /// <param name="value">Value to save.</param>
        public void SaveString(string name, string value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            if (string.IsNullOrEmpty(value))
                return;
            storySavesJSON["saves"][name]["string"] = value;
            Save();
        }

        /// <summary>
        /// Saves a <see cref="JSONNode"/> value to the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to save.</param>
        /// <param name="value">Value to save.</param>
        public void SaveNode(string name, JSONNode value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            if (value == null)
                return;
            storySavesJSON["saves"][name][value.IsArray ? "array" : "object"] = value;
            Save();
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads the current story save slot file.
        /// </summary>
        public void Load()
        {
            storySavesJSON = JSON.Parse(RTFile.FileExists(StorySavesPath) ? RTFile.ReadFromFile(StorySavesPath) : "{}");

            Saves.Clear();
            if (storySavesJSON["lvl"] != null)
                for (int i = 0; i < storySavesJSON["lvl"].Count; i++)
                    Saves.Add(PlayerData.Parse(storySavesJSON["lvl"][i]));
        }

        /// <summary>
        /// Loads a <see cref="bool"/> value from the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to load.</param>
        /// <param name="defaultValue">Default value if no value exists.</param>
        /// <returns>Returns the found value.</returns>
        public bool LoadBool(string name, bool defaultValue) => !HasSave(name) || storySavesJSON["saves"][name]["bool"] == null ? defaultValue : storySavesJSON["saves"][name]["bool"].AsBool;

        /// <summary>
        /// Loads a <see cref="int"/> value from the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to load.</param>
        /// <param name="defaultValue">Default value if no value exists.</param>
        /// <returns>Returns the found value.</returns>
        public int LoadInt(string name, int defaultValue) => !HasSave(name) || storySavesJSON["saves"][name]["int"] == null ? defaultValue : storySavesJSON["saves"][name]["int"].AsInt;

        /// <summary>
        /// Loads a <see cref="float"/> value from the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to load.</param>
        /// <param name="defaultValue">Default value if no value exists.</param>
        /// <returns>Returns the found value.</returns>
        public float LoadFloat(string name, float defaultValue) => !HasSave(name) || storySavesJSON["saves"][name]["float"] == null ? defaultValue : storySavesJSON["saves"][name]["float"].AsFloat;

        /// <summary>
        /// Loads a <see cref="string"/> value from the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to load.</param>
        /// <param name="defaultValue">Default value if no value exists.</param>
        /// <returns>Returns the found value.</returns>
        public string LoadString(string name, string defaultValue) => !HasSave(name) || storySavesJSON["saves"][name]["string"] == null ? defaultValue : storySavesJSON["saves"][name]["string"].Value;

        /// <summary>
        /// Loads a <see cref="JSON"/> value from the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to load.</param>
        /// <returns>Returns the found value.</returns>
        public JSONNode LoadJSON(string name) => !HasSave(name) ? null : storySavesJSON["saves"][name]["array"] != null ? storySavesJSON["saves"][name]["array"] : storySavesJSON["saves"][name]["object"] != null ? storySavesJSON["saves"][name]["object"] : null;

        /// <summary>
        /// Checks if a value exists in the current story save slot.
        /// </summary>
        /// <param name="name">Name of the value to check.</param>
        /// <returns>Returns true if the value exists, otherwise returns false.</returns>
        public bool HasSave(string name) => storySavesJSON["saves"][name] != null;

        #endregion
    }
}
