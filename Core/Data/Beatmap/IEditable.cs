using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be viewed in the Editor Timeline.
    /// </summary>
    public interface IEditable
    {
        /// <summary>
        /// ID of the editable.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Data for the object in the editor.
        /// </summary>
        public ObjectEditorData EditorData { get; set; }

        /// <summary>
        /// Timeline Object reference for the editor.
        /// </summary>
        public TimelineObject TimelineObject { get; set; }

        /// <summary>
        /// If the object can render in the editor timeline.
        /// </summary>
        public bool CanRenderInTimeline { get; }
    }
}
