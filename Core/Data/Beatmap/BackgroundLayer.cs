using SimpleJSON;

using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BackgroundLayer : PAObject<BackgroundLayer>, IPrefabable
    {
        public BackgroundLayer() : base() { }

        public BackgroundLayer(int depth, int color) : this()
        {
            this.depth = depth;
            this.color = color;
        }

        /// <summary>
        /// Depth offset of the layer.
        /// </summary>
        public int depth;

        /// <summary>
        /// Color slot override for all Background Objects on this BG layer. If the value is -1, then BG objects will continue to use their own color.
        /// </summary>
        public int color = -1;

        /// <summary>
        /// Original ID of the object from the prefab.
        /// </summary>
        public string OriginalID { get; set; }

        /// <summary>
        /// Prefab reference ID.
        /// </summary>
        public string PrefabID { get; set; }

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string PrefabInstanceID { get; set; }

        /// <summary>
        /// If the object was spawned from a prefab.
        /// </summary>
        public bool FromPrefab { get; set; }

        /// <summary>
        /// Gets the runtime object for the prefabbed object.
        /// </summary>
        /// <returns>Returns the runtime object of the object.</returns>
        public IRTObject GetRuntimeObject() => null;

        /// <summary>
        /// Runtime object reference.
        /// </summary>
        public BackgroundLayerObject runtimeObject;

        #region Methods

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

            return jn;
        }

        public static BackgroundLayer GetDefault(int i) => new BackgroundLayer(100 * (i + 1), 1 * (i + 1));

        #endregion
    }
}
