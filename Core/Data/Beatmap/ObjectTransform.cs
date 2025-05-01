using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    public struct ObjectTransform
    {
        public static ObjectTransform Default => new ObjectTransform(Vector3.zero, Vector2.one, 0f);

        public ObjectTransform(Vector3 position, Vector2 scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public Vector3 position;
        public Vector2 scale;
        public float rotation;
    }
}
