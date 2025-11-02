using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a 2D transform.
    /// </summary>
    public class ObjectTransform : Exists
    {
        public ObjectTransform(Vector3 position, Vector2 scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public Vector3 position;
        public Vector2 scale;
        public float rotation;

        /// <summary>
        /// Converts <see cref="ObjectTransform"/> to <see cref="Struct"/>.
        /// </summary>
        /// <returns>Returns a struct based on <see cref="ObjectTransform"/>.</returns>
        public Struct ToStruct() => new Struct(position, scale, rotation);

        /// <summary>
        /// Struct version of <see cref="ObjectTransform"/>.
        /// </summary>
        public struct Struct
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
    public class FullTransform : Exists
    {
        public FullTransform(Vector3 position, Vector3 scale, Vector3 rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public Vector3 position;
        public Vector3 scale;
        public Vector3 rotation;

        /// <summary>
        /// Converts <see cref="FullTransform"/> to <see cref="Struct"/>.
        /// </summary>
        /// <returns>Returns a struct based on <see cref="FullTransform"/>.</returns>
        public Struct ToStruct() => new Struct(position, scale, rotation);

        /// <summary>
        /// Struct version of <see cref="FullTransform"/>.
        /// </summary>
        public struct Struct
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

            /// <summary>
            /// Converts <see cref="Struct"/> to <see cref="FullTransform"/>.
            /// </summary>
            /// <returns>Returns a <see cref="FullTransform"/> based on the struct.</returns>
            public FullTransform ToClass() => new FullTransform(position, scale, rotation);
        }
    }
}
