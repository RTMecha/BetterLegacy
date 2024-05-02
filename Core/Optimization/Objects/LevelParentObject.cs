using BetterLegacy.Core.Animation;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects
{
    public class LevelParentObject : Exists
    {
        public bool Active { get; set; }

        public Sequence<Vector2> PositionSequence { get; set; }
        public Sequence<Vector3> Position3DSequence { get; set; }
        public Sequence<Vector2> ScaleSequence { get; set; }
        public Sequence<float> RotationSequence { get; set; }

        public float TimeOffset { get; set; }

        public bool ParentAnimatePosition { get; set; }
        public bool ParentAnimateScale { get; set; }
        public bool ParentAnimateRotation { get; set; }

        public float ParentOffsetPosition { get; set; }
        public float ParentOffsetScale { get; set; }
        public float ParentOffsetRotation { get; set; }

        public bool ParentAdditivePosition { get; set; }
        public bool ParentAdditiveScale { get; set; }
        public bool ParentAdditiveRotation { get; set; }

        public float ParentParallaxPosition { get; set; }
        public float ParentParallaxScale { get; set; }
        public float ParentParallaxRotation { get; set; }

        public GameObject GameObject { get; set; }
        public Transform Transform { get; set; }
        public string ID { get; set; }
        public Data.BeatmapObject BeatmapObject { get; set; }
    }
}
