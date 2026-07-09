using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Managers
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    /// <summary>
    /// Manages server-client connections.
    /// </summary>
    public class NetworkManager : BaseManager<NetworkManager, ManagerSettings>
    {
        #region Values

        protected ServerNetworkConnection serverConnection;
        protected Dictionary<int, ClientNetworkConnection> clientConnections = new Dictionary<int, ClientNetworkConnection>();

        public ClientNetworkConnection ServerSelfPeerConnection { get; protected set; }

        public Action<ServerNetworkConnection> onClientConnectedTemp;

        public Action<int, ClientNetworkConnection> onServerConnectedTemp;

        // was gonna do 9000000 but oh well
        /// <summary>
        /// Max packet size until packets get split.
        /// </summary>
        public const int SPLIT_DATA_COUNT = 500000; // 500KB

        /// <summary>
        /// Max buffer size.
        /// </summary>
        public const int MAX_BUFFER_SIZE = 10000000; // 10MB

        bool eventsSet;

        /// <summary>
        /// List of functions to run over a network.
        /// </summary>
        public List<NetworkFunction> functions = new List<NetworkFunction>
        {
            new NetworkFunction(Side.Client, NetworkFunction.LOG_CLIENT, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(Side.Server, NetworkFunction.LOG_SERVER, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(NetworkFunction.LOG_MULTI, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(NetworkFunction.SET_LOADED, 1, reader => SteamLobbyManager.inst.SetLoaded(reader.ReadUInt64())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_UNLOADED, reader =>
            {
                SteamLobbyManager.inst.CurrentLobby.SetMemberData(SteamLobbyManager.IS_LOADED, "0");
                SteamLobbyManager.inst.UnloadAll();
                SteamLobbyManager.inst.SetSceneLoaded(false);
                SteamLobbyManager.inst.SetSongLoaded(false);
                SteamLobbyManager.inst.SetGameDataLoaded(false);
            }),
            new NetworkFunction(NetworkFunction.SEND_CHUNK_DATA, 1, reader =>
            {
                var uniqueID = reader.ReadString();
                if (!inst.dataChunkQueue.TryGetValue(uniqueID, out DataQueue dataQueue))
                    return;

                inst.dataChunkQueue.Remove(uniqueID);
                inst.Split(dataQueue.chunks, dataQueue.group, dataQueue.id, dataQueue.side, dataQueue.count, uniqueID, dataQueue.steamId);
            }),
            new NetworkFunction(Side.Server, NetworkFunction.REQUEST_HOST, 1, reader =>
            {
                var message = reader.ReadString();
                if (ProjectArrhythmia.State.InEditor)
                {
                    EditorManager.inst.DisplayNotification(message, message.Length / 10f, EditorManager.NotificationType.Warning);
                    return;
                }
                CoreHelper.Notify(message, RTColors.InvertColor(ProjectArrhythmia.State.InGame ? ThemeManager.inst.Current.backgroundColor : InterfaceManager.inst.CurrentTheme.backgroundColor));
            }),
        };

        public List<NetworkFunction> playerFunctions = new List<NetworkFunction>
        {
            new NetworkFunction(Side.Client, NetworkFunction.SEND_CLIENT_PLAYER_DATA, 1, reader =>
            {
                foreach (var player in PlayerManager.Players)
                    PlayerManager.DestroyPlayer(player);
                PlayerManager.Players.Clear();
                var list = new PacketList<PAPlayer>(new List<PAPlayer>());
                list.ReadPacket(reader);
                CoreHelper.Log($"Got players [{list.Count}]");
                for (int i = 0; i < list.Count; i++)
                {
                    var player = list[i];
                    if (SteamLobbyManager.inst.localPlayers != null && SteamLobbyManager.inst.localPlayers.TryFind(x => x.id == player.id, out PAPlayer origPlayer))
                    {
                        origPlayer.index = i;
                        PlayerManager.Players.Add(origPlayer);
                    }
                    else
                    {
                        player.index = i;
                        PlayerManager.Players.Add(player);
                    }
                }
                if (ProjectArrhythmia.State.InEditor && EditorManager.inst.hasLoadedLevel)
                    PlayerManager.SpawnPlayers(PlayerManager.GetSpawnPosition());
            }),
            new NetworkFunction(Side.Server, NetworkFunction.SEND_SERVER_PLAYER_DATA, 1, reader =>
            {
                var list = new PacketList<PAPlayer>(new List<PAPlayer>());
                list.ReadPacket(reader);
                CoreHelper.Log($"Got players [{list.Count}]");
                for (int i = 0; i < list.Count; i++)
                {
                    var player = list[i];
                    if (!PlayerManager.Players.Has(x => x.id == player.id))
                        PlayerManager.Players.Add(player);
                }
                PlayerManager.Players.Sort((a, b) => b.IsLocalPlayer.CompareTo(a.IsLocalPlayer));
                for (int i = 0; i < PlayerManager.Players.Count; i++)
                    PlayerManager.Players[i].index = i;
                SteamLobbyManager.inst.SyncPlayersToClients();
                if (ProjectArrhythmia.State.InEditor && EditorManager.inst.hasLoadedLevel)
                    PlayerManager.RespawnPlayers();
            }),
            new NetworkFunction(NetworkFunction.SEND_MULTI_PLAYER_DATA, 1, reader =>
            {
                foreach (var player in PlayerManager.Players)
                    PlayerManager.DestroyPlayer(player);
                PlayerManager.Players.Clear();
                var list = new PacketList<PAPlayer>(new List<PAPlayer>());
                list.ReadPacket(reader);
                CoreHelper.Log($"Got players [{list.Count}]");
                for (int i = 0; i < list.Count; i++)
                {
                    var player = list[i];
                    if (SteamLobbyManager.inst.localPlayers != null && SteamLobbyManager.inst.localPlayers.TryFind(x => x.id == player.id, out PAPlayer origPlayer))
                    {
                        origPlayer.index = i;
                        PlayerManager.Players.Add(origPlayer);
                    }
                    else
                    {
                        player.index = i;
                        PlayerManager.Players.Add(player);
                    }
                }
                if (ProjectArrhythmia.State.InEditor)
                    PlayerManager.SpawnPlayers(PlayerManager.GetSpawnPosition());
            }),
            new NetworkFunction(Side.Client, NetworkFunction.UPDATE_PLAYER_DATA, reader =>
            {
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.IsLocalPlayer)
                        player.ReadPacket(reader);
                }
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SPAWN_PLAYERS_CHECKPOINT, 1, reader => PlayerManager.SpawnPlayers(Packet.CreateFromPacket<Checkpoint>(reader), true)),
            new NetworkFunction(Side.Client, NetworkFunction.SPAWN_PLAYERS_POS, 1, reader => PlayerManager.SpawnPlayers(reader.ReadVector2(), true)),
            new NetworkFunction(Side.Client, NetworkFunction.RESPAWN_PLAYERS, 1, reader => PlayerManager.RespawnPlayers(true)),
            new NetworkFunction(Side.Client, NetworkFunction.RESPAWN_PLAYERS_POS, 1, reader => PlayerManager.RespawnPlayers(reader.ReadVector2(), true)),
            new NetworkFunction(Side.Client, NetworkFunction.DESTROY_PLAYERS, 1, reader => PlayerManager.DestroyPlayers(true)),
            new NetworkFunction(NetworkFunction.PLAYER_BOOST, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Boost();
            }),
            new NetworkFunction(NetworkFunction.PLAYER_BOOST_STOP, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.StopBoosting();
            }),
            new NetworkFunction(NetworkFunction.PLAYER_JUMP, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Jump();
            }),
            new NetworkFunction(NetworkFunction.PLAYER_HEAL, 3, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                var heal = reader.ReadInt32();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Heal(heal);
            }),
            new NetworkFunction(NetworkFunction.PLAYER_HIT, 3, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                var damage = reader.ReadInt32();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                {
                    if (damage > 0)
                        player.RuntimePlayer.Hit(damage);
                    else
                        player.RuntimePlayer.Hit();
                }
            }),
            new NetworkFunction(NetworkFunction.PLAYER_KILL, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
            }),
            new NetworkFunction(NetworkFunction.PLAYER_RESET_HEALTH, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player))
                    player.ResetHealth();
            }),
            new NetworkFunction(NetworkFunction.SET_PLAYER_POSITION, 4, reader =>
            {
                var steamID = reader.ReadUInt64();
                var id = reader.ReadString();
                var pos = reader.ReadVector2();
                var rot = reader.ReadSingle();
                if (steamID == RTSteamManager.inst.steamUser.steamID || !PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.position = pos;
                player.RuntimePlayer.rb.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            }),
            new NetworkFunction(NetworkFunction.SET_PLAYER_HEALTH, 3, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                var health = reader.ReadInt32();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player))
                    player.Health = health;
            }),
        };

        public List<NetworkFunction> interfaceFunctions = new List<NetworkFunction>
        {
            new NetworkFunction(NetworkFunction.PAUSE_GAME, reader => PauseInterface.Pause(false)),
            new NetworkFunction(NetworkFunction.UNPAUSE_GAME, reader => PauseInterface.UnPause(false)),

            new NetworkFunction(Side.Client, NetworkFunction.INIT_ARCADE_INTERFACE, 1, reader =>
            {
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    ArcadeInterface.Tab.tabs[i].ReadPacket(reader);
                ArcadeInterface.Init();
            }),

            new NetworkFunction(Side.Client, NetworkFunction.CLEAR_ARCADE_LEVELS, reader =>
            {
                LevelManager.Levels.Clear();
                LevelManager.ArcadeQueue.Clear();
                LevelManager.LevelCollections.Clear();
                LevelManager.CurrentLevel = null;
                LevelManager.CurrentLevelCollection = null;
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SEND_ARCADE_LEVEL, 3, reader =>
            {
                var level = Packet.CreateFromPacket<Level>(reader);
                var name = reader.ReadString();
                var progress = reader.ReadInt32();
                LevelManager.Levels.Add(level);
                LoadLevelsInterface.Current?.UpdateInfo(level.icon, $"Loading {name}", progress);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SEND_ARCADE_LEVEL_COLLECTION, 3, reader =>
            {
                var levelCollection = Packet.CreateFromPacket<LevelCollection>(reader);
                var name = reader.ReadString();
                var progress = reader.ReadInt32();
                LevelManager.LevelCollections.Add(levelCollection);
                LoadLevelsInterface.Current?.UpdateInfo(levelCollection.icon, $"Loading {name}", progress);
            }),

            new NetworkFunction(Side.Client, NetworkFunction.SEND_STEAM_LEVEL, 3, reader =>
            {
                var level = Packet.CreateFromPacket<Level>(reader);
                var name = reader.ReadString();
                var progress = reader.ReadInt32();
                RTSteamManager.inst.Levels.Add(level);
                LoadLevelsInterface.Current?.UpdateInfo(level.icon, $"Loading {name}", progress);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SORT_ARCADE_LEVELS, 2, reader =>
            {
                LobbyInfo.LocalLevelSort = (LevelSort)reader.ReadInt32();
                LobbyInfo.LocalLevelAscend = reader.ReadBoolean();
                LevelManager.Sort(LobbyInfo.LocalLevelSort, LobbyInfo.LocalLevelAscend);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SORT_STEAM_LEVELS, 2, reader =>
            {
                LobbyInfo.SteamLevelSort = (LevelSort)reader.ReadInt32();
                LobbyInfo.SteamLevelAscend = reader.ReadBoolean();
                RTSteamManager.inst.Levels = LevelManager.SortLevels(RTSteamManager.inst.Levels, LobbyInfo.SteamLevelSort, LobbyInfo.SteamLevelAscend);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SORT_STEAM_WORKSHOP_LEVELS, 1, reader =>
            {
                LobbyInfo.SteamWorkshopSort = (QuerySort)reader.ReadInt32();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SORT_ONLINE_LEVELS, 2, reader =>
            {
                LobbyInfo.OnlineLevelSort = (OnlineLevelSort)reader.ReadInt32();
                LobbyInfo.OnlineLevelAscend = reader.ReadBoolean();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SORT_ONLINE_LEVEL_COLLECTIONS, 2, reader =>
            {
                LobbyInfo.OnlineLevelCollectionSort = (OnlineLevelCollectionSort)reader.ReadInt32();
                LobbyInfo.OnlineLevelCollectionAscend = reader.ReadBoolean();
            }),

            new NetworkFunction(Side.Client, NetworkFunction.INIT_PLAY_LEVEL_INTERFACE, 1, reader => PlayLevelInterface.InitClient(Packet.CreateFromPacket<Level>(reader))),
            new NetworkFunction(Side.Client, NetworkFunction.INIT_LOAD_LEVELS_INTERFACE, 1, reader => LoadLevelsInterface.InitClient(reader.ReadInt32())),
        };

        public List<NetworkFunction> gameFunctions = new List<NetworkFunction>
        {
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_SCENE, 3, reader =>
            {
                var scene = (SceneName)reader.ReadByte();
                var showLoading = reader.ReadBoolean();
                var func = reader.ReadInt32();
                if (func != 0)
                    SceneHelper.OnSceneLoad += scene => inst.RunFunction(func);
                SceneHelper.OnSceneLoad += scene => SteamLobbyManager.inst.SetSceneLoaded(true);
                SceneHelper.LoadScene(scene, showLoading);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_GAME_DATA, 2, reader =>
            {
                var steamID = reader.ReadString();
                if (!string.IsNullOrEmpty(steamID) && ulong.TryParse(steamID, out ulong id) && RTSteamManager.inst.steamUser.steamID != id)
                    return;

                try
                {
                    if (ProjectArrhythmia.State.InEditor)
                        EditorLevelManager.inst.ClearObjects();
                    else
                        LevelManager.ClearObjects();

                    GameData.Current = null;
                    GameData.Current = Packet.CreateFromPacket<GameData>(reader);
                    RTLevel.Reinit();
                    RTPlayer.SetGameDataProperties();
                    CoroutineHelper.StartCoroutine(GameData.Current.assets.LoadSounds());

                    if (ProjectArrhythmia.State.InEditor)
                    {
                        if (RTEditor.inst.PreviewCover != null && RTEditor.inst.PreviewCover.gameObject)
                            RTEditor.inst.PreviewCover.gameObject.SetActive(false);

                        EditorLevelManager.inst.PostInitLevel();
                        EditorTimeline.inst.RenderTimeline();
                        EditorTimeline.inst.RenderBins();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read game data due to the exception: {ex}");
                }

                SteamLobbyManager.inst.SetGameDataLoaded(true);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_META_DATA, 2, reader =>
            {
                var steamID = reader.ReadString();
                if (!string.IsNullOrEmpty(steamID) && ulong.TryParse(steamID, out ulong id) && RTSteamManager.inst.steamUser.steamID != id)
                    return;

                try
                {
                    LevelManager.SetCurrentMetaData(Packet.CreateFromPacket<MetaData>(reader));
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read game data due to the exception: {ex}");
                }
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_RUNTIME, 2, reader =>
            {
                var steamID = reader.ReadString();
                if (!string.IsNullOrEmpty(steamID) && ulong.TryParse(steamID, out ulong id) && RTSteamManager.inst.steamUser.steamID != id)
                    return;

                try
                {
                    RTBeatmap.Current.ReadPacket(reader);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read game data due to the exception: {ex}");
                }
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_SEED, 2, reader =>
            {
                var steamID = reader.ReadString();
                if (!string.IsNullOrEmpty(steamID) && ulong.TryParse(steamID, out ulong id) && RTSteamManager.inst.steamUser.steamID != id)
                    return;

                try
                {
                    RandomHelper.CurrentSeed = reader.ReadString();
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read game data due to the exception: {ex}");
                }
            }),

            new NetworkFunction(Side.Server, NetworkFunction.REQUEST_GAME_DATA, 3, reader =>
            {
                var id = reader.ReadUInt64();
                var clientScene = (SceneName)reader.ReadByte();
                var currentScene = SceneHelper.Current;
                var currentInterface = reader.ReadString();
                if (currentScene != clientScene)
                {
                    NetworkFunction.SetClientScene(currentScene, true, 0);
                    NetworkFunction.RequestGameData(id, currentScene, currentInterface);
                }
                else if (SceneHelper.GetSceneType(currentScene) != SceneType.Interface)
                {
                    NetworkFunction.SetClientSeed(RandomHelper.CurrentSeed, id.ToString());
                    NetworkFunction.SetClientMetaData(MetaData.Current, id.ToString());
                    NetworkFunction.SetClientGameData(GameData.Current, id.ToString());
                    NetworkFunction.SetClientMusicTime(AudioManager.inst.CurrentAudioSource.time);
                    NetworkFunction.SetClientPitch(RTLevel.Current && RTLevel.Current.eventEngine ? RTLevel.Current.eventEngine.pitchOffset : AudioManager.inst.pitch);
                    NetworkFunction.SetClientRuntime(RTBeatmap.Current, id.ToString());
                    NetworkFunction.SetClientAudio(AudioManager.inst.CurrentAudioSource.clip);
                }
            }),

            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_AUDIO, 1, reader =>
            {
                if (ProjectArrhythmia.State.InEditor)
                    EditorLevelManager.inst.SetCurrentAudio(Packet.AudioClipFromPacket(reader));
                else
                    LevelManager.SetCurrentAudio(Packet.AudioClipFromPacket(reader));
                SteamLobbyManager.inst.SetSongLoaded(true);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),

            new NetworkFunction(Side.Server, NetworkFunction.REQUEST_MUSIC_TIME, reader => NetworkFunction.SetClientMusicTime(AudioManager.inst.CurrentAudioSource.time)),
            new NetworkFunction(Side.Client, NetworkFunction.SYNC_LEVEL, 2, reader =>
            {
                var inEditor = reader.ReadBoolean();
                var time = reader.ReadSingle();
                if (!inEditor)
                    AudioManager.inst.SetMusicTime(time);
            }),

            new NetworkFunction(NetworkFunction.SET_SERVER_PLAYING_STATE, 1, reader =>
            {
                var state = reader.ReadBoolean();
                if (ProjectArrhythmia.State.Paused == state)
                    return;
                if (state)
                    PauseInterface.Pause();
                else
                    PauseInterface.UnPause();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_PLAYING_STATE, 1, reader =>
            {
                var state = reader.ReadBoolean();
                if (ProjectArrhythmia.State.Paused == state)
                    return;
                if (state)
                    PauseInterface.Pause();
                else
                    PauseInterface.UnPause();
            }),

            new NetworkFunction(Side.Client, NetworkFunction.LOAD_CLIENT_LEVEL, 9, reader => LevelManager.PlayClient(reader, RTBeatmap.Current.EndOfLevel)),
            new NetworkFunction(Side.Client, NetworkFunction.LOAD_CLIENT_EDITOR_LEVEL, 4, reader =>
            {
                if (!ProjectArrhythmia.State.InEditor)
                {
                    SceneHelper.LoadScene(SceneName.Editor, scene => EditorLevelManager.inst.LoadLevelClient(reader));
                    return;
                }

                EditorLevelManager.inst?.LoadLevelClient(reader);
            }),

            new NetworkFunction(Side.Client, NetworkFunction.RESTART_LEVEL, reader => ArcadeHelper.RestartLevel()),
        };

        public List<NetworkFunction> editorFunctions = new List<NetworkFunction>
        {
            new NetworkFunction(Side.Client, NetworkFunction.CLEAR_EDITOR_LEVELS, reader =>
            {
                EditorLevelManager.inst.LevelPanels.Clear();
                EditorLevelManager.inst.OpenLevelPopup.ClearContent();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SEND_EDITOR_LEVEL, 1, reader =>
            {
                var levelPanel = Packet.CreateFromPacket<LevelPanel>(reader);
                EditorLevelManager.inst.LevelPanels.Add(levelPanel);

                if (levelPanel.isFolder)
                {
                    levelPanel.Init(levelPanel.Path);

                    if (ProjectArrhythmia.State.IsHosting)
                        NetworkFunction.SendEditorLevel(levelPanel);

                    return;
                }

                if (!levelPanel.Item)
                {
                    levelPanel.Init(levelPanel.Info);
                    return;
                }

                levelPanel.Init(levelPanel.Item);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.REFRESH_EDITOR_LEVEL_LIST, 1, reader => EditorLevelManager.inst.OpenLevelPopup.SearchField.text = reader.ReadString()),
        };

        Dictionary<string, NetworkWriterQueue> dataChunks = new Dictionary<string, NetworkWriterQueue>();
        Dictionary<string, DataQueue> dataChunkQueue = new Dictionary<string, DataQueue>();

        /// <summary>
        /// If packets are being written.
        /// </summary>
        public bool writingPackets;

        #endregion

        #region Functions

        public override void OnInit()
        {
            for (int i = 0; i < playerFunctions.Count; i++)
                playerFunctions[i].group =  NetworkFunction.Group.Player;
            for (int i = 0; i < interfaceFunctions.Count; i++)
                interfaceFunctions[i].group = NetworkFunction.Group.Interface;
            for (int i = 0; i < gameFunctions.Count; i++)
                gameFunctions[i].group = NetworkFunction.Group.Game;
            for (int i = 0; i < editorFunctions.Count; i++)
                editorFunctions[i].group = NetworkFunction.Group.Editor;
            SetEvents();
        }

        public override void OnTick()
        {
            if (!Transport.Instance)
                return;

            Transport.Instance?.server?.Receive();
            Transport.Instance?.client?.Receive();
        }

        public override void OnManagerDestroyed() => RemoveEvents();

        #region Network Functions

        List<NetworkFunction> GetNetworkFunctions(NetworkFunction.Group group) => (NetworkFunction.Group)group switch
        {
            NetworkFunction.Group.Player => playerFunctions,
            NetworkFunction.Group.Interface => interfaceFunctions,
            NetworkFunction.Group.Game => gameFunctions,
            _ => functions,
        };

        // BetterLegacy.Core.Managers.NetworkManager.inst.RunFunction(NetworkFunction.LOG_MULTI, new NetworkFunction.StringParameter("test"));
        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(int id, params IPacket[] packets) => RunFunction(NetworkFunction.Group.Core, id, SendType.Reliable, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="steamId">The specific client to send to.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(int id, SteamId? steamId, params IPacket[] packets) => RunFunction(NetworkFunction.Group.Core, id, SendType.Reliable, steamId, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="group">Group of the function.</param>
        /// <param name="id">ID of the function.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction.Group group, int id, params IPacket[] packets) => RunFunction(group, id, SendType.Reliable, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="group">Group of the function.</param>
        /// <param name="id">ID of the function.</param>
        /// <param name="steamId">The specific client to send to.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction.Group group, int id, SteamId? steamId, params IPacket[] packets) => RunFunction(group, id, SendType.Reliable, steamId, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction.Group group, int id, SendType sendType, params IPacket[] packets)
        {
            if (GetNetworkFunctions(group).TryFind(x => x.id == id && x.parameterCount == packets.Length, out NetworkFunction function))
                RunFunction(function, sendType, packets);
        }

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="steamId">The specific client to send to.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction.Group group, int id, SendType sendType, SteamId? steamId, params IPacket[] packets)
        {
            if (GetNetworkFunctions(group).TryFind(x => x.id == id && x.parameterCount == packets.Length, out NetworkFunction function))
                RunFunction(function, sendType, steamId, packets);
        }

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="function">Function to run.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction function, params IPacket[] packets) => RunFunction(function, SendType.Reliable, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="function">Function to run.</param>
        /// <param name="steamId">The specific client to send to.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction function, SteamId? steamId, params IPacket[] packets) => RunFunction(function, SendType.Reliable, steamId, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="function">Function to run.</param>
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction function, SendType sendType, params IPacket[] packets) => RunFunction(function, sendType, null, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="function">Function to run.</param>
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="steamId">The specific client to send to.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction function, SendType sendType, SteamId? steamId, params IPacket[] packets)
        {
            writingPackets = true;
            var uniqueID = LSText.randomNumString(16);
            // not very good way of getting packet length but idk how else to do it so yeah
            using var packetWriter = new NetworkWriter();
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(packetWriter);
            var length = packetWriter.Position;
            writingPackets = false;
            // handle data chunk splitting
            if (length > SPLIT_DATA_COUNT)
            {
                CoreHelper.Log($"Splitting function [{function.id} {uniqueID}] and sending it to [{function.side}] with send type [{sendType}]\nPacket size: {length}");
                Split(packetWriter.GetBuffer(), function, length, uniqueID, steamId);
                return;
            }
            using var writer = new NetworkWriter();
            writer.Write((int)function.group);
            writer.Write(function.id);
            writer.Write(length);
            writer.Write(uniqueID);
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(writer);

            writingPackets = false;
            Send(function.side, writer.GetData(), sendType, steamId);
        }

        void Split(byte[] data, NetworkFunction function, long position, string uniqueID, SteamId? steamId)
        {
            data = SevenZip.SevenZipCompressor.CompressBytes(data);
            //data = CoreHelper.Compress(data);
            var chunks = data.Split(SPLIT_DATA_COUNT);
            Split(chunks, (int)function.group, function.id, function.side, position, uniqueID, steamId);
        }

        void Split(List<List<byte>> chunks, int group, int id, NetworkFunction.Side side, long position, string uniqueID, SteamId? steamId)
        {
            long size = 0;
            while (!chunks.IsEmpty())
            {
                using var writer = new NetworkWriter();
                writer.Write(group);
                writer.Write(id);
                writer.Write(position);
                writer.Write(uniqueID);
                writer.Write(chunks.Count);
                //var data = CoreHelper.Compress(chunks[0].ToArray());
                var data = SevenZip.SevenZipCompressor.CompressBytes(chunks[0].ToArray());
                size += data.Length;
                writer.Write(size);
                writer.Write(data.Length);
                writer.Write(data);
                chunks.RemoveAt(0);
                Send(side, writer.GetData(), SendType.Reliable | SendType.NoNagle, steamId);

                if (size > MAX_BUFFER_SIZE)
                {
                    dataChunkQueue[uniqueID] = new DataQueue(chunks, side, group, id, uniqueID, position, steamId);
                    break;
                }
            }
        }

        void Send(NetworkFunction.Side side, ArraySegment<byte> data, SendType sendType, SteamId? steamId = null)
        {
            switch (side)
            {
                case NetworkFunction.Side.Client: {
                        if (steamId.TryGetValue(out SteamId value))
                            SendToClient(value, data, sendType);
                        else
                            SendToAllClients(data, sendType);
                        break;
                    }
                case NetworkFunction.Side.Server: {
                        if (ProjectArrhythmia.State.IsHosting)
                            Transport.onServerDataReceived?.Invoke(ServerSelfPeerConnection, data);
                        else
                            SendToServer(data, sendType);
                        break;
                    }
                case NetworkFunction.Side.Multi: {
                        if (ProjectArrhythmia.State.IsHosting)
                            Transport.onServerDataReceived?.Invoke(ServerSelfPeerConnection, data);
                        else
                            SendToServer(data, sendType);
                        SendToAllClients(data, sendType);
                        break;
                    }
            }
        }

        #endregion

        void SetEvents()
        {
            if (eventsSet)
                return;

            Transport.onClientConnected += OnClientConnected;
            Transport.onServerStarted += OnServerStarted;

            Transport.onClientDisconnected += OnClientDisconnected;
            Transport.onServerClientConnected += OnServerClientConnected;
            Transport.onServerClientDisconnected += OnServerClientDisconnected;

            Transport.onServerDataReceived += OnServerTransportDataReceived;
            Transport.onClientDataReceived += OnClientTransportDataReceived;

            eventsSet = true;
        }

        void RemoveEvents()
        {
            if (!eventsSet)
                return;

            Transport.onClientConnected -= OnClientConnected;
            Transport.onServerStarted -= OnServerStarted;

            Transport.onClientDisconnected -= OnClientDisconnected;
            Transport.onServerClientConnected -= OnServerClientConnected;
            Transport.onServerClientDisconnected -= OnServerClientDisconnected;

            Transport.onServerDataReceived -= OnServerTransportDataReceived;
            Transport.onClientDataReceived -= OnClientTransportDataReceived;

            eventsSet = false;
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="address">Steam ID address.</param>
        public void ConnectToServer(string address)
        {
            Transport.Instance.idToConnection.Clear();
            Transport.Instance.steamIDToNetID.Clear();
            if (ulong.TryParse(address, out ulong id))
            {
                Transport.Instance.client = SteamNetworkingSockets.ConnectRelay(id, 0, Transport.Instance);
                return;
            }

            throw new ArgumentException($"{address} is not a valid SteamID");
        }

        /// <summary>
        /// Disconnects from the current server.
        /// </summary>
        public void Disconnect()
        {
            clientConnections.Clear();
            serverConnection = null;
            Transport.Instance?.client?.Close();
            Transport.Instance = null;
        }

        /// <summary>
        /// Kicks a client's connection.
        /// </summary>
        /// <param name="clientID">Client ID to kick.</param>
        public void KickClient(int clientID)
        {
            if (Transport.Instance && Transport.Instance.idToConnection.TryGetValue(clientID, out Connection? connection))
                connection?.Close();
        }

        public void OnClientConnected(ServerNetworkConnection connection)
        {
            serverConnection = connection;
            onClientConnectedTemp?.Invoke(connection);
            onClientConnectedTemp = null;
        }

        public void OnClientDisconnected()
        {
            serverConnection = null;

            if (!ProjectArrhythmia.State.IsOnlineMultiplayer)
                return;

            RTSteamManager.inst.EndClient();
        }

        public void SendToServer(ArraySegment<byte> data, SendType sendType)
        {
            if (!serverConnection || Transport.Instance == null || !Transport.Instance.IsActive)
                throw new Exception("Tried calling a server rpc while server is null!");

            serverConnection.SendRpcToTransport(data, sendType);
        }

        internal void OnClientTransportDataReceived(ArraySegment<byte> data)
        {
            if (data.Count < 2)
            {
                CoreHelper.LogError($"Data was too small.");
                return;
            }
            var reader = new NetworkReader(data);
            var group = (NetworkFunction.Group)reader.ReadInt32();
            var id = reader.ReadInt32();
            //var handler = await HandleChunkData(reader);
            var handler = HandleChunkData(reader);
            if (handler.Item1 && GetNetworkFunctions(group).TryFind(x => x.id == id && x.side != NetworkFunction.Side.Server, out NetworkFunction function))
                function.Run(handler.Item2);
            handler.Item2.Dispose();
        }

        /// <summary>
        /// Starts hosting a server.
        /// </summary>
        public void StartServer()
        {
            Transport.Instance = new Transport();
            clientConnections.Clear();
            ServerSelfPeerConnection = new ClientNetworkConnection(0, RTSteamManager.inst.steamUser.steamID.ToString());
            Transport.Instance.idToConnection.Clear();
            Transport.Instance.steamIDToNetID.Clear();
            Transport.Instance.server = SteamNetworkingSockets.CreateRelaySocket(0, Transport.Instance);
            Transport.Instance.IsActive = true;
            Transport.Instance.steamIDToNetID.Add(RTSteamManager.inst.steamUser.steamID, ServerSelfPeerConnection.connectionID);
            Transport.Instance.idToConnection.Add(ServerSelfPeerConnection.connectionID, null);
            clientConnections.Add(0, ServerSelfPeerConnection);
        }

        /// <summary>
        /// Stops the current server.
        /// </summary>
        public void StopServer()
        {
            clientConnections.Clear();
            ServerSelfPeerConnection = null;
            Transport.Instance?.server?.Close();
            Transport.Instance = null;
        }

        public void OnServerClientDisconnected(ClientNetworkConnection connection) => clientConnections.Remove(connection.connectionID);

        public void OnServerClientConnected(ClientNetworkConnection connection)
        {
            clientConnections.Add(connection.connectionID, connection);
            onServerConnectedTemp?.Invoke(connection.connectionID, connection);
            onServerConnectedTemp = null;
        }

        public void OnServerStarted() => CoreHelper.Log($"Server started!");

        public void SendToClient(SteamId id, ArraySegment<byte> data, SendType sendType)
        {
            if (Transport.Instance && Transport.Instance.steamIDToNetID.TryGetValue(id, out int netID))
                SendToClient(netID, data, sendType);
        }

        public void SendToClient(int id, ArraySegment<byte> data, SendType sendType)
        {
            if (clientConnections.TryGetValue(id, out var connection))
                SendToClient(connection, data, sendType);
        }

        public void SendToClient(ClientNetworkConnection connection, ArraySegment<byte> data, SendType sendType)
        {
            if (Transport.Instance == null || !Transport.Instance.IsActive)
                throw new Exception("Tried calling a client rpc while transport is null!");
            if (connection == null)
                throw new ArgumentException("Tried to send rpc to invalid connection ID!");
            connection.SendRpcToTransport(data, sendType);
        }

        public void SendToAllClients(ArraySegment<byte> data, SendType sendType)
        {
            if (Transport.Instance == null || !Transport.Instance.IsActive)
                throw new Exception("Tried calling a client rpc while transport is null!");
            foreach (var connection in clientConnections)
            {
                if (ServerSelfPeerConnection == connection.Value)
                    continue;

                connection.Value.SendRpcToTransport(data, sendType);
            }
        }

        internal void OnServerTransportDataReceived(ClientNetworkConnection connection, ArraySegment<byte> data)
        {
            if (data.Count < 2)
            {
                CoreHelper.LogError($"Data was too small.");
                return;
            }
            var reader = new NetworkReader(data);
            var group = (NetworkFunction.Group)reader.ReadInt32();
            var id = reader.ReadInt32();
            //var handler = await HandleChunkData(reader);
            var handler = HandleChunkData(reader);
            if (handler.Item1 && GetNetworkFunctions(group).TryFind(x => x.id == id && x.side != NetworkFunction.Side.Client, out NetworkFunction function))
                function.Run(handler.Item2);
            handler.Item2.Dispose();
        }

        //async Task<(bool, NetworkReader)> HandleChunkData(NetworkReader reader)
        (bool, NetworkReader) HandleChunkData(NetworkReader reader)
        {
            var dataLength = reader.ReadInt64();
            var uniqueID = reader.ReadString();
            if (dataLength <= SPLIT_DATA_COUNT)
                return (true, reader);

            var count = reader.ReadInt32();
            var totalChunkSize = reader.ReadInt64();
            var currentDataLength = reader.ReadInt32();

            if (!dataChunks.TryGetValue(uniqueID, out NetworkWriterQueue writerQueue))
            {
                writerQueue = new NetworkWriterQueue();
                writerQueue.writer = new NetworkWriter();
                writerQueue.dataLength = dataLength;
                dataChunks[uniqueID] = writerQueue;
            }

            var sw = CoreHelper.StartNewStopwatch();
            //await Task.Run(() => writer.Write(reader.ReadBytes(currentDataLength)));
            //writer.Write(CoreHelper.Decompress(reader.ReadBytes(currentDataLength)));
            writerQueue.writer.Write(SevenZip.SevenZipExtractor.ExtractBytes(reader.ReadBytes(currentDataLength)));
            sw.Stop();

            if (totalChunkSize > MAX_BUFFER_SIZE)
                RunFunction(NetworkFunction.SEND_CHUNK_DATA, new NetworkFunction.StringParameter(uniqueID));

            if (count > 1)
            {
                CoreHelper.Log($"Data chunk is not the end of the chunk list, so waiting for more.\n" +
                    $"Count: {count}\n" +
                    $"ID: {uniqueID}\n" +
                    $"Total data size: {dataLength}\n" +
                    $"Current chunk sequence size: {totalChunkSize}\n" +
                    $"Chunk data size: {currentDataLength}\n" +
                    $"Elapsed read/write time: {sw.Elapsed}");
                return (false, reader);
            }

            var data = writerQueue.writer.GetBuffer();
            //var compressedData = CoreHelper.Decompress(data);
            var compressedData = SevenZip.SevenZipExtractor.ExtractBytes(data);
            reader.Dispose();
            reader = new NetworkReader(compressedData);
            writerQueue.writer.Dispose();
            dataChunks.Remove(uniqueID);
            CoreHelper.Log($"Data chunk has reached the end.\n" +
                    $"Count: {count}\n" +
                    $"ID: {uniqueID}\n" +
                    $"Total data size: {dataLength}\n" +
                    $"Current chunk sequence size: {totalChunkSize}\n" +
                    $"Chunk data size: {currentDataLength}\n" +
                    $"Compressed data size: {data.Length}\n" +
                    $"Final data size: {compressedData.Length}\n" +
                    $"Elapsed read/write time: {sw.Elapsed}");
            return (true, reader);
        }

        /// <summary>
        /// Gets the current progress of the network queue.
        /// </summary>
        /// <returns>Returns a 0-1 range of network queue progress.</returns>
        public float GetProgress()
        {
            if (dataChunks.IsEmpty())
                return 1f;

            long progress = 0;
            long length = 0;
            foreach (var networkWriterQueue in dataChunks.Values)
            {
                progress += networkWriterQueue.writer.Position;
                length += networkWriterQueue.dataLength;
            }
            return length == 0 ? 1f : progress / (float)length;
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// Wraps <see cref="NetworkWriter"/>.
        /// </summary>
        public class NetworkWriterQueue
        {
            /// <summary>
            /// Network writer reference.
            /// </summary>
            public NetworkWriter writer;

            /// <summary>
            /// Total size of expected data.
            /// </summary>
            public long dataLength;
        }

        /// <summary>
        /// Holds byte data in chunks.
        /// </summary>
        public class DataQueue
        {
            #region Constructors

            public DataQueue(List<List<byte>> chunks, NetworkFunction.Side side, int group, int id, string uniqueID, long count, SteamId? steamId)
            {
                this.chunks = chunks;
                this.side = side;
                this.group = group;
                this.id = id;
                this.uniqueID = uniqueID;
                this.count = count;
                this.steamId = steamId;
            }

            #endregion

            #region Values

            /// <summary>
            /// List of byte chunks.
            /// </summary>
            public List<List<byte>> chunks = new List<List<byte>>();

            /// <summary>
            /// Network function group.
            /// </summary>
            public int group;

            /// <summary>
            /// Network function side.
            /// </summary>
            public NetworkFunction.Side side;

            /// <summary>
            /// Network function ID.
            /// </summary>
            public int id;

            /// <summary>
            /// Network function unique ID.
            /// </summary>
            public string uniqueID;

            /// <summary>
            /// Total data length.
            /// </summary>
            public long count;

            /// <summary>
            /// Steam ID to send to.
            /// </summary>
            public SteamId? steamId;

            #endregion

            #region Functions

            /// <summary>
            /// Gets the current progress of the data chunks.
            /// </summary>
            /// <returns>Returns a 0-1 range of data chunk progress.</returns>
            public float GetProgress()
            {
                if (this.count == 0)
                    return 1f;
                long count = 0;
                for (int i = 0; i < chunks.Count; i++)
                    count += chunks[i].LongCount();
                return count / (float)this.count;
            }

            #endregion
        }

        #endregion
    }
}
