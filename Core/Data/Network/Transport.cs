using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Network
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class Transport : Exists, ISocketManager, IConnectionManager
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
                client = SteamNetworkingSockets.ConnectRelay(id, 0, this);
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
            server = SteamNetworkingSockets.CreateRelaySocket(0, this);
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

        #region ISocketManager (Server)

        public void OnConnecting(Connection connection, ConnectionInfo info)
        {
            connection.Accept();
            CoreHelper.Log($"Player {info.Identity.SteamId} is connecting to the game server.");
        }

        public void OnConnected(Connection connection, ConnectionInfo info)
        {
            CoreHelper.Log($"Client connected!");
            int id;
            if (steamIDToNetID.TryGetValue(info.Identity.SteamId, out id))
            {
                idToConnection[id] = connection;
                onServerClientConnected?.Invoke(new ClientNetworkConnection(id, info.Identity.SteamId.ToString()));
                return;
            }

            id = GetNextConnectionID();

            idToConnection.Add(id, connection);
            steamIDToNetID.Add(info.Identity.SteamId, id);

            onServerClientConnected?.Invoke(new ClientNetworkConnection(id, info.Identity.SteamId.ToString()));
        }

        public void OnDisconnected(Connection connection, ConnectionInfo info)
        {
            CoreHelper.Log($"Client disconnected!");
            connection.Close();

            if (!steamIDToNetID.TryGetValue(info.Identity.SteamId, out int id))
                return;

            idToConnection.Remove(id);
            steamIDToNetID.Remove(info.Identity.SteamId);
            onServerClientDisconnected?.Invoke(new ClientNetworkConnection(id, info.Identity.SteamId.ToString()));
        }

        public void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
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

            AssureBufferSpace(size);
            Marshal.Copy(data, buffer, 0, size);
            onServerDataReceived?.Invoke(new ClientNetworkConnection(id, identity.SteamId.ToString()), new ArraySegment<byte>(buffer, 0, size));
        }

        int GetIDFromSteamConnection(Connection connection)
        {
            foreach (var keyValuePair in idToConnection)
            {
                if (keyValuePair.Value == connection)
                    return keyValuePair.Key;
            }
            return -1;
        }

        int GetNextConnectionID()
        {
            int id = 0;
            while (idToConnection.ContainsKey(id))
                id++;
            return id;
        }

        #endregion

        #region IConnectionManager (Client)

        public void OnConnecting(ConnectionInfo info)
        {
            CoreHelper.Log($"Connecting");
        }

        public void OnConnected(ConnectionInfo info)
        {
            IsActive = true;
            onClientConnected?.Invoke(new ServerNetworkConnection(info.Identity.SteamId.ToString()));
            CoreHelper.Log($"Connected");
        }

        public void OnDisconnected(ConnectionInfo info)
        {
            IsActive = false;
            onClientDisconnected?.Invoke();
            CoreHelper.Log($"Disconnected");
        }

        public void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
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

            AssureBufferSpace(size);
            Marshal.Copy(data, buffer, 0, size);
            onClientDataReceived?.Invoke(new ArraySegment<byte>(buffer, 0, size));
        }

        #endregion
    }
}
