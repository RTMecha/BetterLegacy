using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a group of timeline objects in the editor.
    /// </summary>
    public class EditorGroup : PAObject<EditorGroup>
    {
        public EditorGroup() { }

        public EditorGroup(string name) => this.name = name;

        /// <summary>
        /// Name of the group.
        /// </summary>
        public string name;

        /// <summary>
        /// Timeline object collapse override.
        /// </summary>
        public CollapsedType collapsedType;

        int bin;
        /// <summary>
        /// Row the timeline object is at.
        /// </summary>
        public int Bin
        {
            get => bin;
            set => bin = value;
        }

        int layer;
        /// <summary>
        /// Layer the timeline object appears on.
        /// </summary>
        public int Layer
        {
            get => Mathf.Clamp(layer, 0, int.MaxValue);
            set => layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        /// <summary>
        /// Timeline object collapse type.
        /// </summary>
        public enum CollapsedType
        {
            /// <summary>
            /// Does not override timeline object.
            /// </summary>
            Off,
            /// <summary>
            /// Overrides timeline object collapse.
            /// </summary>
            Collapsed,
            /// <summary>
            /// Hides the timeline object.
            /// </summary>
            Hidden,
        }

        public override void CopyData(EditorGroup orig, bool newID = true)
        {
            name = orig.name;
            collapsedType = orig.collapsedType;
            Bin = orig.Bin;
            Layer = orig.Layer;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["name"] != null)
                name = jn["name"];
            if (jn["collapse_type"] != null)
                collapsedType = (CollapsedType)jn["collapse_type"].AsInt;
            Bin = jn["bin"].AsInt;
            Layer = jn["layer"].AsInt;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (collapsedType != CollapsedType.Off)
                jn["collapse_type"] = (int)collapsedType;
            if (Bin != 0)
                jn["bin"] = Bin;
            if (Layer != 0)
                jn["layer"] = Layer;

            return jn;
        }
    }
}
