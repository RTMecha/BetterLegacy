using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents an easing option for dropdowns.
    /// </summary>
    public class EasingOption : Exists
    {
        /// <summary>
        /// Display name of the easing.
        /// </summary>
        public string name;

        /// <summary>
        /// Ordinal of the easing.
        /// </summary>
        public int index;

        /// <summary>
        /// Icon of the easing.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// <see cref="Easing"/> value that this easing option represents.
        /// </summary>
        public Easing EasingValue => (Easing)index;
    }
}
