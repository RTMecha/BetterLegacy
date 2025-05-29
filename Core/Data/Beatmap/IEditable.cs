using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be viewed in the Editor Timeline.
    /// </summary>
    public interface IEditable
    {
        /// <summary>
        /// Data for the object in the editor.
        /// </summary>
        public ObjectEditorData EditorData { get; set; }

        /// <summary>
        /// Timeline Object reference for the editor.
        /// </summary>
        public TimelineObject TimelineObject { get; set; }
    }
}
