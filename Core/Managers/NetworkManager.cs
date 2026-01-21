using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

using LSFunctions;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

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
        public const int SPLIT_DATA_COUNT = 500000; // 9 MB

        bool eventsSet;

        /// <summary>
        /// List of functions to run over a network.
        /// </summary>
        public List<NetworkFunction> functions = new List<NetworkFunction>
        {
            //new NetworkFunction(NetworkFunction.SEND_CHUNK_DATA, 1, reader =>
            //{
            //    var id = reader.ReadString();
            //    if (!inst.queuedBytes.TryGetValue(id, out DataQueue dataQueue))
            //        return;

            //    using var writer = new NetworkWriter();
            //    writer.Write(dataQueue.id);
            //    writer.Write(dataQueue.count);
            //    writer.Write(id);
            //    writer.Write(dataQueue.chunks[0].ToArray());
            //    dataQueue.chunks.RemoveAt(0);

            //    if (dataQueue.chunks.IsEmpty())
            //        inst.queuedBytes.Remove(id);

            //    inst.Send(dataQueue.side, writer.GetData(), SendType.Reliable);
            //}),
            new NetworkFunction(Side.Client, NetworkFunction.CLIENT_TEST, reader => { }),
            new NetworkFunction(Side.Server, NetworkFunction.SERVER_TEST, reader => { }),
            new NetworkFunction(NetworkFunction.MULTI_TEST, reader => { }),

            #region Players

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
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_BOOST, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Boost();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_BOOST_STOP, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.StopBoosting();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_JUMP, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Jump();
            }),
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_HEAL, 3, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                var heal = reader.ReadInt32();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Heal(heal);
            }),
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_HIT, 3, reader =>
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
            new NetworkFunction(Side.Client, NetworkFunction.PLAYER_KILL, 2, reader =>
            {
                var steamID = reader.ReadUInt64();
                if (steamID == RTSteamManager.inst.steamUser.steamID)
                    return;

                var id = reader.ReadString();
                if (PlayerManager.Players.TryFind(x => x.id == id, out PAPlayer player) && player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
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

            #endregion

            new NetworkFunction(Side.Client, NetworkFunction.LOG_CLIENT, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(Side.Server, NetworkFunction.LOG_SERVER, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(NetworkFunction.LOG_MULTI, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_SCENE, 3, reader =>
            {
                var func = reader.ReadInt32();
                if (func != 0)
                    SceneHelper.OnSceneLoad += scene => inst.RunFunction(func);
                SceneHelper.LoadScene((SceneName)reader.ReadByte(), reader.ReadBoolean());
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_GAME_DATA, 2, reader =>
            {
                var steamID = reader.ReadString();
                if (!string.IsNullOrEmpty(steamID) && ulong.TryParse(steamID, out ulong id) && RTSteamManager.inst.steamUser.steamID != id)
                    return;

                SteamLobbyManager.inst.CurrentLobby.SetMemberData("IsLoaded", "1");

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
                    NetworkFunction.SetClientGameData(GameData.Current, id.ToString());
                    NetworkFunction.SetClientMusicTime(AudioManager.inst.CurrentAudioSource.time);
                    NetworkFunction.SetClientPitch(RTLevel.Current && RTLevel.Current.eventEngine ? RTLevel.Current.eventEngine.pitchOffset : AudioManager.inst.pitch);
                }
            }),

            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_AUDIO, 1, reader =>
            {
                if (ProjectArrhythmia.State.InEditor)
                    EditorLevelManager.inst.SetCurrentAudio(Packet.AudioClipFromPacket(reader));
                else
                    LevelManager.SetCurrentAudio(Packet.AudioClipFromPacket(reader));
            }),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),
        };

        Dictionary<string, DataChunkHandler> dataChunkHandlers = new Dictionary<string, DataChunkHandler>();

        Dictionary<string, NetworkWriter> dataChunks = new Dictionary<string, NetworkWriter>();

        //Dictionary<string, DataQueue> queuedBytes = new Dictionary<string, DataQueue>();

        /// <summary>
        /// If packets are being written.
        /// </summary>
        public bool writingPackets;

        #endregion

        #region Functions

        public override void OnInit() => SetEvents();

        public override void OnTick()
        {
            if (!Transport.Instance)
                return;

            foreach (var player in PlayerManager.Players)
            {
                if (!player.IsLocalPlayer)
                    continue;

                // send player update
            }

            Transport.Instance.Receive();
        }

        public override void OnManagerDestroyed() => RemoveEvents();

        // BetterLegacy.Core.Managers.NetworkManager.inst.RunFunction(NetworkFunction.LOG_MULTI, new NetworkFunction.StringParameter("test"));
        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(int id, params IPacket[] packets) => RunFunction(id, SendType.Reliable, packets);

        /// <summary>
        /// Runs a function over the network.
        /// </summary>
        /// <param name="id">ID of the function.</param>
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(int id, SendType sendType, params IPacket[] packets)
        {
            if (functions.TryFind(x => x.id == id && x.parameterCount == packets.Length, out NetworkFunction function))
                RunFunction(function, sendType, packets);
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
        /// <param name="sendType">How the packet data should be handled over the network.</param>
        /// <param name="packets">Parameters as packets. Must match the specific network functions' parameter count.</param>
        public void RunFunction(NetworkFunction function, SendType sendType, params IPacket[] packets)
        {
            //using var writer = new NetworkWriter();
            //writer.Write(function.id);
            //var setPos = writer.Position; // 10
            //var uniqueID = LSText.randomNumString(16);
            //writer.Write(uniqueID);
            //for (int i = 0; i < packets.Length; i++)
            //    packets[i].WritePacket(writer);
            //var position = writer.Position; // 150
            //writer.Position = setPos; // set 10
            //writer.Write(position); // add 20 = 30 - 10
            //writer.Position = position + (writer.Position - setPos); // 170

            writingPackets = true;
            // not very good way of getting packet length but idk how else to do it so yeah
            using var packetWriter = new NetworkWriter();
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(packetWriter);
            var length = packetWriter.Position;
            using var writer = new NetworkWriter();
            writer.Write(function.id);
            writer.Write(length);
            var uniqueID = LSText.randomNumString(16);
            writer.Write(uniqueID);
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(writer);

            var data = writer.GetData();
            writingPackets = false;
            // handle data chunk splitting
            if (length > SPLIT_DATA_COUNT)
            {
                CoreHelper.Log($"Splitting function [{function.id} {uniqueID}] and sending it to [{function.side}] with send type [{sendType}]\nPacket size: {length}");
                Split(data, function, length, uniqueID);
                return;
            }

            Send(function.side, data, sendType);
        }

        void Split(ArraySegment<byte> data, NetworkFunction function, long position, string uniqueID)
        {
            var chunks = data.Split(SPLIT_DATA_COUNT);
            //var dataQueue = new DataQueue(chunks, function.side, function.id, position);
            //queuedBytes[uniqueID] = dataQueue;
            while (!chunks.IsEmpty())
            {
                using var writer = new NetworkWriter();
                writer.Write(function.id);
                writer.Write(position);
                writer.Write(uniqueID);
                writer.Write(chunks.Count);
                var d = chunks[0].ToArray();
                writer.Write(d.Length);
                writer.Write(d);
                chunks.RemoveAt(0);
                Send(function.side, writer.GetData(), SendType.NoDelay);
            }
        }

        void Send(NetworkFunction.Side side, ArraySegment<byte> data, SendType sendType)
        {
            switch (side)
            {
                case NetworkFunction.Side.Client: {
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

        public void ConnectToServer(string address) => Transport.Instance.ConnectClient(address);

        public void Disconnect()
        {
            clientConnections.Clear();
            serverConnection = null;
            Transport.Instance?.StopClient();
            Transport.Instance = null;
        }

        public void KickClient(int clientID) => Transport.Instance.KickConnection(clientID);

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
            var id = reader.ReadInt32();
            if (HandleChunkData(ref reader) && functions.TryFind(x => x.id == id && x.side != NetworkFunction.Side.Server, out NetworkFunction function))
                function.Run(reader);
            reader.Dispose();
        }

        public void StartServer()
        {
            Transport.Instance = new Transport();
            clientConnections.Clear();
            ServerSelfPeerConnection = new ClientNetworkConnection(0, RTSteamManager.inst.steamUser.steamID.ToString());
            Transport.Instance.StartServer();
            Transport.Instance.steamIDToNetID.Add(RTSteamManager.inst.steamUser.steamID, ServerSelfPeerConnection.connectionID);
            Transport.Instance.idToConnection.Add(ServerSelfPeerConnection.connectionID, null);
            clientConnections.Add(0, ServerSelfPeerConnection);
        }

        public void StopServer()
        {
            clientConnections.Clear();
            ServerSelfPeerConnection = null;
            Transport.Instance?.StopServer();
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
            var id = reader.ReadInt32();
            if (HandleChunkData(ref reader) && functions.TryFind(x => x.id == id && x.side != NetworkFunction.Side.Client, out NetworkFunction function))
                function.Run(reader);
            reader.Dispose();
        }

        bool HandleChunkData(ref NetworkReader reader)
        {
            var dataLength = reader.ReadInt64();
            var uniqueID = reader.ReadString();
            if (dataLength <= SPLIT_DATA_COUNT)
                return true;

            var count = reader.ReadInt32();
            var currentDataLength = reader.ReadInt32();

            if (!dataChunks.TryGetValue(uniqueID, out NetworkWriter writer))
            {
                writer = new NetworkWriter();
                dataChunks[uniqueID] = writer;
            }

            writer.Write(reader.ReadBytes(currentDataLength));

            if (count > 1)
            {
                CoreHelper.Log($"Data chunk is not the end of the chunk list, so waiting for more.\n" +
                    $"Count: {count}\n" +
                    $"ID: {uniqueID}\n" +
                    $"Total data size: {dataLength}\n" +
                    $"Chunk data size: {currentDataLength}");
                return false;
            }

            CoreHelper.Log($"Data chunk has reached the end.\n" +
                    $"Count: {count}\n" +
                    $"ID: {uniqueID}\n" +
                    $"Total data size: {dataLength}\n" +
                    $"Chunk data size: {currentDataLength}");

            reader = new NetworkReader(writer.GetData());
            writer.Dispose();
            dataChunks.Remove(uniqueID);

            //if (!dataChunkHandlers.TryGetValue(uniqueID, out DataChunkHandler handler))
            //{
            //    handler = new DataChunkHandler();
            //    dataChunkHandlers[uniqueID] = handler;
            //}

            //if (!handler.WriteChunkData(reader, dataLength))
            //{
            //    CoreHelper.Log($"Requesting more chunk data for [{uniqueID}] with total size [{dataLength}]");
            //    //RunFunction(NetworkFunction.SEND_CHUNK_DATA, new NetworkFunction.StringParameter(uniqueID));
            //    return false;
            //}
            //CoreHelper.Log($"Chunk data request for [{uniqueID}] with total size [{dataLength}] has ended");
            //dataChunkHandlers.Remove(uniqueID);
            //reader = new NetworkReader(handler.GetData());
            return true;
        }

        #endregion

        #region Sub Classes

        //class DataQueue
        //{
        //    public DataQueue(List<List<byte>> chunks, NetworkFunction.Side side, int id, long count)
        //    {
        //        this.chunks = chunks;
        //        this.side = side;
        //        this.id = id;
        //        this.count = count;
        //    }
        //    public List<List<byte>> chunks = new List<List<byte>>();
        //    public NetworkFunction.Side side;
        //    public int id;
        //    public long count;
        //}

        class DataChunkHandler : Exists, IDisposable
        {
            public DataChunkHandler() => writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);

            public long Position { get => memoryStream.Position; set => memoryStream.Position = value; }
            MemoryStream memoryStream = new MemoryStream(1024);
            readonly BinaryWriter writer;

            /// <summary>
            /// Gets the byte data of the current writer.
            /// </summary>
            /// <returns>Returns a byte array.</returns>
            public ArraySegment<byte> GetData() => new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);

            /// <summary>
            /// Writes chunk data from packet data.
            /// </summary>
            /// <param name="reader">The current network reader.</param>
            /// <returns>Returns <see langword="true"/> if the chunk data has reached the end, otherwise returns <see langword="false"/>.</returns>
            public bool WriteChunkData(NetworkReader reader, long dataCount)
            {
                if (Position >= dataCount)
                    return true;
                writer.Write(reader.ReadBytes(SPLIT_DATA_COUNT));
                return Position >= dataCount;
            }

            public void Dispose()
            {
                writer.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
