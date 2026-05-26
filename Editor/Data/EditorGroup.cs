using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a group of timeline objects in the editor.
    /// </summary>
    public class EditorGroup : PAObject<EditorGroup>
    {
        #region Constructors

        public EditorGroup() { }

        public EditorGroup(string name) => this.name = name;

        #endregion

        #region Values

        /// <summary>
        /// Name of the group.
        /// </summary>
        public string name;

        /// <summary>
        /// Timeline object collapse override.
        /// </summary>
        public CollapsedType collapsedType;

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
        /// If tags should be used as well as the editor group value.
        /// </summary>
        public bool useTags;

        /// <summary>
        /// Prefab reference.
        /// </summary>
        public string prefabID;

        /// <summary>
        /// Prefab object reference.
        /// </summary>
        public string prefabInstanceID;

        #endregion

        #region Functions

        public override void CopyData(EditorGroup orig, bool newID = true)
        {
            name = orig.name;
            collapsedType = orig.collapsedType;
            Bin = orig.Bin;
            Layer = orig.Layer;
            useTags = orig.useTags;
            prefabID = orig.prefabID;
            prefabInstanceID = orig.prefabInstanceID;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["name"] != null)
                name = jn["name"];
            if (jn["collapse_type"] != null)
                collapsedType = (CollapsedType)jn["collapse_type"].AsInt;
            Bin = jn["bin"].AsInt;
            Layer = jn["layer"].AsInt;
            useTags = jn["use_tags"].AsBool;
            prefabID = jn["pid"];
            prefabInstanceID = jn["piid"];
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
            if (useTags)
                jn["use_tags"] = useTags;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pid"] = prefabID;
            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["piid"] = prefabInstanceID;

            return jn;
        }

        /// <summary>
        /// Checks if values matches the group name.
        /// </summary>
        /// <param name="name">Name to compare.</param>
        /// <param name="tags">Tags to check if <see cref="useTags"/> is true.</param>
        /// <returns>Returns <see langword="true"/> if the parameters match the current editor group.</returns>
        public bool Matches(string name, List<string> tags, IPrefabable prefabable) => (this.name == name || useTags && tags.Contains(this.name)) &&
            (string.IsNullOrEmpty(prefabID) || prefabable.PrefabID == prefabID) && (string.IsNullOrEmpty(prefabInstanceID) || prefabable.PrefabInstanceID == prefabInstanceID);

        #endregion
    }
}
