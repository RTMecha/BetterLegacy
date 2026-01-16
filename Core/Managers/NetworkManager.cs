using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class NetworkManager : BaseManager<NetworkManager, ManagerSettings>
    {
        protected ServerNetworkConnection serverConnection;
        protected Dictionary<int, ClientNetworkConnection> clientConnections = new Dictionary<int, ClientNetworkConnection>();

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
        };

        public override void OnInit() => SetEvents();

        public override void OnTick()
        {
            if (!ProjectArrhythmia.State.InGame || !ProjectArrhythmia.State.IsOnlineMultiplayer)
                return;

            foreach (var player in PlayerManager.Players)
            {
                if (!player.IsLocalPlayer)
                    continue;

                // send player update
            }

            Transport.Instance?.Receive();
        }

        public override void OnManagerDestroyed() => RemoveEvents();

        // BetterLegacy.Core.Managers.NetworkManager.inst.RunFunction(43292487, new NetworkFunction.StringParameter("test"));
        public void RunFunction(int id, params IPacket[] packets)
        {
            if (!functions.TryFind(x => x.id == id && x.parameterCount == packets.Length, out NetworkFunction function))
                return;

            var writer = new NetworkWriter();
            writer.Write(id);
            for (int i = 0; i < packets.Length; i++)
                packets[i].WritePacket(writer);

            switch (function.side)
            {
                case NetworkFunction.Side.Client: {
                        SendToAllClients(writer.GetData(), SendType.Reliable);
                        break;
                    }
                case NetworkFunction.Side.Server: {
                        SendToServer(writer.GetData(), SendType.Reliable);
                        break;
                    }
                case NetworkFunction.Side.Multi: {
                        // idk if this is how it works
                        SendToAllClients(writer.GetData(), SendType.Reliable);
                        SendToServer(writer.GetData(), SendType.Reliable);
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

        public void Disconnect() => Transport.Instance.StopClient();

        public void KickClient(int clientID) => Transport.Instance.KickConnection(clientID);

        public void OnClientConnected(ServerNetworkConnection connection) => serverConnection = connection;

        public void OnClientDisconnected() => serverConnection = null;

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

        public void OnServerClientDisconnected(ClientNetworkConnection connection) => clientConnections.Remove(connection.connectionID);

        public void OnServerClientConnected(ClientNetworkConnection connection) => clientConnections.Add(connection.connectionID, connection);

        public void OnServerStarted()
        {

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
                connection.Value.SendRpcToTransport(data, sendType);
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
            if (functions.TryFind(x => x.id == id && x.side != NetworkFunction.Side.Client, out NetworkFunction function))
                function.Run(reader);
        }
    }
}
