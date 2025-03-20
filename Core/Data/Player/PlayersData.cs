using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents the player model data.
    /// </summary>
    public class PlayersData : Exists
    {
        public PlayersData()
        {
            AssignDefaultModels();
        }

        public static bool AllowCustomModels => GameData.Current && GameData.Current.data && GameData.Current.data.level.allowCustomPlayerModels;

        /// <summary>
        /// If custom models should be used instead of the loaded ones.
        /// </summary>
        public static bool UseGlobal => CoreHelper.InEditor || AllowCustomModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value;

        public static bool IsValid => Current;
        public static PlayersData Current { get; set; }

        public static Dictionary<string, PlayerModel> externalPlayerModels = new Dictionary<string, PlayerModel>();

        /// <summary>
        /// All player models that is currently loaded.
        /// </summary>
        public Dictionary<string, PlayerModel> playerModels = new Dictionary<string, PlayerModel>();

        public static Dictionary<string, PlayerModel> CurrentModelDictionary => UseGlobal ? externalPlayerModels : Current.playerModels;

        /// <summary>
        /// Player model ID indexer.
        /// </summary>
        public List<string> playerModelsIndex = new List<string> { "0", "0", "0", "0", };

        /// <summary>
        /// How players above the normal amount are treated.
        /// </summary>
        public MaxBehavior maxBehavior;

        #region Methods

        public static PlayerModel GetPlayerModel(string id) =>
            UseGlobal && externalPlayerModels.TryGetValue(id, out PlayerModel externalPlayerModel) ? externalPlayerModel :
            IsValid && Current.playerModels.TryGetValue(id, out PlayerModel playerModel) ? playerModel : PlayerModel.DefaultPlayer;

        /// <summary>
        /// Creates a copy of a <see cref="PlayersData"/>.
        /// </summary>
        /// <param name="orig">Original to copy.</param>
        /// <returns>Returns a copied <see cref="PlayersData"/>.</returns>
        public static PlayersData DeepCopy(PlayersData orig) => new PlayersData
        {
            playerModels = orig.playerModels,
            playerModelsIndex = orig.playerModelsIndex,
            maxBehavior = orig.maxBehavior,
        };

        /// <summary>
        /// Loads the global player models.
        /// </summary>
        public static void LoadJSON(JSONNode jn)
        {
            var exists = !jn.IsNull;
            Current = exists ? Parse(jn) : new PlayersData();

            var currentLevel = CoreHelper.CurrentLevel;
            if (!exists && currentLevel)
                for (int i = 0; i < Current.playerModelsIndex.Count; i++)
                    Current.playerModelsIndex[i] = currentLevel.IsVG ? PlayerModel.DEV_ID : PlayerModel.DEFAULT_ID;

            externalPlayerModels.Clear();
            foreach (var playerModel in PlayerModel.DefaultModels)
                externalPlayerModels[playerModel.basePart.id] = playerModel;

            var fullPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, PlayerManager.PLAYERS_PATH);
            RTFile.CreateDirectory(fullPath);

            var files = Directory.GetFiles(fullPath, FileFormat.LSPL.ToPattern());

            if (files.Length < 1)
                return;

            for (int i = 0; i < files.Length; i++)
            {
                var file = RTFile.ReplaceSlash(files[i]);
                var model = PlayerModel.Parse(JSON.Parse(RTFile.ReadFromFile(file)));
                var id = model.basePart.id;

                if (PlayerModel.DefaultModels.Has(x => x.basePart.id == id))
                    continue;

                externalPlayerModels[id] = model;

                if (IsValid && CoreHelper.InEditor)
                    Current.playerModels[id] = model;
            }
        }

        /// <summary>
        /// Loads the global player models.
        /// </summary>
        public static void Load(string filePath) => LoadJSON(RTFile.FileExists(filePath) ? JSON.Parse(RTFile.ReadFromFile(filePath)) : new JSONNull());

        /// <summary>
        /// Saves the global player models.
        /// </summary>
        /// <returns>Returns true if the models saved correctly.</returns>
        public static bool Save()
        {
            bool success = true;

            foreach (var model in externalPlayerModels.Values)
            {
                if (model.IsDefault)
                    continue;

                try
                {
                    RTFile.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, PlayerManager.PLAYERS_PATH, $"{RTFile.FormatLegacyFileName(model.basePart.name)}{FileFormat.LSPL.Dot()}"), model.ToJSON().ToString(3));
                }
                catch (Exception ex)
                {
                    success = false;
                    CoreHelper.LogException(ex);
                }
            }

            return success;
        }

        void AssignDefaultModels()
        {
            foreach (var playerModel in PlayerModel.DefaultModels)
                playerModels[playerModel.basePart.id] = playerModel;
        }

        /// <summary>
        /// Parses a player model data from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="PlayersData"/>.</returns>
        public static PlayersData Parse(JSONNode jn)
        {
            var playerModelData = new PlayersData();
            playerModelData.maxBehavior = (MaxBehavior)jn["max"].AsInt;
            for (int i = 0; i < jn["models"].Count; i++)
            {
                var playerModel = PlayerModel.Parse(jn["models"][i]);
                playerModelData.playerModels[playerModel.basePart.id] = playerModel;
            }
            playerModelData.AssignDefaultModels();
            for (int i = 0; i < jn["indexes"].Count; i++)
                playerModelData.SetPlayerModel(i, jn["indexes"][i]);
            return playerModelData;
        }

        /// <summary>
        /// Writes the <see cref="PlayersData"/> to a JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="PlayersData"/>.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            if (maxBehavior != MaxBehavior.Loop)
                jn["max"] = (int)maxBehavior;
            for (int i = 0; i < playerModelsIndex.Count; i++)
                jn["indexes"][i] = playerModelsIndex[i];
            int index = 0;
            foreach (var keyValuePair in playerModels)
            {
                if (keyValuePair.Value.IsDefault)
                    continue;
                
                jn["models"][index] = keyValuePair.Value.ToJSON();
                index++;
            }
            return jn;
        }

        /// <summary>
        /// Gets the player model at the index.
        /// </summary>
        /// <param name="index">Index of the player model.</param>
        /// <returns>Returns a player model from the dictionary.</returns>
        public PlayerModel GetPlayerModel(int index) => maxBehavior == MaxBehavior.Default && index >= playerModelsIndex.Count ? PlayerModel.DefaultPlayer : CurrentModelDictionary.TryGetValue(playerModelsIndex[GetMaxIndex(index)], out PlayerModel playerModel) ? playerModel : PlayerModel.DefaultPlayer;

        /// <summary>
        /// Gets a players' maxed index.
        /// </summary>
        /// <param name="index">Index of the player.</param>
        /// <returns>Returns maxed index.</returns>
        public int GetMaxIndex(int index) => maxBehavior switch
        {
            MaxBehavior.Loop => index % playerModelsIndex.Count,
            MaxBehavior.Clamp => Mathf.Clamp(index, 0, playerModelsIndex.Count - 1),
            MaxBehavior.First => index >= 0 && index < playerModelsIndex.Count ? index : 0,
            _ => index,
        };

        /// <summary>
        /// Gets a players' maxed index.
        /// </summary>
        /// <param name="index">Index of the player.</param>
        /// <param name="count">Total player count.</param>
        /// <returns>Returns maxed index.</returns>
        public int GetMaxIndex(int index, int count) => maxBehavior switch
        {
            MaxBehavior.Loop => index % count,
            MaxBehavior.Clamp => Mathf.Clamp(index, 0, count - 1),
            _ => index >= 0 && index < count ? index : 0,
        };

        /// <summary>
        /// Creates a new player model.
        /// </summary>
        public PlayerModel CreateNewPlayerModel()
        {
            var model = new PlayerModel();
            model.basePart.name = "New Model";
            model.basePart.id = LSText.randomNumString(16);

            externalPlayerModels[model.basePart.id] = model;
            playerModels[model.basePart.id] = model;
            return model;
        }

        /// <summary>
        /// Duplicates a player model.
        /// </summary>
        /// <param name="id">Model ID to duplicate.</param>
        public PlayerModel DuplicatePlayerModel(string id)
        {
            if (playerModels.TryGetValue(id, out PlayerModel orig))
            {
                var model = PlayerModel.DeepCopy(orig);
                model.basePart.name += " Clone";
                model.basePart.id = LSText.randomNumString(16);

                playerModels[model.basePart.id] = model;
                return model;
            }
            return null;
        }

        /// <summary>
        /// Sets the player model by an ID and index and caches it.
        /// </summary>
        /// <param name="index">Index of the player.</param>
        /// <param name="id">ID of the player model.</param>
        public void SetPlayerModel(int index, string id)
        {
            if (!PlayerModel.DefaultModels.Has(x => x.basePart.id == id) && (!MetaData.IsValid || MetaData.Current.song == null || MetaData.Current.song.difficulty != 6))
                AchievementManager.inst.UnlockAchievement("costume_party");

            while (index >= playerModelsIndex.Count)
                playerModelsIndex.Add("0");
            playerModelsIndex[index] = id;
        }

        #endregion
    }

    public enum MaxBehavior
    {
        /// <summary>
        /// Loops the player models over.
        /// </summary>
        Loop,
        /// <summary>
        /// Stops at the last player model.
        /// </summary>
        Clamp,
        /// <summary>
        /// Defaults to the first player model.
        /// </summary>
        First,
        /// <summary>
        /// Defaults to the default player model.
        /// </summary>
        Default,
    }
}
