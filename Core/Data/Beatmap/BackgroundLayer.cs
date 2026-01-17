using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BackgroundLayer : PAObject<BackgroundLayer>, IPacket, IPrefabable
    {
        #region Constructors

        public BackgroundLayer() : base() { }

        public BackgroundLayer(int depth, int color) : this()
        {
            this.depth = depth;
            this.color = color;
        }

        #endregion

        #region Values

        /// <summary>
        /// Depth offset of the layer.
        /// </summary>
        public int depth;

        /// <summary>
        /// Color slot override for all Background Objects on this BG layer. If the value is -1, then BG objects will continue to use their own color.
        /// </summary>
        public int color = -1;

        public string OriginalID { get; set; }

        public string PrefabID { get; set; }

        public string PrefabInstanceID { get; set; }

        public bool FromPrefab { get; set; }

        public Prefab CachedPrefab { get; set; }
        public PrefabObject CachedPrefabObject { get; set; }

        public float StartTime { get; set; }

        /// <summary>
        /// Runtime object reference.
        /// </summary>
        public BackgroundLayerObject runtimeObject;

        #endregion

        #region Functions

        public override void CopyData(BackgroundLayer orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            depth = orig.depth;
            color = orig.color;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            depth = jn["d"].AsInt;
            color = jn["c"].AsInt;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            depth = jn["d"].AsInt;
            if (jn["col"] != null)
                color = jn["col"].AsInt;

            this.ReadPrefabJSON(jn);
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["d"] = depth;
            jn["c"] = color;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id;
            jn["d"] = depth;
            if (color != -1)
                jn["col"] = color;

            this.WritePrefabJSON(jn);

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();

            this.ReadPrefabPacket(reader);

            depth = reader.ReadInt32();
            color = reader.ReadInt32();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);

            this.WritePrefabPacket(writer);

            writer.Write(depth);
            writer.Write(color);
        }

        /// <summary>
        /// Gets the default background layer.
        /// </summary>
        /// <param name="i">Background layer index.</param>
        /// <returns>Returns the default background layer.</returns>
        public static BackgroundLayer GetDefault(int i) => new BackgroundLayer(100 * (i + 1), 1 * (i + 1));

        public IRTObject GetRuntimeObject() => null;

        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false) => 0f;

        #endregion
    }
}
