using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class CachedSequences : Exists
    {
        public Sequence<Vector3> PositionSequence { get; set; }
        public Sequence<Vector2> ScaleSequence { get; set; }
        public Sequence<Vector3> RotationSequence { get; set; }
        public Sequence<Color> ColorSequence { get; set; }
        public Sequence<Color> SecondaryColorSequence { get; set; }
    }
}
