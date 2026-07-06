using System;

using SteamworksFacepunch.Data;

namespace BetterLegacy.Core.Data.Network
{
    // classes based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    /// <summary>
    /// Base class for network connections.
    /// </summary>
    public abstract class NetworkConnection : Exists
    {
        #region Constructors

        internal NetworkConnection() { }

        internal NetworkConnection(int connectionID, string address)
        {
            this.connectionID = connectionID;
            Address = address;
        }

        #endregion

        #region Values

        /// <summary>
        /// Connection ID.
        /// </summary>
        public readonly int connectionID = 0;

        /// <summary>
        /// Connection address.
        /// </summary>
        public string Address { get; private set; }

        #endregion

        #region Functions

        /// <summary>
        /// Sends a message to client / server.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="sendType">Send type.</param>
        public abstract void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable);

        #endregion
    }

    /// <summary>
    /// Represents a connection to a server.
    /// </summary>
    public class ServerNetworkConnection : NetworkConnection
    {
        public ServerNetworkConnection(string address) : base(0, address) { }

        public override void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable) => Transport.Instance.SendMessageToServer(data, sendType);
    }

    /// <summary>
    /// Represents a connection to a client.
    /// </summary>
    public class ClientNetworkConnection : NetworkConnection
    {
        public ClientNetworkConnection(int connectionID, string address) : base(connectionID, address) { }

        public override void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable) => Transport.Instance.SendMessageToClient(connectionID, data, sendType);
    }
}
