using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Provides a reference to the markers' button in the marker list.
    /// </summary>
    public class MarkerPanel : Exists
    {
        public MarkerPanel(TimelineMarker timelineMarker) => this.timelineMarker = timelineMarker;

        #region Values

        readonly TimelineMarker timelineMarker;

        /// <summary>
        /// GameObject of the marker button.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Name text of the marker button.
        /// </summary>
        public Text Name { get; set; }

        /// <summary>
        /// Time text of the marker button.
        /// </summary>
        public Text Time { get; set; }

        /// <summary>
        /// Color image of the marker button.
        /// </summary>
        public Image Color { get; set; }

        /// <summary>
        /// Button component of the marker button.
        /// </summary>
        public Button Button { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Clears all <see cref="MarkerButton"/> data.
        /// </summary>
        public void Clear()
        {
            GameObject = null;
            Name = null;
            Time = null;
            Color = null;
            Button = null;
        }

        /// <summary>
        /// Renders the marker button color.
        /// </summary>
        public void RenderColor() => RenderColor(timelineMarker.Color);

        /// <summary>
        /// Renders the marker button color.
        /// </summary>
        /// <param name="color">Color to set.</param>
        public void RenderColor(Color color) => Color.color = color;

        /// <summary>
        /// Renders the marker button name.
        /// </summary>
        public void RenderName() => RenderName(timelineMarker.Marker.name);

        /// <summary>
        /// Renders the marker button name.
        /// </summary>
        /// <param name="name">Name to set.</param>
        public void RenderName(string name) => Name.text = name;

        /// <summary>
        /// Renders the marker button time.
        /// </summary>
        public void RenderTime() => RenderTime(timelineMarker.Marker.time);

        /// <summary>
        /// Renders the marker button time.
        /// </summary>
        /// <param name="time">Time to set.</param>
        public void RenderTime(float time) => Time.text = RTString.SecondsToTime(time);

        #endregion
    }
}
