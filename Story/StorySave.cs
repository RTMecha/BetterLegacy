using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Represents a story save slot.
    /// </summary>
    public class StorySave : Exists
    {
        public StorySave() { }

        public StorySave(int slot) => Slot = slot;

        #region Data

        /// <summary>
        /// The currently saved chapter.
        /// </summary>
        public int ChapterIndex => LoadInt("Chapter", 0);

        /// <summary>
        /// Gets the currently saved level index of a chapter.
        /// </summary>
        /// <param name="chapterIndex">The chapter progress.</param>
        public int GetLevelSequenceIndex(int chapterIndex) => LoadInt($"DOC{RTString.ToStoryNumber(chapterIndex)}Progress", 0);

        /// <summary>
        /// Path to the current save slot file.
        /// </summary>
        public string StorySavesPath => $"{RTFile.ApplicationDirectory}profile/story_saves_{RTString.ToStoryNumber(Slot)}{FileFormat.LSS.Dot()}";

        JSONNode storySavesJSON;

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
        public List<SaveData> Saves { get; set; } = new List<SaveData>();

        #endregion

        #region Saving

        /// <summary>
        /// Updates the current story levels' player data.
        /// </summary>
        public void UpdateCurrentLevelProgress()
        {
            var level = LevelManager.CurrentLevel;

            if (!level)
                return;

            CoreHelper.Log($"Setting Player Data");

            // will zen / practice ever be implemented to the story?
            //if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            //    return;

            bool makeNewSaveData = !level.saveData;
            if (makeNewSaveData)
                level.saveData = new SaveData(level);
            level.saveData.LevelName = level.metadata?.beatmap?.name; // update level name

            CoreHelper.Log($"Updating save data\n" +
                $"New Player Data = {makeNewSaveData}\n" +
                $"Deaths [OLD = {level.saveData.Deaths} > NEW = {RTBeatmap.Current.deaths.Count}]\n" +
                $"Hits: [OLD = {level.saveData.Hits} > NEW = {RTBeatmap.Current.hits.Count}]\n" +
                $"Boosts: [OLD = {level.saveData.Boosts} > NEW = {RTBeatmap.Current.boosts.Count}]");

            level.saveData.Update(RTBeatmap.Current.deaths.Count, RTBeatmap.Current.hits.Count, RTBeatmap.Current.boosts.Count, true);

            if (Saves.TryFindIndex(x => x.ID == level.id, out int saveIndex))
                Saves[saveIndex] = level.saveData;
            else
                Saves.Add(level.saveData);

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
        /// Saves the progress of the story mode.
        /// </summary>
        /// <param name="chapter">Chapter to save.</param>
        /// <param name="level">Level to save.</param>
        public void SaveProgress(int chapter, int level)
        {
            SaveInt("Chapter", chapter);
            SaveInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", level);
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
                    Saves.Add(SaveData.Parse(storySavesJSON["lvl"][i]));
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
        public bool HasSave(string name) => storySavesJSON != null && storySavesJSON["saves"][name] != null;

        #endregion
    }
}
