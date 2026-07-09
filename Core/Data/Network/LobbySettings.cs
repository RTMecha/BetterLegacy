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
        public const int MAX_PLAYER_COUNT = 16; // maybe think about increasing the cap

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

        #region Game

        /// <summary>
        /// If users can view levels and request them to be opened.
        /// </summary>
        public bool CanViewLevels { get; set; } = true;

        #endregion

        #region Editor

        /// <summary>
        /// If users can edit the current level when the host is in the editor.
        /// </summary>
        public bool CanEdit { get; set; } = true;

        /// <summary>
        /// If users can view editor levels and request them to be opened.
        /// </summary>
        public bool CanViewEditorLevels { get; set; } = true;

        /// <summary>
        /// If users can import their own prefabs into the current level when the host is in the editor.
        /// </summary>
        public bool CanImportPrefabs { get; set; }

        /// <summary>
        /// If users can import their own themes into the current level when the host is in the editor.
        /// </summary>
        public bool CanImportThemes { get; set; }

        /// <summary>
        /// If users can create and edit objects (including all timeline object types).
        /// </summary>
        public bool CanEditObjects { get; set; } = true;

        /// <summary>
        /// If users can create and edit markers.
        /// </summary>
        public bool CanEditMarkers { get; set; } = true;

        /// <summary>
        /// If users can draw annotations.
        /// </summary>
        public bool CanDrawAnnotations { get; set; } = true;

        /// <summary>
        /// If users can create and edit event keyframes.
        /// </summary>
        public bool CanEditEvents { get; set; } = true;

        /// <summary>
        /// If users can create and edit pinned editor layers.
        /// </summary>
        public bool CanEditPinnedEditorLayers { get; set; }

        /// <summary>
        /// If users can edit the levels' metadata.
        /// </summary>
        public bool CanEditMetaData { get; set; }

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
            CanViewLevels = orig.CanViewLevels;
            CanEdit = orig.CanEdit;
            CanViewEditorLevels = orig.CanViewEditorLevels;
            CanImportPrefabs = orig.CanImportPrefabs;
            CanImportThemes = orig.CanImportThemes;
            CanEditObjects = orig.CanEditObjects;
            CanEditMarkers = orig.CanEditMarkers;
            CanDrawAnnotations = orig.CanDrawAnnotations;
            CanEditEvents = orig.CanEditEvents;
            CanEditMetaData = orig.CanEditMetaData;
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            PlayerCount = jn["player_count"].AsInt;
            Visibility = Parser.TryParse(jn["visibility"], true, LobbyVisibility.Public);
            State = Parser.TryParse(jn["state"], true, LobbyState.Joinable);
            Channel = jn["channel"];
            CanViewLevels = jn["can_view_levels"].AsBool;
            CanEdit = jn["can_edit"].AsBool;
            CanViewEditorLevels = jn["can_view_editor_levels"].AsBool;
            CanImportPrefabs = jn["can_import_prefabs"].AsBool;
            CanImportThemes = jn["can_import_themes"].AsBool;
            CanEditObjects = jn["can_edit_objects"].AsBool;
            CanEditMarkers = jn["can_edit_markers"].AsBool;
            CanDrawAnnotations = jn["can_draw_annotations"].AsBool;
            CanEditEvents = jn["can_edit_events"].AsBool;
            CanEditMetaData = jn["can_edit_metadata"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name ?? string.Empty;
            jn["player_count"] = PlayerCount;
            jn["visibility"] = Visibility.ToString();
            jn["state"] = State.ToString();
            jn["channel"] = Channel ?? string.Empty;
            jn["can_view_levels"] = CanViewLevels;
            jn["can_edit"] = CanEdit;
            jn["can_view_editor_levels"] = CanViewEditorLevels;
            jn["can_import_prefabs"] = CanImportPrefabs;
            jn["can_import_themes"] = CanImportThemes;
            jn["can_edit_objects"] = CanEditObjects;
            jn["can_edit_markers"] = CanEditMarkers;
            jn["can_draw_annotations"] = CanDrawAnnotations;
            jn["can_edit_events"] = CanEditEvents;
            jn["can_edit_metadata"] = CanEditMetaData;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            Name = reader.ReadString();
            PlayerCount = reader.ReadInt32();
            Visibility = (LobbyVisibility)reader.ReadByte();
            State = (LobbyState)reader.ReadByte();
            Channel = reader.ReadString();
            CanViewLevels = reader.ReadBoolean();
            CanEdit = reader.ReadBoolean();
            CanViewEditorLevels = reader.ReadBoolean();
            CanImportPrefabs = reader.ReadBoolean();
            CanImportThemes = reader.ReadBoolean();
            CanEditObjects = reader.ReadBoolean();
            CanEditMarkers = reader.ReadBoolean();
            CanDrawAnnotations = reader.ReadBoolean();
            CanEditEvents = reader.ReadBoolean();
            CanEditMetaData = reader.ReadBoolean();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(Name);
            writer.Write(PlayerCount);
            writer.Write((byte)Visibility);
            writer.Write((byte)State);
            writer.Write(Channel);
            writer.Write(CanViewLevels);
            writer.Write(CanEdit);
            writer.Write(CanViewEditorLevels);
            writer.Write(CanImportPrefabs);
            writer.Write(CanImportThemes);
            writer.Write(CanEditObjects);
            writer.Write(CanEditMarkers);
            writer.Write(CanDrawAnnotations);
            writer.Write(CanEditEvents);
            writer.Write(CanEditMetaData);
        }

        #endregion
    }
}
