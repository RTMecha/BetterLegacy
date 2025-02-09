using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects
{
    public class LevelParentObject : Exists
    {
        public LevelParentObject() { }

        public Sequence<Vector2> positionSequence;
        public Sequence<Vector3> position3DSequence;
        public Sequence<Vector2> scaleSequence;
        public Sequence<float> rotationSequence;

        public float timeOffset;

        public bool parentAnimatePosition;
        public bool parentAnimateScale;
        public bool parentAnimateRotation;

        public float parentOffsetPosition;
        public float parentOffsetScale;
        public float parentOffsetRotation;

        public bool parentAdditivePosition;
        public bool parentAdditiveScale;
        public bool parentAdditiveRotation;

        public float parentParallaxPosition;
        public float parentParallaxScale;
        public float parentParallaxRotation;

        public GameObject gameObject;
        public Transform transform;
        public string id;
        public bool desync;
        public BeatmapObject BeatmapObject { get; set; }
    }
}
