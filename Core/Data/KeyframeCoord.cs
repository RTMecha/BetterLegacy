using System;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a keyframes' location on a two-dimensional list.
    /// </summary>
    public struct KeyframeCoord : IEquatable<KeyframeCoord>
    {
        public KeyframeCoord(int type, int index)
        {
            this.type = type;
            this.index = index;
        }

        #region Values

        /// <summary>
        /// Type of the keyframe.
        /// </summary>
        public int type;

        /// <summary>
        /// Index of the keyframe.
        /// </summary>
        public int index;

        #endregion

        #region Functions

        public bool Equals(KeyframeCoord keyframeCoord) => Equals(keyframeCoord);

        public override string ToString() => $"{type}, {index}";

        public override bool Equals(object obj) => obj is KeyframeCoord keyframeCoord && keyframeCoord.type.Equals(type) && keyframeCoord.index.Equals(index);

        public override int GetHashCode() => CoreHelper.CombineHashCodes(type, index);

        public static bool operator ==(KeyframeCoord a, KeyframeCoord b) => a.Equals(b);

        public static bool operator !=(KeyframeCoord a, KeyframeCoord b) => !(a == b);

        #endregion
    }
}
