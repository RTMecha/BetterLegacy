using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Represents a story save slot.
    /// </summary>
    public class StorySave : Exists, IPacket
    {
        #region Constructors

        public StorySave() { }

        public StorySave(int slot) => Slot = slot;

        #endregion

        #region Values

        /// <summary>
        /// The name of the save.
        /// </summary>
        public string SaveName => LoadString("SaveName", "[ DEFAULT ]");

        /// <summary>
        /// The currently saved chapter.
        /// </summary>
        public int ChapterIndex => LoadInt("Chapter", 0);

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

        #region Functions

        #region Packet

        public void ReadPacket(NetworkReader reader)
        {
            slot = reader.ReadInt32();
            Packet.ReadPacketList(Saves, reader);
            var hasData = reader.ReadBoolean();
            if (!hasData)
                return;

            var storySaveVariablesCount = reader.ReadInt32();
            for (int i = 0; i < storySaveVariablesCount; i++)
            {
                var key = reader.ReadString();
                var value = Parser.NewJSONObject();
                var boolValue = reader.ReadString();
                if (!string.IsNullOrEmpty(boolValue))
                    value["bool"] = Parser.TryParse(boolValue, false);
                var intValue = reader.ReadString();
                if (!string.IsNullOrEmpty(intValue))
                    value["int"] = Parser.TryParse(intValue, 0);
                var floatValue = reader.ReadString();
                if (!string.IsNullOrEmpty(floatValue))
                    value["float"] = Parser.TryParse(floatValue, 0);
                var stringValue = reader.ReadString();
                if (!string.IsNullOrEmpty(stringValue))
                    value["string"] = Parser.TryParse(stringValue, 0);
                var arrayValue = reader.ReadString();
                if (!string.IsNullOrEmpty(arrayValue))
                    value["array"] = JSON.Parse(arrayValue);
                var objectValue = reader.ReadString();
                if (!string.IsNullOrEmpty(objectValue))
                    value["object"] = JSON.Parse(objectValue);
                storySavesJSON[key] = value;
            }
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(slot);
            Packet.WritePacketList(Saves, writer);
            var hasData = storySavesJSON != null;
            writer.Write(hasData);
            if (!hasData)
                return;

            if (storySavesJSON["saves"] == null)
            {
                writer.Write(0);
                return;
            }
            var storySaveVariables = storySavesJSON["saves"].Linq;
            writer.Write(storySaveVariables.Count());
            foreach (var keyValuePair in storySaveVariables)
            {
                writer.Write(keyValuePair.Key);
                var value = keyValuePair.Value;
                writer.Write(value.GetValueOrDefault("bool", string.Empty).Value);
                writer.Write(value.GetValueOrDefault("int", string.Empty).Value);
                writer.Write(value.GetValueOrDefault("float", string.Empty).Value);
                writer.Write(value.GetValueOrDefault("string", string.Empty).Value);
                if (value["array"] != null)
                    writer.Write(value["array"].ToString());
                else
                    writer.Write(string.Empty);
                if (value["object"] != null)
                    writer.Write(value["object"].ToString());
                else
                    writer.Write(string.Empty);
            }
        }

        #endregion

        /// <summary>
        /// Gets the currently saved level index of a chapter.
        /// </summary>
        /// <param name="chapterIndex">The chapter progress.</param>
        public int GetLevelSequenceIndex(int chapterIndex) => LoadInt($"DOC{RTString.ToStoryNumber(chapterIndex)}Progress", 0);

        #region Saving

        /// <summary>
        /// Updates the current story levels' player data.
        /// </summary>
        public void UpdateCurrentLevelProgress()
        {
            var currentLevel = LevelManager.CurrentLevel;

            if (!currentLevel)
                return;

            CoreHelper.Log($"Setting Player Data");

            // will zen / practice ever be implemented to the story?
            //if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            //    return;

            bool makeNewSaveData = !currentLevel.saveData;
            if (makeNewSaveData)
                currentLevel.saveData = new SaveData(currentLevel);
            currentLevel.saveData.LevelName = currentLevel.metadata?.beatmap?.name; // update level name

            CoreHelper.Log($"Updating save data\n" +
                $"New Player Data = {makeNewSaveData}\n" +
                $"Deaths [OLD = {currentLevel.saveData.Deaths} > NEW = {RTBeatmap.Current.deaths.Count}]\n" +
                $"Hits: [OLD = {currentLevel.saveData.Hits} > NEW = {RTBeatmap.Current.hits.Count}]\n" +
                $"Boosts: [OLD = {currentLevel.saveData.Boosts} > NEW = {RTBeatmap.Current.boosts.Count}]");

            currentLevel.saveData.LastPlayed = DateTime.Now;
            currentLevel.saveData.Update(RTBeatmap.Current.deaths.Count, RTBeatmap.Current.hits.Count, RTBeatmap.Current.boosts.Count, true);

            if (Saves.TryFindIndex(x => x.ID == currentLevel.id, out int saveIndex))
                Saves[saveIndex] = currentLevel.saveData;
            else
                Saves.Add(currentLevel.saveData);

            SaveProgress();
        }

        /// <summary>
        /// Saves all story level player data.
        /// </summary>
        public void SaveProgress()
        {
            storySavesJSON["lvl"] = new JSONArray();
            int num = 0;
            for (int i = 0; i < Saves.Count; i++)
            {
                var save = Saves[i];
                if (!save.ShouldSerialize)
                    continue;

                storySavesJSON["lvl"][num] = save.ToJSON();
                num++;
            }

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
            {
                storySavesJSON["saves"].Remove(name);
                return;
            }
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

        /// <summary>
        /// Removes the value from the story save.
        /// </summary>
        /// <param name="name">Name of the value to remove.</param>
        public void Remove(string name)
        {
            storySavesJSON["saves"].Remove(name);
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

        #endregion
    }
}
