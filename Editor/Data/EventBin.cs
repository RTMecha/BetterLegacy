using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a bin (row) for a list of keyframes.
    /// </summary>
    public class EventBin : Exists
    {
        public EventBin() { }

        /// <summary>
        /// Name of the bin.
        /// </summary>
        public string name;

        /// <summary>
        /// Index of the event bin.
        /// </summary>
        public int index;

        /// <summary>
        /// Path of the complexity value in the complexity.json file.
        /// </summary>
        public string complexityPath;

        /// <summary>
        /// Checks if the event bin should display.
        /// </summary>
        public bool IsActive => EditorHelper.CheckComplexity(EditorHelper.GetComplexity(complexityPath, index >= 10 ? Complexity.Advanced : Complexity.Simple));

        public override string ToString() => $"[{index}] {name}";
    }
}
