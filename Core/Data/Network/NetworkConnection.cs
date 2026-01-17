using System;

using SteamworksFacepunch.Data;

namespace BetterLegacy.Core.Data.Network
{
    // classes based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public abstract class NetworkConnection : Exists
    {
        internal NetworkConnection() { }

        internal NetworkConnection(int connectionID, string address)
        {
            this.connectionID = connectionID;
            Address = address;
        }

        public readonly int connectionID = 0;
        public string Address { get; private set; }

        public abstract void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable);
    }

    public class ServerNetworkConnection : NetworkConnection
    {
        public ServerNetworkConnection(string address) : base(0, address) { }

        public override void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable) => Transport.Instance.SendMessageToServer(data, sendType);
    }

    public class ClientNetworkConnection : NetworkConnection
    {
        public ClientNetworkConnection(int connectionID, string address) : base(connectionID, address) { }

        public override void SendRpcToTransport(ArraySegment<byte> data, SendType sendType = SendType.Reliable) => Transport.Instance.SendMessageToClient(connectionID, data, sendType);
    }
}
