using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents an editor layer that has been pinned by the user.
    /// </summary>
    public class PinnedEditorLayer : Exists
    {
        /// <summary>
        /// The editor layer.
        /// </summary>
        public int layer;
        /// <summary>
        /// Name of the pinned editor layer.
        /// </summary>
        public string name;
        /// <summary>
        /// Description of the editor layer.
        /// </summary>
        public string desc;
        /// <summary>
        /// If <see cref="color"/> should be used instead of the global editor colors.
        /// </summary>
        public bool overrideColor;
        /// <summary>
        /// Color to override the editor layer with.
        /// </summary>
        public Color color;

        /// <summary>
        /// Creates a copy of a pinned editor layer.
        /// </summary>
        /// <param name="orig">Pinned editor layer to copy.</param>
        /// <returns>Returns a new pinned editor layer with the same values as the original.</returns>
        public static PinnedEditorLayer DeepCopy(PinnedEditorLayer orig) => new PinnedEditorLayer
        {
            layer = orig.layer,
            name = orig.name,
            desc = orig.desc,
            overrideColor = orig.overrideColor,
            color = orig.color,
        };

        /// <summary>
        /// Parses a pinned editor layer from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed pinned editor layer.</returns>
        public static PinnedEditorLayer Parse(JSONNode jn) => new PinnedEditorLayer
        {
            layer = jn["layer"].AsInt,
            name = jn["name"],
            desc = jn["desc"],
            overrideColor = jn["col"] != null,
            color = LSColors.HexToColor(jn["col"]),
        };

        /// <summary>
        /// Writes the pinned editor layer to JSON.
        /// </summary>
        /// <returns>Returns JSON representing the pinned editor layer.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["layer"] = layer;
            jn["name"] = name;
            jn["desc"] = desc;
            if (overrideColor)
                jn["col"] = RTColors.ColorToHexOptional(color);
            return jn;
        }
    }
}
