using System;

using UnityEngine;

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

        public const int CLIENT_TEST = 532986;

        public const int SERVER_TEST = 632957668;

        public const int MULTI_TEST = 984885;

        public const int SEND_PLAYER_DATA = 5932573;

        public const int UPDATE_PLAYER_DATA = 83553876;

        public const int LOG_CLIENT = 842754988;
        public const int LOG_SERVER = 53295835;
        public const int LOG_MULTI = 43292487;

        #endregion

        /// <summary>
        /// Side the function should only run on.
        /// </summary>
        public Side side;

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

        #endregion

        #region Functions

        /// <summary>
        /// Runs the function.
        /// </summary>
        public void Run(NetworkReader reader) => action?.Invoke(reader);

        #endregion

        #region Sub Classes

        public abstract class Parameter : Exists, IPacket
        {
            public abstract void ReadPacket(NetworkReader reader);

            public abstract void WritePacket(NetworkWriter writer);
        }

        public class IntParameter : Parameter
        {
            public int value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadInt32();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class FloatParameter : Parameter
        {
            public float value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadSingle();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class Vector2Parameter : Parameter
        {
            public Vector2 value;

            public override void ReadPacket(NetworkReader reader) => value = reader.ReadVector2();

            public override void WritePacket(NetworkWriter writer) => writer.Write(value);
        }

        public class Vector3Parameter : Parameter
        {
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

            public override void WritePacket(NetworkWriter writer) => writer.Write(value ?? string.Empty);
        }

        #endregion
    }
}
