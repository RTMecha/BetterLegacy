using UnityEngine;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a 2D transform.
    /// </summary>
    public class ObjectTransform : Exists, IPacket
    {
        public ObjectTransform(Vector3 position, Vector2 scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        #region Values

        public Vector3 position;
        public Vector2 scale;
        public float rotation;

        #endregion

        #region Functions

        public void ReadPacket(NetworkReader reader)
        {
            position = reader.ReadVector3();
            scale = reader.ReadVector2();
            rotation = reader.ReadSingle();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(position);
            writer.Write(scale);
            writer.Write(rotation);
        }

        /// <summary>
        /// Converts <see cref="ObjectTransform"/> to <see cref="Struct"/>.
        /// </summary>
        /// <returns>Returns a struct based on <see cref="ObjectTransform"/>.</returns>
        public Struct ToStruct() => new Struct(position, scale, rotation);

        #endregion

        /// <summary>
        /// Struct version of <see cref="ObjectTransform"/>.
        /// </summary>
        public struct Struct : IPacket
        {
            public static Struct Default => new Struct(Vector3.zero, Vector2.one, 0f);

            public Struct(Vector3 position, Vector2 scale, float rotation)
            {
                this.position = position;
                this.scale = scale;
                this.rotation = rotation;
            }

            public Vector3 position;
            public Vector2 scale;
            public float rotation;

            public void ReadPacket(NetworkReader reader)
            {
                position = reader.ReadVector3();
                scale = reader.ReadVector2();
                rotation = reader.ReadSingle();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(position);
                writer.Write(scale);
                writer.Write(rotation);
            }

            /// <summary>
            /// Converts <see cref="Struct"/> to <see cref="ObjectTransform"/>.
            /// </summary>
            /// <returns>Returns a <see cref="ObjectTransform"/> based on the struct.</returns>
            public ObjectTransform ToClass() => new ObjectTransform(position, scale, rotation);
        }
    }

    /// <summary>
    /// Represents a 3D transform.
    /// </summary>
    public class FullTransform : Exists, IPacket
    {
        public FullTransform(Vector3 position, Vector3 scale, Vector3 rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        #region Values

        public Vector3 position;
        public Vector3 scale;
        public Vector3 rotation;

        #endregion

        #region Functions

        public void ReadPacket(NetworkReader reader)
        {
            position = reader.ReadVector3();
            scale = reader.ReadVector3();
            rotation = reader.ReadVector3();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(position);
            writer.Write(scale);
            writer.Write(rotation);
        }

        /// <summary>
        /// Converts <see cref="FullTransform"/> to <see cref="Struct"/>.
        /// </summary>
        /// <returns>Returns a struct based on <see cref="FullTransform"/>.</returns>
        public Struct ToStruct() => new Struct(position, scale, rotation);

        #endregion

        /// <summary>
        /// Struct version of <see cref="FullTransform"/>.
        /// </summary>
        public struct Struct : IPacket
        {
            public static Struct Default => new Struct(Vector3.zero, Vector3.one, Vector3.zero);

            public Struct(Vector3 position, Vector3 scale, Vector3 rotation)
            {
                this.position = position;
                this.scale = scale;
                this.rotation = rotation;
            }

            public Vector3 position;
            public Vector3 scale;
            public Vector3 rotation;

            public void ReadPacket(NetworkReader reader)
            {
                position = reader.ReadVector3();
                scale = reader.ReadVector3();
                rotation = reader.ReadVector3();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(position);
                writer.Write(scale);
                writer.Write(rotation);
            }

            /// <summary>
            /// Converts <see cref="Struct"/> to <see cref="FullTransform"/>.
            /// </summary>
            /// <returns>Returns a <see cref="FullTransform"/> based on the struct.</returns>
            public FullTransform ToClass() => new FullTransform(position, scale, rotation);
        }
    }
}
