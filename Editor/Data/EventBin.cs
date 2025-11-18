using BetterLegacy.Core.Data;

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
        /// Index of the event.
        /// </summary>
        public int index;

        public override string ToString() => $"[{index}] {name}";
    }
}
