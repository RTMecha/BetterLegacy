using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents the player model data.
    /// </summary>
    public class PlayersData : Exists
    {
        public PlayersData(bool isGlobal = false)
        {
            this.isGlobal = isGlobal;
            AssignDefaultModels();
        }

        /// <summary>
        /// If the <see cref="Current"/> should be used instead of the <see cref="Main"/>.
        /// </summary>
        public static bool LoadLocal => CoreHelper.InEditor || (GameData.IsValid && !GameData.Current.beatmapData.levelData.allowCustomPlayerModels) || !PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value;

        /// <summary>
        /// If custom models should be used instead of the loaded ones.
        /// </summary>
        public static bool AllowCustomModels => GameData.IsValid && GameData.Current.beatmapData.levelData.allowCustomPlayerModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value;

        /// <summary>
        /// The main <see cref="PlayersData"/> to use for the level.
        /// </summary>
        public static PlayersData Main => LoadLocal && IsValid ? Current : Global;

        public static bool IsValid => Current;
        public static PlayersData Current { get; set; }

        static PlayersData global;
        public static PlayersData Global
        {
            get
            {
                if (!global)
                    global = new PlayersData(true);
                return global;
            }
            set => global = value;
        }

        public bool isGlobal;

        /// <summary>
        /// All player models that is currently loaded.
        /// </summary>
        public Dictionary<string, PlayerModel> playerModels = new Dictionary<string, PlayerModel>();

        /// <summary>
        /// Player model ID indexer.
        /// </summary>
        public List<string> playerModelsIndex = new List<string> { "0", "0", "0", "0", };

        /// <summary>
        /// How players above the normal amount are treated.
        /// </summary>
        public MaxBehavior maxBehavior;

        List<PlayerModel> cachedPlayerModels = new List<PlayerModel>()
        {
            PlayerModel.DefaultPlayer,
            PlayerModel.DefaultPlayer,
            PlayerModel.DefaultPlayer,
            PlayerModel.DefaultPlayer,
        };

        #region Methods

        /// <summary>
        /// Creates a copy of a <see cref="PlayersData"/>.
        /// </summary>
        /// <param name="orig">Original to copy.</param>
        /// <returns>Returns a copied <see cref="PlayersData"/>.</returns>
        public static PlayersData DeepCopy(PlayersData orig) => new PlayersData
        {
            isGlobal = orig.isGlobal,
            playerModels = orig.playerModels,
            playerModelsIndex = orig.playerModelsIndex,
        };

        /// <summary>
        /// Loads the global player models.
        /// </summary>
        public static void Load(string filePath)
        {
            if (RTFile.FileExists(filePath))
                Current = Parse(JSON.Parse(RTFile.ReadFromFile(filePath)));

            var global = Global;

            global.AssignDefaultModels();

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

                if (!model.IsDefault)
                    global.playerModels[id] = model;
            }
        }

        /// <summary>
        /// Saves the global player models.
        /// </summary>
        /// <returns>Returns true if the models saved correctly.</returns>
        public static bool Save() => Global.SaveLocal() && (!Current || Current.SaveLocal());

        /// <summary>
        /// Saves the player data locally.
        /// </summary>
        /// <returns>Returns true if the models saved correctly.</returns>
        public bool SaveLocal()
        {
            bool success = true;

            foreach (var model in playerModels.Values)
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
            playerModels.Clear();
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
            jn["receive_type"] = ((int)maxBehavior);
            for (int i = 0; i < playerModelsIndex.Count; i++)
                jn["indexes"][i] = playerModelsIndex[i];
            int index = 0;
            foreach (var keyValuePair in playerModels)
            {
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
        public PlayerModel GetPlayerModel(int index) => maxBehavior == MaxBehavior.Default ? PlayerModel.DefaultPlayer : cachedPlayerModels[GetMaxIndex(index)];

        /// <summary>
        /// Gets a players' maxed index.
        /// </summary>
        /// <param name="index">Index of the player.</param>
        /// <returns>Returns maxed index.</returns>
        public int GetMaxIndex(int index) => maxBehavior switch
        {
            MaxBehavior.Loop => index % cachedPlayerModels.Count,
            MaxBehavior.Clamp => Mathf.Clamp(index, 0, cachedPlayerModels.Count - 1),
            MaxBehavior.First => index >= 0 && index < cachedPlayerModels.Count ? index : 0,
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
            MaxBehavior.Loop => index & count,
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

            if (isGlobal)
            {
                PlayerManager.PlayerIndexes[Mathf.Clamp(index, 0, PlayerManager.PlayerIndexes.Count - 1)].Value = id;
                return;
            }

            while (index >= playerModelsIndex.Count)
                playerModelsIndex.Add("0");
            playerModelsIndex[index] = id;
            while (index >= cachedPlayerModels.Count)
                cachedPlayerModels.Add(PlayerModel.DefaultPlayer);
            if (playerModels.TryGetValue(id, out PlayerModel playerModel))
                cachedPlayerModels[index] = playerModel;
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
