using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class ParentObject : Exists
    {
        public ParentObject() { }

        public const int DEFAULT_PARENT_CHAIN_CAPACITY = 30;

        public Sequence<Vector3> positionSequence;
        public Sequence<Vector2> scaleSequence;
        public Sequence<float> rotationSequence;

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
        public float desyncOffset;
        public bool spawned;

        public BeatmapObject beatmapObject;
    }
}
