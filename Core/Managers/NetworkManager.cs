using System;
using System.Collections.Generic;

using LSFunctions;

using SteamworksFacepunch.Data;

using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Managers
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class NetworkManager : BaseManager<NetworkManager, ManagerSettings>
    {
        protected ServerNetworkConnection serverConnection;
        protected Dictionary<int, ClientNetworkConnection> clientConnections = new Dictionary<int, ClientNetworkConnection>();

        public ClientNetworkConnection ServerSelfPeerConnection { get; protected set; }

        bool eventsSet;

        public List<NetworkFunction> functions = new List<NetworkFunction>
        {
            new NetworkFunction(Side.Client, NetworkFunction.CLIENT_TEST, reader => { }),
            new NetworkFunction(Side.Server, NetworkFunction.SERVER_TEST, reader => { }),
            new NetworkFunction(NetworkFunction.MULTI_TEST, reader => { }),
            new NetworkFunction(Side.Client, NetworkFunction.UPDATE_PLAYER_DATA, reader =>
            {
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.IsLocalPlayer)
                        player.ReadPacket(reader);
                }
            }),
            new NetworkFunction(Side.Client, NetworkFunction.LOG_CLIENT, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(Side.Server, NetworkFunction.LOG_SERVER, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(NetworkFunction.LOG_MULTI, 1, reader => CoreHelper.Log(reader.ReadString())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_GAME_DATA, 1, reader =>
            {
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
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_MUSIC_TIME, 1, reader => AudioManager.inst.SetMusicTime(reader.ReadSingle())),
            new NetworkFunction(Side.Client, NetworkFunction.SET_CLIENT_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),
            new NetworkFunction(Side.Server, NetworkFunction.SET_SERVER_PITCH, 1, reader => AudioManager.inst.SetPitch(reader.ReadSingle())),
        };

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
        public void RunFunction(int id, params IPacket[] packets)
        {
            if (functions.TryFind(x => x.id == id && x.parameterCount == packets.Length, out NetworkFunction function))
                RunFunction(function, packets);
        }

        public void RunFunction(NetworkFunction function, params IPacket[] packets)
        {
            var writer = new NetworkWriter();
            writer.Write(function.id);
            writer.Write(LSText.randomNumString(16));
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(writer);
            var position = writer.Position;
            writer.Position = 1;
            var greaterData = position > 9000000;
            writer.Write(greaterData); // means the data has been split up
            writer.Position = position + 1;

            var data = writer.GetData();
            // handle data chunk splitting
            //var chunks = data.Split(9000000);

            switch (function.side)
            {
                case NetworkFunction.Side.Client: {
                        SendToAllClients(data, SendType.Reliable);
                        break;
                    }
                case NetworkFunction.Side.Server: {
                        if (ProjectArrhythmia.State.IsHosting)
                            Transport.onServerDataReceived?.Invoke(ServerSelfPeerConnection, data);
                        else
                            SendToServer(data, SendType.Reliable);
                        break;
                    }
                case NetworkFunction.Side.Multi: {
                        if (ProjectArrhythmia.State.IsHosting)
                            Transport.onServerDataReceived?.Invoke(ServerSelfPeerConnection, data);
                        SendToAllClients(data, SendType.Reliable);
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

        public void OnClientConnected(ServerNetworkConnection connection) => serverConnection = connection;

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
            using var reader = new NetworkReader(data);
            var id = reader.ReadInt32();
            if (functions.TryFind(x => x.id == id && x.side != NetworkFunction.Side.Server, out NetworkFunction function))
                function.Run(reader);
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

        public void OnServerClientConnected(ClientNetworkConnection connection) => clientConnections.Add(connection.connectionID, connection);

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

            using var reader = new NetworkReader(data);
            var id = reader.ReadInt32();
            var uniqueID = reader.ReadString();
            var greaterData = reader.ReadBoolean();
            if (functions.TryFind(x => x.id == id && x.side != NetworkFunction.Side.Client, out NetworkFunction function))
                function.Run(reader);
        }
    }
}
