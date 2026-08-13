using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class ParentObject : Exists
    {
        #region Constructors

        public ParentObject(GameObject gameObject, BeatmapObject beatmapObject)
        {
            this.beatmapObject = beatmapObject;
            this.gameObject = gameObject;
            transform = gameObject.transform;
            poolObject = gameObject.GetComponent<Pool.PoolObject>();

            positionSequence = beatmapObject.cachedSequences?.PositionSequence ?? new Sequence<Vector3>(new List<IKeyframe<Vector3>>
            {
                new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
            });
            scaleSequence = beatmapObject.cachedSequences?.ScaleSequence ?? new Sequence<Vector3>(new List<IKeyframe<Vector3>>
            {
                new Vector3Keyframe(0f, Vector3.one, Ease.Linear),
            });
            rotationSequence = beatmapObject.cachedSequences?.RotationSequence ?? new Sequence<Vector3>(new List<IKeyframe<Vector3>>
            {
                new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
            });

            parentAnimatePosition = beatmapObject.GetParentType(0);
            parentAnimateScale = beatmapObject.GetParentType(1);
            parentAnimateRotation = beatmapObject.GetParentType(2);

            parentOffsetPosition = beatmapObject.parentOffsets[0];
            parentOffsetScale = beatmapObject.parentOffsets[1];
            parentOffsetRotation = beatmapObject.parentOffsets[2];

            parentAdditivePosition = beatmapObject.GetParentAdditive(0);
            parentAdditiveScale = beatmapObject.GetParentAdditive(1);
            parentAdditiveRotation = beatmapObject.GetParentAdditive(2);

            parentParallaxPosition = beatmapObject.parallaxSettings[0];
            parentParallaxScale = beatmapObject.parallaxSettings[1];
            parentParallaxRotation = beatmapObject.parallaxSettings[2];

            boneLength = beatmapObject.boneLength;

            id = beatmapObject.id;
            desync = !string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObject.desync;
        }

        #endregion

        #region Values

        public const int DEFAULT_PARENT_CHAIN_CAPACITY = 30;

        public float boneLength;

        public Sequence<Vector3> positionSequence;
        public Sequence<Vector3> scaleSequence;
        public Sequence<Vector3> rotationSequence;

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

        public Pool.PoolObject poolObject;
        public GameObject gameObject;
        public Transform transform;
        public string id;
        public bool desync;
        public float desyncOffset;
        public bool spawned;
        public bool animate = true;

        public BeatmapObject beatmapObject;
        public PrefabObject prefabObject;

        #endregion

        #region Functions

        public void Clear()
        {
            if (poolObject)
            {
                poolObject.Return();
                poolObject = null;
            }
            else
                CoreHelper.Delete(gameObject);
            beatmapObject = null;
            gameObject = null;
            id = null;
            positionSequence = null;
            scaleSequence = null;
            rotationSequence = null;
        }

        public override string ToString() => beatmapObject?.ToString() ?? string.Empty;

        #endregion
    }
}
