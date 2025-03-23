using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents an editor UI with content.
    /// </summary>
    public interface IContentUI
    {
        /// <summary>
        /// Search field of the editor UI.
        /// </summary>
        InputField SearchField { get; set; }

        /// <summary>
        /// Content transform of the editor UI.
        /// </summary>
        Transform Content { get; set; }

        /// <summary>
        /// Grid layout of the editor UI's content.
        /// </summary>
        GridLayoutGroup Grid { get; set; }

        /// <summary>
        /// Scrollbar of the editor UI's content.
        /// </summary>
        Scrollbar ContentScrollbar { get; set; }

        /// <summary>
        /// Gets and sets the search input field text.
        /// </summary>
        string SearchTerm { get; set; }

        /// <summary>
        /// Clears the content from the editor UI.
        /// </summary>
        public void ClearContent();
    }
}
