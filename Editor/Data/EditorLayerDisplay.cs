using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents the display for an editor layer as a toggle.
    /// </summary>
    public class EditorLayerDisplay : Exists
    {
        #region Constructors

        public EditorLayerDisplay() { }

        public EditorLayerDisplay(int layer, ThemeGroup themeGroup, string color)
        {
            this.layer = layer;
            this.themeGroup = themeGroup;
            this.color = color;
        }

        #endregion

        #region Values

        /// <summary>
        /// Layer the toggle represents.
        /// </summary>
        public int layer;

        /// <summary>
        /// Theme group of the toggle.
        /// </summary>
        public ThemeGroup themeGroup;

        /// <summary>
        /// Hex color of the toggle.
        /// </summary>
        public string color;

        #endregion
    }
}
