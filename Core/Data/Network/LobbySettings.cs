using SimpleJSON;

using BetterLegacy.Configs;

namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Represents settings for an online lobby.
    /// </summary>
    public class LobbySettings : PAObject<LobbySettings>, IPacket, IFile
    {
        #region Values

        public const string MAIN_FILE_NAME = "lobby_settings.lss";
        public const int MIN_PLAYER_COUNT = 2;
        public const int MAX_PLAYER_COUNT = 16;

        public FileFormat FileFormat => FileFormat.LSS;

        /// <summary>
        /// Name of the lobby.
        /// </summary>
        public string Name { get; set; } = CoreConfig.Instance.DisplayName.Value;

        int playerCount = MAX_PLAYER_COUNT;
        /// <summary>
        /// Max amount of players that can join the lobby.
        /// </summary>
        public int PlayerCount
        {
            get => RTMath.Clamp(playerCount, MIN_PLAYER_COUNT, MAX_PLAYER_COUNT);
            set => playerCount = RTMath.Clamp(value, MIN_PLAYER_COUNT, MAX_PLAYER_COUNT);
        }

        /// <summary>
        /// Visibility of the lobby.
        /// </summary>
        public LobbyVisibility Visibility { get; set; } = LobbyVisibility.Public;

        /// <summary>
        /// Current state of the lobby.
        /// </summary>
        public LobbyState State { get; set; } = LobbyState.Joinable;

        /// <summary>
        /// Channel ID of the lobby.
        /// </summary>
        public string Channel { get; set; } = string.Empty;

        #region Editor

        /// <summary>
        /// If users can edit the current level when the host is in the editor.
        /// </summary>
        public bool CanEdit { get; set; } = true;

        /// <summary>
        /// If users can import their own prefabs into the current level when the host is in the editor.
        /// </summary>
        public bool CanImportPrefabs { get; set; }

        #endregion

        #endregion

        #region Functions

        public string GetFileName() => MAIN_FILE_NAME;

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.EndsWith(FileFormat.Dot()))
                path = path += FileFormat.Dot();

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            ReadJSON(JSON.Parse(file));
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = ToJSON();
            RTFile.WriteToFile(path, jn.ToString());
        }

        public override void CopyData(LobbySettings orig, bool newID = true)
        {
            Name = orig.Name;
            PlayerCount = orig.PlayerCount;
            Visibility = orig.Visibility;
            State = orig.State;
            Channel = orig.Channel;
            CanEdit = orig.CanEdit;
            CanImportPrefabs = orig.CanImportPrefabs;
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            PlayerCount = jn["player_count"].AsInt;
            Visibility = Parser.TryParse(jn["visibility"], true, LobbyVisibility.Public);
            State = Parser.TryParse(jn["state"], true, LobbyState.Joinable);
            Channel = jn["channel"];
            CanEdit = jn["can_edit"].AsBool;
            CanImportPrefabs = jn["can_import_prefabs"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name ?? string.Empty;
            jn["player_count"] = PlayerCount;
            jn["visibility"] = Visibility.ToString();
            jn["state"] = State.ToString();
            jn["channel"] = Channel ?? string.Empty;
            jn["can_edit"] = CanEdit;
            jn["can_import_prefabs"] = CanImportPrefabs;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            Name = reader.ReadString();
            PlayerCount = reader.ReadInt32();
            Visibility = (LobbyVisibility)reader.ReadByte();
            State = (LobbyState)reader.ReadByte();
            Channel = reader.ReadString();
            CanEdit = reader.ReadBoolean();
            CanImportPrefabs = reader.ReadBoolean();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(Name);
            writer.Write(PlayerCount);
            writer.Write((byte)Visibility);
            writer.Write((byte)State);
            writer.Write(Channel);
            writer.Write(CanEdit);
            writer.Write(CanImportPrefabs);
        }

        #endregion
    }
}
