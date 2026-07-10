using System;

using UnityEngine;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Story;

namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Represents a function that can run on the network.
    /// </summary>
    public class NetworkFunction : Exists
    {
        #region Constructors

        public NetworkFunction(Side side, int id, Action<NetworkReader> action)
        {
            this.side = side;
            this.id = id;
            this.action = action;
        }

        public NetworkFunction(Network.Side side, int id, Action<NetworkReader> action)
        {
            this.side = (Side)side;
            this.id = id;
            this.action = action;
        }

        public NetworkFunction(int id, Action<NetworkReader> action) : this(Side.Multi, id, action) { }

        public NetworkFunction(Side side, int id, int parameterCount, Action<NetworkReader> action)
        {
            this.side = side;
            this.id = id;
            this.parameterCount = parameterCount;
            this.action = action;
        }

        public NetworkFunction(Network.Side side, int id, int parameterCount, Action<NetworkReader> action)
        {
            this.side = (Side)side;
            this.id = id;
            this.parameterCount = parameterCount;
            this.action = action;
        }

        public NetworkFunction(int id, int parameterCount, Action<NetworkReader> action) : this(Side.Multi, id, parameterCount, action) { }

        #endregion

        #region Values

        #region Constants

        #region Core

        public const int LOG_CLIENT = 842754988;
        public const int LOG_SERVER = 53295835;
        public const int LOG_MULTI = 43292487;

        public const int SET_CLIENT_UNLOADED = 5285835;
        public const int SET_LOADED = 26327436;

        public const int SEND_CHUNK_DATA = 326243667;

        public const int REQUEST_HOST = 10;

        public const int KEY_PRESS_DOWN = 3437654;

        public const int KEY_PRESS = 4326347;

        public const int KEY_PRESS_UP = 86325;

        public const int MOUSE_BUTTON_DOWN = 362626;

        public const int MOUSE_BUTTON = 97587;

        public const int MOUSE_BUTTON_UP = 462889;

        public const int CONTROL_PRESS_DOWN = 367246;

        public const int CONTROL_PRESS = 753736;

        public const int CONTROL_PRESS_UP = 9630986;

        #endregion

        #region Player

        public const int SEND_CLIENT_PLAYER_DATA = 5932573;
        public const int SEND_SERVER_PLAYER_DATA = 74362567;
        public const int SEND_MULTI_PLAYER_DATA = 476256437;

        public const int UPDATE_PLAYER_DATA = 83553876;

        public const int SET_PLAYER_POSITION = 326462532;
        public const int SET_PLAYER_HEALTH = 357437643;

        public const int DESTROY_PLAYERS = 532264743;
        public const int RESPAWN_PLAYERS = 857453345;
        public const int RESPAWN_PLAYERS_POS = 236326525;
        public const int SPAWN_PLAYERS_CHECKPOINT = 359827539;
        public const int SPAWN_PLAYERS_POS = 532264288;
        public const int PLAYER_BOOST = 23582865;
        public const int PLAYER_BOOST_STOP = 2149812;
        public const int PLAYER_HEAL = 236236743;
        public const int PLAYER_HIT = 643698467;
        public const int PLAYER_KILL = 593762876;
        public const int PLAYER_JUMP = 8538582;
        public const int PLAYER_RESET_HEALTH = 5396969;

        #endregion

        #region Interface

        public const int PAUSE_GAME = 52375731;
        public const int UNPAUSE_GAME = 2542562;

        public const int INIT_ARCADE_INTERFACE = 4562345;

        public const int CLEAR_ARCADE_LEVELS = 2513561;
        public const int SEND_ARCADE_LEVEL = 53857832;
        public const int SEND_ARCADE_LEVEL_COLLECTION = 75323456;
        public const int SEND_STEAM_LEVEL = 236567643;

        public const int SORT_ARCADE_LEVELS = 246346535;
        public const int SORT_STEAM_LEVELS = 356243467;
        public const int SORT_STEAM_WORKSHOP_LEVELS = 9855322;
        public const int SORT_ONLINE_LEVELS = 34773463;
        public const int SORT_ONLINE_LEVEL_COLLECTIONS = 4535323;

        public const int INIT_PLAY_LEVEL_INTERFACE = 63262362;
        public const int INIT_LOAD_LEVELS_INTERFACE = 26523676;

        #endregion

        #region Game

        public const int SET_CLIENT_SCENE = 683429582;

        public const int SET_CLIENT_SEED = 64674378;
        public const int SET_CLIENT_GAME_DATA = 9432119;
        public const int SET_CLIENT_META_DATA = 52635853;
        public const int SET_CLIENT_RUNTIME = 2536736;

        public const int SET_CLIENT_AUDIO = 82386538;

        public const int SET_CLIENT_MUSIC_TIME = 352633265;
        public const int SET_SERVER_MUSIC_TIME = 75842873;

        public const int SET_CLIENT_PITCH = 266775874;
        public const int SET_SERVER_PITCH = 378545366;

        public const int REQUEST_MUSIC_TIME = 25643243;

        public const int SYNC_LEVEL = 528958632;

        public const int SET_SERVER_PLAYING_STATE = 493217532;
        public const int SET_CLIENT_PLAYING_STATE = 124853777;

        public const int LOAD_CLIENT_LEVEL = 8637528;

        public const int LOAD_CLIENT_EDITOR_LEVEL = 32583295;

        public const int RESTART_LEVEL = 4285788;

        public const int SEND_HOST_JSON_TRIGGER = 673257345;

        #endregion

        #region Editor

        public const int CLEAR_EDITOR_LEVELS = 7363268;

        public const int SEND_EDITOR_LEVEL = 345324;

        public const int REFRESH_EDITOR_LEVEL_LIST = 53262467;

        #endregion

        #endregion

        /// <summary>
        /// Side the function should only run on.
        /// </summary>
        public Side side;

        /// <summary>
        /// Group of the function.
        /// </summary>
        public Group group;

        /// <summary>
        /// Identification of the function.
        /// </summary>
        public int id;

        /// <summary>
        /// Required amount of parameters.
        /// </summary>
        public int parameterCount;

        /// <summary>
        /// Action to run.
        /// </summary>
        public Action<NetworkReader> action;

        #endregion

        #region Functions

        #region Global

        #region Core

        public static void LogClientSide(string message) => NetworkManager.inst.RunFunction(LOG_CLIENT, new StringParameter(message));

        public static void LogServerSide(string message) => NetworkManager.inst.RunFunction(LOG_SERVER, new StringParameter(message));

        public static void LogMultiSide(string message) => NetworkManager.inst.RunFunction(LOG_MULTI, new StringParameter(message));

        public static void SetClientUnloaded() => NetworkManager.inst.RunFunction(SET_CLIENT_UNLOADED);

        public static void SetClientLoaded() => NetworkManager.inst.RunFunction(SET_LOADED,
            new ULongParameter(RTSteamManager.inst.steamUser.steamID));

        public static void RequestHost(string message) => NetworkManager.inst.RunFunction(REQUEST_HOST, new StringParameter(message));

        public static void KeyPressDown(KeyCode keyCode) => NetworkManager.inst.RunFunction(KEY_PRESS_DOWN, new IntParameter((int)keyCode));
        
        public static void KeyPress(KeyCode keyCode) => NetworkManager.inst.RunFunction(KEY_PRESS, SendType.Unreliable, new IntParameter((int)keyCode));
        
        public static void KeyPressUp(KeyCode keyCode) => NetworkManager.inst.RunFunction(KEY_PRESS_UP, new IntParameter((int)keyCode));

        public static void MouseButtonDown(int button) => NetworkManager.inst.RunFunction(MOUSE_BUTTON_DOWN, new IntParameter(button));
        
        public static void MouseButton(int button) => NetworkManager.inst.RunFunction(MOUSE_BUTTON, SendType.Unreliable, new IntParameter(button));
        
        public static void MouseButtonUp(int button) => NetworkManager.inst.RunFunction(MOUSE_BUTTON_UP, new IntParameter(button));

        public static void ControlPressDown(InControl.InputControlType inputControlType) => NetworkManager.inst.RunFunction(CONTROL_PRESS_DOWN, new IntParameter((int)inputControlType));
        
        public static void ControlPress(InControl.InputControlType inputControlType) => NetworkManager.inst.RunFunction(CONTROL_PRESS, SendType.Unreliable, new IntParameter((int)inputControlType));
        
        public static void ControlPressUp(InControl.InputControlType inputControlType) => NetworkManager.inst.RunFunction(CONTROL_PRESS_UP, new IntParameter((int)inputControlType));

        #endregion

        #region Player

        public static void SetPlayerHealth(string id, int health) => NetworkManager.inst.RunFunction(Group.Player, SET_PLAYER_HEALTH,
                    new ULongParameter(RTSteamManager.inst.steamUser.steamID),
                    new StringParameter(id),
                    new IntParameter(health));

        #endregion

        #region Interface

        public static void PauseGame() => NetworkManager.inst.RunFunction(Group.Interface, PAUSE_GAME);

        public static void UnPauseGame() => NetworkManager.inst.RunFunction(Group.Interface, UNPAUSE_GAME);

        public static void InitArcadeInterface() => NetworkManager.inst.RunFunction(Group.Interface, INIT_ARCADE_INTERFACE, new PacketArray<ArcadeInterface.Tab>(ArcadeInterface.Tab.tabs));

        public static void ClearArcadeLevels() => NetworkManager.inst.RunFunction(Group.Interface, CLEAR_ARCADE_LEVELS);

        public static void SendArcadeLevel(Level.Level level) => NetworkManager.inst.RunFunction(Group.Interface, SEND_ARCADE_LEVEL,
            level,
            new StringParameter(level.FolderName),
            new IntParameter(LoadLevelsInterface.Current?.progress ?? 0));

        public static void SendArcadeLevelCollection(Level.LevelCollection levelCollection) => NetworkManager.inst.RunFunction(Group.Interface, SEND_ARCADE_LEVEL_COLLECTION,
            levelCollection,
            new StringParameter(levelCollection.FolderName),
            new IntParameter(LoadLevelsInterface.Current?.progress ?? 0));

        public static void SendSteamLevel(Level.Level level) => NetworkManager.inst.RunFunction(Group.Interface, SEND_STEAM_LEVEL,
            level,
            new StringParameter(level.FolderName),
            new IntParameter(LoadLevelsInterface.Current?.progress ?? 0));

        public static void SortArcadeLevels(LevelSort levelSort, bool ascend) => NetworkManager.inst.RunFunction(Group.Interface, SORT_ARCADE_LEVELS,
            new IntParameter((int)levelSort),
            new BoolParameter(ascend));
        
        public static void SortSteamLevels(LevelSort levelSort, bool ascend) => NetworkManager.inst.RunFunction(Group.Interface, SORT_STEAM_LEVELS,
            new IntParameter((int)levelSort),
            new BoolParameter(ascend));

        public static void SortSteamWorkshopLevels(QuerySort querySort) => NetworkManager.inst.RunFunction(Group.Interface, SORT_STEAM_WORKSHOP_LEVELS,
            new IntParameter((int)querySort));

        public static void SortOnlineLevels(OnlineLevelSort onlineLevelSort, bool ascend) => NetworkManager.inst.RunFunction(Group.Interface, SORT_ONLINE_LEVELS,
            new IntParameter((int)onlineLevelSort),
            new BoolParameter(ascend));
        
        public static void SortOnlineLevelCollections(OnlineLevelCollectionSort onlineLevelCollectionSort, bool ascend) => NetworkManager.inst.RunFunction(Group.Interface, SORT_ONLINE_LEVEL_COLLECTIONS,
            new IntParameter((int)onlineLevelCollectionSort),
            new BoolParameter(ascend));

        public static void InitPlayLevelInterface(Level.Level level) => NetworkManager.inst.RunFunction(Group.Interface, INIT_PLAY_LEVEL_INTERFACE, level);

        public static void InitLoadLevelsInterface(int levelCount) => NetworkManager.inst.RunFunction(Group.Interface, INIT_LOAD_LEVELS_INTERFACE,
            new IntParameter(levelCount));

        #endregion

        #region Game

        public static void SetClientScene(SceneName scene, bool showLoading, int onLoadFunc) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_SCENE, new ByteParameter((byte)scene), new BoolParameter(showLoading), new IntParameter(onLoadFunc));
        
        public static void SetClientRuntime(RTBeatmap runtime, string id = null) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_RUNTIME, new StringParameter(id), runtime);

        public static void SetClientSeed(string seed, SteamId? steamId) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_SEED, steamId, new StringParameter(seed));

        public static void SetClientMetaData(MetaData metaData, string id = null) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_META_DATA, new StringParameter(id), metaData);

        public static void SetClientGameData(GameData gameData, string id = null) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_GAME_DATA, new StringParameter(id), gameData);

        public static void SetClientAudio(AudioClip audioClip) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_AUDIO, new AudioClipParameter(audioClip));

        public static void SetClientMusicTime(float time) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_MUSIC_TIME, new FloatParameter(time));

        public static void SetServerMusicTime(float time) => NetworkManager.inst.RunFunction(Group.Game, SET_SERVER_MUSIC_TIME, new FloatParameter(time));

        public static void SetClientPitch(float pitch) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_PITCH, new FloatParameter(pitch));

        public static void SetServerPitch(float pitch) => NetworkManager.inst.RunFunction(Group.Game, SET_SERVER_PITCH, new FloatParameter(pitch));

        public static void RequestMusicTime() => NetworkManager.inst.RunFunction(Group.Game, REQUEST_MUSIC_TIME);

        public static void SyncLevelToClients() => NetworkManager.inst.RunFunction(Group.Game, SYNC_LEVEL, sendType: SendType.Unreliable,
                new BoolParameter(ProjectArrhythmia.State.InEditor),
                new FloatParameter(AudioManager.inst.CurrentAudioSource.time));

        public static void SetServerPlayingState(bool state) => NetworkManager.inst.RunFunction(Group.Game, SET_SERVER_PLAYING_STATE, new BoolParameter(state));

        public static void SetClientPlayingState(bool state) => NetworkManager.inst.RunFunction(Group.Game, SET_CLIENT_PLAYING_STATE, new BoolParameter(state));

        public static void LoadClientLevel(Level.Level level, SteamId? steamId = null) => NetworkManager.inst.RunFunction(Group.Game, LOAD_CLIENT_LEVEL, steamId,
            new StringParameter(RandomHelper.CurrentSeed),
            RTBeatmap.Current,
            new BoolParameter(ProjectArrhythmia.State.InStory),
            new IntParameter(StoryManager.inst.currentPlayingChapterIndex),
            new IntParameter(StoryManager.inst.currentPlayingLevelSequenceIndex),
            new ByteArrayParameter(level.ReadZipBytes()),
            new IntParameter(level.IsVG ? 1 : level.isStory ? 2 : 0),
            PlayersData.Current,
            level.saveData ??= new Level.SaveData(level)
            );

        public static void LoadClientEditorLevel(Level.Level level, SteamId? steamId = null) => NetworkManager.inst.RunFunction(Group.Game, LOAD_CLIENT_EDITOR_LEVEL, steamId,
            new StringParameter(EditorManager.inst.currentLoadedLevel),
            new StringParameter(RandomHelper.CurrentSeed),
            new ByteArrayParameter(level.ReadZipBytes()),
            PlayersData.Current
            );

        public static void RestartLevel() => NetworkManager.inst.RunFunction(Group.Game, RESTART_LEVEL);

        public static void SendHostJSONTrigger(string path, bool value) => NetworkManager.inst.RunFunction(Group.Game, SEND_HOST_JSON_TRIGGER,
            new StringParameter(path),
            new BoolParameter(value));

        #endregion

        #region Editor

        public static void ClearEditorLevels() => NetworkManager.inst.RunFunction(Group.Editor, CLEAR_EDITOR_LEVELS);

        public static void SendEditorLevel(LevelPanel levelPanel) => NetworkManager.inst.RunFunction(Group.Editor, SEND_EDITOR_LEVEL, levelPanel);

        public static void RefreshEditorLevelList(string searchTerm) => NetworkManager.inst.RunFunction(Group.Editor, REFRESH_EDITOR_LEVEL_LIST,
            new StringParameter(searchTerm));

        #endregion

        #endregion

        /// <summary>
        /// Runs the function.
        /// </summary>
        public void Run(NetworkReader reader) => action?.Invoke(reader);

        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents the sides of a network and what a network function can run on.
        /// </summary>
        public enum Side
        {
            /// <summary>
            /// Function can run on client-side.
            /// </summary>
            Client,
            /// <summary>
            /// Function can run on server-side.
            /// </summary>
            Server,
            /// <summary>
            /// Function can run on all sides.
            /// </summary>
            Multi,
        }

        /// <summary>
        /// Represents the group of functions.
        /// </summary>
        public enum Group
        {
            /// <summary>
            /// Main network functions.
            /// </summary>
            Core,
            /// <summary>
            /// Network functions that handle the players.
            /// </summary>
            Player,
            /// <summary>
            /// Network functions that handle the interfaces.
            /// </summary>
            Interface,
            /// <summary>
            /// Network functions that handle the game.
            /// </summary>
            Game,
            /// <summary>
            /// Network functions that handle the editor.
            /// </summary>
            Editor,
        }

        public abstract class Parameter : Exists, IPacket
        {
            public abstract void ReadPacket(NetworkReader reader);

            public abstract void WritePacket(NetworkWriter writer);
        }

        public class AudioClipParameter : Parameter
        {
            public AudioClipParameter(AudioClip value) => this.value = value;

            public AudioClip value;

            public override void ReadPacket(NetworkReader reader) => value = Packet.AudioClipFromPacket(reader);

            public override void WritePacket(NetworkWriter writer) => value.WritePacket(writer);
        }

        public class ByteParameter : Parameter
        {
            public ByteParameter(byte value) => this.value = value;

            public byte value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadByte();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class ByteArrayParameter : Parameter
        {
            public ByteArrayParameter(byte[] value) => this.value = value;

            public byte[] value;

            public override void ReadPacket(NetworkReader reader)
            {
                var length = reader.ReadInt32();
                value = reader.ReadBytes(length);
            }

            public override void WritePacket(NetworkWriter writer)
            {
                writer.Write(value.Length);
                writer.Write(value);
            }
        }
        
        public class BoolParameter : Parameter
        {
            public BoolParameter(bool value) => this.value = value;

            public bool value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadBoolean();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }
        
        public class IntParameter : Parameter
        {
            public IntParameter(int value) => this.value = value;

            public int value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadInt32();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class LongParameter : Parameter
        {
            public LongParameter(long value) => this.value = value;

            public long value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadInt64();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class UIntParameter : Parameter
        {
            public UIntParameter(uint value) => this.value = value;

            public uint value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadUInt32();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class ULongParameter : Parameter
        {
            public ULongParameter(ulong value) => this.value = value;

            public ulong value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadUInt64();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class FloatParameter : Parameter
        {
            public FloatParameter(float value) => this.value = value;

            public float value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadSingle();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class Vector2Parameter : Parameter
        {
            public Vector2Parameter(Vector2 value) => this.value = value;

            public Vector2 value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadVector2();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class Vector3Parameter : Parameter
        {
            public Vector3Parameter(Vector3 value) => this.value = value;

            public Vector3 value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadVector3();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class StringParameter : Parameter
        {
            public StringParameter() { }

            public StringParameter(string value) => this.value = value;

            public string value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadString();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        #endregion
    }
}
