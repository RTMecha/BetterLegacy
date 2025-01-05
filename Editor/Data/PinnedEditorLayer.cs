using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using UnityEngine;

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
                jn["col"] = CoreHelper.ColorToHexOptional(color);
            return jn;
        }
    }
}
