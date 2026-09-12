using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents the player model data.
    /// </summary>
    public class PlayersData : PAObject<PlayersData>, IPacket
    {
        #region Constructors

        public PlayersData() => AssignDefaultModels();

        #endregion

        #region Values

        /// <summary>
        /// If custom player models are allowed.
        /// </summary>
        public static bool AllowCustomModels => GameData.Current && GameData.Current.data && GameData.Current.data.level.allowCustomPlayerModels;

        /// <summary>
        /// If custom models should be used instead of the loaded ones.
        /// </summary>
        public static bool UseGlobal => ProjectArrhythmia.State.InEditor || AllowCustomModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value;

        /// <summary>
        /// The current <see cref="PlayersData"/>.
        /// </summary>
        public static PlayersData Current { get; set; }

        /// <summary>
        /// Player models from the players folder.
        /// </summary>
        public static List<PlayerModel> externalPlayerModels = new List<PlayerModel>();

        /// <summary>
        /// All player models that is currently loaded.
        /// </summary>
        public List<PlayerModel> playerModels = new List<PlayerModel>();

        /// <summary>
        /// Player model ID indexer.
        /// </summary>
        public List<string> playerModelsIndex = new List<string> { "0", "0", "0", "0", };

        /// <summary>
        /// How players above the normal amount are treated.
        /// </summary>
        public MaxBehavior maxBehavior;
        
        /// <summary>
        /// List of player properties.
        /// </summary>
        public List<PlayerProperties> playersProperties = new List<PlayerProperties>
        {
            new PlayerProperties(),
            new PlayerProperties(),
            new PlayerProperties(),
            new PlayerProperties(),
        };

        #endregion

        #region Functions

        public override void CopyData(PlayersData orig, bool newID = true)
        {
            maxBehavior = orig.maxBehavior;
            playerModels.Clear();
            foreach (var playerModel in orig.playerModels)
                OverwritePlayerModel(playerModel.Copy());
            AssignDefaultModels();
            playerModelsIndex = new List<string>(playerModelsIndex);
            playersProperties = new List<PlayerProperties>(playersProperties.Select(x => x.Copy()));
        }

        public override void ReadJSON(JSONNode jn)
        {
            maxBehavior = (MaxBehavior)jn["max"].AsInt;
            for (int i = 0; i < jn["models"].Count; i++)
                OverwritePlayerModel(PlayerModel.Parse(jn["models"][i]));
            AssignDefaultModels();
            for (int i = 0; i < jn["indexes"].Count; i++)
                SetPlayerModel(i, jn["indexes"][i]);
            for (int i = 0; i < jn["controls"].Count; i++)
            {
                var control = PlayerProperties.Parse(jn["controls"][i]);
                if (i < playersProperties.Count)
                    playersProperties[i] = control;
                else
                    playersProperties.Add(control);
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            if (maxBehavior != MaxBehavior.Loop)
                jn["max"] = (int)maxBehavior;
            for (int i = 0; i < playerModelsIndex.Count; i++)
                jn["indexes"][i] = playerModelsIndex[i];
            for (int i = 0; i < playersProperties.Count; i++)
                jn["controls"][i] = playersProperties[i].ToJSON();

            int index = 0;
            foreach (var playerModel in playerModels)
            {
                if (playerModel.IsDefault)
                    continue;

                jn["models"][index] = playerModel.ToJSON();
                index++;
            }
            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            maxBehavior = (MaxBehavior)reader.ReadByte();
            playerModelsIndex = reader.ReadList(reader.ReadString);
            Packet.ReadPacketList(playersProperties, reader);
            Packet.ReadPacketList(playerModels, reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write((byte)maxBehavior);
            writer.Write(playerModelsIndex, writer.Write);
            Packet.WritePacketList(playersProperties, writer);
            Packet.WritePacketList(playerModels, writer);
        }

        /// <summary>
        /// Loads the global player models.
        /// </summary>
        public static void LoadJSON(JSONNode jn)
        {
            var exists = !jn.IsNull;
            Current = exists ? Parse(jn) : new PlayersData();

            var currentLevel = CoreHelper.CurrentLevel;
            if (!exists && currentLevel)
            {
                for (int i = 0; i < Current.playerModelsIndex.Count; i++)
                    Current.playerModelsIndex[i] = currentLevel.IsVG ? PlayerModel.DEV_ID : PlayerModel.DEFAULT_ID;
                for (int i = 0; i < Current.playersProperties.Count; i++)
                {
                    var playerProperties = Current.playersProperties[i];
                    playerProperties.moveSpeed = 22f;
                    playerProperties.boostSpeed = 80f;
                    playerProperties.boostCooldown = 0f;
                    playerProperties.minBoostTime = 0.05f;
                    playerProperties.maxBoostTime = 0.25f;
                    playerProperties.hitCooldown = 2f;
                }
            }

            externalPlayerModels.Clear();
            foreach (var playerModel in PlayerModel.DefaultModels)
                OverwriteExternalPlayerModel(playerModel);

            var fullPath = ProjectArrhythmia.State.InEditor ? RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlayersPath) : RTFile.CombinePaths(RTFile.ApplicationDirectory, PlayerManager.PLAYERS_PATH);
            RTFile.CreateDirectory(fullPath);

            var files = Directory.GetFiles(fullPath, FileFormat.LSPL.ToPattern());

            if (files.Length < 1)
                return;

            for (int i = 0; i < files.Length; i++)
            {
                var file = RTFile.ReplaceSlash(files[i]);
                var playerModel = PlayerModel.Parse(JSON.Parse(RTFile.ReadFromFile(file)));
                playerModel.path = file;
                var id = playerModel.ID;

                if (PlayerModel.DefaultModels.Has(x => x.ID == id))
                    continue;

                OverwriteExternalPlayerModel(playerModel);
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

            foreach (var model in externalPlayerModels)
            {
                if (model.IsDefault)
                    continue;

                try
                {
                    var path = !string.IsNullOrEmpty(model.path) ? model.path : RTFile.CombinePaths(RTFile.ApplicationDirectory, PlayerManager.PLAYERS_PATH, $"{RTFile.FormatLegacyFileName(model.Name)}{FileFormat.LSPL.Dot()}");
                    if (string.IsNullOrEmpty(model.path))
                        model.path = path;
                    RTFile.WriteToFile(path, model.ToJSON().ToString(3));
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
                OverwritePlayerModel(playerModel);
        }

        /// <summary>
        /// Gets the player model for a player to use.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>Returns a player model from the dictionary.</returns>
        public PlayerModel GetPlayerModel(PAPlayer player)
        {
            if (UseGlobal)
                return externalPlayerModels.TryFind(x => x.ID == (PlayerManager.inst.GetPlayerSettings(player.localIndex)?.playerModelID ?? string.Empty), out PlayerModel customModel) ? customModel : PlayerModel.DefaultPlayer;
            if (maxBehavior == MaxBehavior.Default && player.index >= playerModelsIndex.Count)
                return PlayerModel.DefaultPlayer;
            return playerModels.TryFind(x => x.ID == playerModelsIndex[GetMaxIndex(player.index)], out PlayerModel playerModel) ? playerModel : PlayerModel.DefaultPlayer;
        }

        /// <summary>
        /// Gets the player model at the index.
        /// </summary>
        /// <param name="index">Index of the player model.</param>
        /// <returns>Returns a player model from the dictionary.</returns>
        public PlayerModel GetPlayerModel(int index)
        {
            if (maxBehavior == MaxBehavior.Default && index >= playerModelsIndex.Count)
                return PlayerModel.DefaultPlayer;
            return playerModels.TryFind(x => x.ID == playerModelsIndex[GetMaxIndex(index)], out PlayerModel playerModel) ? playerModel : PlayerModel.DefaultPlayer;
        }

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
        /// Sets the player model by an ID and index and caches it.
        /// </summary>
        /// <param name="index">Index of the player.</param>
        /// <param name="id">ID of the player model.</param>
        public void SetPlayerModel(int index, string id)
        {
            if (!PlayerModel.DefaultModels.Has(x => x.ID == id) && (!MetaData.Current || MetaData.Current.song == null || MetaData.Current.song.difficulty != 6))
                AchievementManager.inst.UnlockAchievement("costume_party");

            while (index >= playerModelsIndex.Count)
                playerModelsIndex.Add("0");
            playerModelsIndex[index] = id;
        }

        public void OverwritePlayerModel(PlayerModel playerModel)
        {
            if (!playerModel)
                return;
            if (playerModels.TryFindIndex(x => x.ID == playerModel.ID, out int index))
                playerModels[index] = playerModel;
            else
                playerModels.Add(playerModel);
        }

        public static void OverwriteExternalPlayerModel(PlayerModel playerModel)
        {
            if (!playerModel)
                return;
            if (externalPlayerModels.TryFindIndex(x => x.ID == playerModel.ID, out int index))
                externalPlayerModels[index] = playerModel;
            else
                externalPlayerModels.Add(playerModel);
        }

        #endregion
    }

    /// <summary>
    /// How players above the normal amount are treated.
    /// </summary>
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
