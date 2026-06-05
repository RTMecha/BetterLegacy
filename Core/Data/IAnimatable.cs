using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object has a set of keyframes that animate the object.
    /// </summary>
    public interface IAnimatable
    {
        #region Values

        /// <summary>
        /// ID of the animatable.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Animation events.
        /// </summary>
        public List<List<EventKeyframe>> Events { get; set; }

        /// <summary>
        /// The full length of the animation.
        /// </summary>
        public float AnimLength { get; }

        /// <summary>
        /// Animation start time.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// List of timeline keyframes in the editor.
        /// </summary>
        public List<TimelineKeyframe> TimelineKeyframes { get; set; }

        /// <summary>
        /// Data for the object in the editor.
        /// </summary>
        public ObjectEditorData EditorData { get; set; }

        #endregion

        #region Functions

        /// <summary>
        /// Gets the list of event keyframes.
        /// </summary>
        /// <param name="type">Type of list to get.</param>
        /// <returns>Returns the list of keyframes based on the type.</returns>
        public List<EventKeyframe> GetEventKeyframes(int type);

        /// <summary>
        /// Sets the list of event keyframes.
        /// </summary>
        /// <param name="type">Type of list to set.</param>
        /// <param name="eventKeyframes">List of keyframes to set based on the type.</param>
        public void SetEventKeyframes(int type, List<EventKeyframe> eventKeyframes);

        #endregion
    }
}
