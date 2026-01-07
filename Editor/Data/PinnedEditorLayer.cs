using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents an editor layer that has been pinned by the user.
    /// </summary>
    public class PinnedEditorLayer : PAObject<PinnedEditorLayer>
    {
        #region Constructors

        public PinnedEditorLayer() : base() { }

        public PinnedEditorLayer(int layer, EditorTimeline.LayerType layerType) : this()
        {
            this.layer = layer;
            this.layerType = layerType;
        }

        #endregion

        #region Values

        /// <summary>
        /// The editor layer.
        /// </summary>
        public int layer;

        /// <summary>
        /// The editor layer type.
        /// </summary>
        public EditorTimeline.LayerType layerType;

        /// <summary>
        /// Name of the pinned editor layer.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Description of the editor layer.
        /// </summary>
        public string description = string.Empty;

        /// <summary>
        /// If <see cref="color"/> should be used instead of the global editor colors.
        /// </summary>
        public bool overrideColor;

        /// <summary>
        /// Color to override the editor layer with.
        /// </summary>
        public Color color;

        #endregion

        #region Functions

        public override void CopyData(PinnedEditorLayer orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            layer = orig.layer;
            layerType = orig.layerType;
            name = orig.name;
            description = orig.description;
            overrideColor = orig.overrideColor;
            color = orig.color;
        }

        public override void ReadJSON(JSONNode jn)
        {
            layer = jn["layer"].AsInt;
            layerType = (EditorTimeline.LayerType)jn["layer_type"].AsInt;

            name = jn["name"] ?? string.Empty;
            description = jn["desc"] ?? string.Empty;
            overrideColor = jn["col"] != null;
            if (jn["col"] != null)
                color = RTColors.HexToColor(jn["col"]);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["layer"] = layer;
            if (layerType != EditorTimeline.LayerType.Objects)
                jn["layer_type"] = (int)layerType;

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            if (overrideColor)
                jn["col"] = RTColors.ColorToHexOptional(color);

            return jn;
        }

        /// <summary>
        /// Checks if the layer is the same as the pinned layer.
        /// </summary>
        /// <param name="layer">Editor layer.</param>
        /// <param name="layerType">Editor layer type.</param>
        /// <returns>Returns true if the layer is the same, otherwise returns false.</returns>
        public bool IsLayer(int layer, EditorTimeline.LayerType layerType) => this.layer == layer && this.layerType == layerType;

        #endregion
    }
}
