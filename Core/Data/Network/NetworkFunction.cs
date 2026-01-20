using System;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;

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

        public const int SEND_CLIENT_PLAYER_DATA = 5932573;
        public const int SEND_SERVER_PLAYER_DATA = 74362567;
        public const int SEND_MULTI_PLAYER_DATA = 476256437;

        public const int UPDATE_PLAYER_DATA = 83553876;

        public const int SET_PLAYER_POSITION = 326462532;

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

        public const int LOG_CLIENT = 842754988;
        public const int LOG_SERVER = 53295835;
        public const int LOG_MULTI = 43292487;

        public const int SET_CLIENT_GAME_DATA = 9432119;

        public const int SET_CLIENT_MUSIC_TIME = 352633265;
        public const int SET_SERVER_MUSIC_TIME = 75842873;

        public const int SET_CLIENT_PITCH = 266775874;
        public const int SET_SERVER_PITCH = 378545366;

        public const int SEND_CHUNK_DATA = 326243667;

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

        #region Global

        public static void LogClientSide(string message) => NetworkManager.inst.RunFunction(LOG_CLIENT, new StringParameter(message));

        public static void LogServerSide(string message) => NetworkManager.inst.RunFunction(LOG_SERVER, new StringParameter(message));

        public static void LogMultiSide(string message) => NetworkManager.inst.RunFunction(LOG_MULTI, new StringParameter(message));

        public static void SetClientGameData(GameData gameData, string id = null) => NetworkManager.inst.RunFunction(SET_CLIENT_GAME_DATA, gameData, new StringParameter(id));

        #endregion

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
