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
        /// <summary>
        /// Animation events.
        /// </summary>
        public List<List<EventKeyframe>> Events { get; }

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

        public ObjectEditorData EditorData { get; set; }

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

        /// <summary>
        /// Interpolates an animation from the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="valueIndex">Axis index to interpolate.</param>
        /// <param name="time">Time to interpolate to.</param>
        /// <returns>Returns a single value based on the event.</returns>
        public float Interpolate(int type, int valueIndex, float time);

        public float Interpolate(EventKeyframe prevKeyframe, EventKeyframe nextKeyframe, int type, int valueIndex, float time);

        public void SortKeyframes();

        public void SortKeyframes(List<List<EventKeyframe>> events);

        public void SortKeyframes(int type);

        public void SortKeyframes(List<EventKeyframe> eventKeyframes);
    }
}
