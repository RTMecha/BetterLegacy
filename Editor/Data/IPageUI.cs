
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Indicates a UI that has pagination.
    /// </summary>
    public interface IPageUI
    {
        /// <summary>
        /// Page input field.
        /// </summary>
        public InputFieldStorage PageField { get; set; }

        /// <summary>
        /// The current page.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Max amount of pages.
        /// </summary>
        public int MaxPageCount { get; }
    }
}
