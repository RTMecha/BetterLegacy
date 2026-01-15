using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Network
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class Transport : Exists
    {
        /// <summary>
        /// The current transport instance.
        /// </summary>
        public static Transport Instance { get; set; }

        /// <summary>
        /// If the transport is connected and active.
        /// </summary>
        public bool IsActive { get; set; }

        internal readonly Dictionary<int, Connection?> idToConnection = new Dictionary<int, Connection?>();

        internal readonly Dictionary<ulong, int> steamIDToNetID = new Dictionary<ulong, int>();

        public static Action<ArraySegment<byte>> onClientDataReceived;

        public static Action<ServerNetworkConnection> onClientConnected;

        public static Action onClientDisconnected;

        SocketManager server;
        ConnectionManager client;

        public static byte[] buffer = new byte[1024];

        public void Receive()
        {
            server?.Receive();
            client?.Receive();
        }

        public void ConnectClient(string address)
        {
            idToConnection.Clear();
            steamIDToNetID.Clear();
            if (ulong.TryParse(address, out ulong id))
            {
                client = SteamNetworkingSockets.ConnectRelay<RTConnectionManager>(id, 0);
                return;
            }

            throw new ArgumentException($"{address} is not a valid SteamID");
        }

        public void StopClient()
        {
            IsActive = false;
            client?.Close();
        }

        public static Action<ClientNetworkConnection, ArraySegment<byte>> onServerDataReceived;
        public static Action<ClientNetworkConnection> onServerClientConnected;
        public static Action<ClientNetworkConnection> onServerClientDisconnected;
        public static Action onServerStarted;

        public void StartServer()
        {
            idToConnection.Clear();
            steamIDToNetID.Clear();
            server = SteamNetworkingSockets.CreateRelaySocket<RTSocketManager>(0);
            IsActive = true;
        }

        public void StopServer()
        {
            IsActive = false;
            server?.Close();
        }

        public void KickConnection(int connectionID)
        {
            if (idToConnection.TryGetValue(connectionID, out Connection? connection))
                connection?.Close();
        }

        public void SendMessageToServer(ArraySegment<byte> data, SendType sendType = SendType.Reliable)
        {
            client?.Connection.SendMessage(data.Array, data.Offset, data.Count, sendType);
        }

        public void SendMessageToClient(int connectionID, ArraySegment<byte> data, SendType sendType = SendType.Reliable)
        {
            if (idToConnection.TryGetValue(connectionID, out Connection? connection))
                connection?.SendMessage(data.Array, data.Offset, data.Count, sendType);
        }

        public void Shutdown()
        {
            server?.Close();
            client?.Close();
            idToConnection.Clear();
            steamIDToNetID.Clear();
            IsActive = false;
        }

        public static void AssureBufferSpace(int size)
        {
            if (buffer.Length >= size || size <= 0)
                return;

            // taken from MemoryStream
            // Check for overflow
            if (size <= buffer.Length)
                return;

            int newCapacity = Math.Max(size, 256);

            // We are ok with this overflowing since the next statement will deal
            // with the cases where _capacity*2 overflows.
            if (newCapacity < buffer.Length * 2)
                newCapacity = buffer.Length * 2;

            // We want to expand the array up to Array.MaxLength.
            // And we want to give the user the value that they asked for
            if ((uint)(buffer.Length * 2) > 999999)
                newCapacity = Math.Max(size, 9999999);

            byte[] newBuffer = new byte[newCapacity];

            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);

            buffer = newBuffer;
        }
    }

    public class RTSocketManager : SocketManager
    {
        public override void OnConnecting(Connection connection, ConnectionInfo info)
        {
            base.OnConnecting(connection, info);
            connection.Accept();
            CoreHelper.Log($"Player {info.Identity.SteamId} is connecting to the game server.");
        }

        public override void OnConnected(Connection connection, ConnectionInfo info)
        {
            base.OnConnected(connection, info);
            var id = GetNextConnectionID();

            Transport.Instance.idToConnection.Add(id, connection);
            Transport.Instance.steamIDToNetID.Add(info.Identity.SteamId, id);

            Transport.onServerClientConnected?.Invoke(new ClientNetworkConnection(id, info.Identity.SteamId.ToString()));
        }

        public override void OnDisconnected(Connection connection, ConnectionInfo info)
        {
            base.OnDisconnected(connection, info);
            connection.Close();

            if (!Transport.Instance.steamIDToNetID.TryGetValue(info.Identity.SteamId, out int id))
                return;

            Transport.Instance.idToConnection.Remove(id);
            Transport.Instance.steamIDToNetID.Remove(info.Identity.SteamId);
            Transport.onServerClientDisconnected?.Invoke(new ClientNetworkConnection(id, info.Identity.SteamId.ToString()));
        }

        public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
            var id = GetIDFromSteamConnection(connection);
            if (id == -1)
            {
                CoreHelper.LogError("Received data from someone not in the id to connection dictionary.");
                return;
            }

            if (size < 2)
            {
                CoreHelper.LogError("Received too little data, disconnecting.");
                connection.Close();
                return;
            }

            if (size > 524288)
            {
                CoreHelper.LogError("Received too much data from someone, disconnecting.");
                connection.Close();
                return;
            }

            Transport.AssureBufferSpace(size);
            Marshal.Copy(data, Transport.buffer, 0, size);
            Transport.onServerDataReceived?.Invoke(new ClientNetworkConnection(id, identity.SteamId.ToString()), new ArraySegment<byte>(Transport.buffer, 0, size));
        }

        int GetIDFromSteamConnection(Connection connection)
        {
            if (Transport.Instance == null)
                return -1;

            foreach (var keyValuePair in Transport.Instance.idToConnection)
            {
                if (keyValuePair.Value == connection)
                    return keyValuePair.Key;
            }
            return -1;
        }

        int GetNextConnectionID()
        {
            if (Transport.Instance == null)
                return -1;

            int id = 0;
            while (Transport.Instance.idToConnection.ContainsKey(id))
                id++;
            return id;
        }
    }

    public class RTConnectionManager : ConnectionManager
    {
        public override void OnConnected(ConnectionInfo info)
        {
            base.OnConnected(info);
            Transport.Instance.IsActive = true;
            Transport.onClientConnected?.Invoke(new ServerNetworkConnection(info.Identity.SteamId.ToString()));
        }

        public override void OnDisconnected(ConnectionInfo info)
        {
            base.OnDisconnected(info);
            Transport.Instance.IsActive = false;
            Transport.onClientDisconnected?.Invoke();
        }

        public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            base.OnMessage(data, size, messageNum, recvTime, channel);
            if (size < 2)
            {
                CoreHelper.LogError("Received too little data.");
                return;
            }

            if (size > 524288)
            {
                CoreHelper.LogError("Received too much data from the host.");
                return;
            }

            Transport.AssureBufferSpace(size);
            Marshal.Copy(data, Transport.buffer, 0, size);
            Transport.onClientDataReceived?.Invoke(new ArraySegment<byte>(Transport.buffer, 0, size));
        }
    }
}
