using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Indicates a runtime object includes a parent chain.
    /// </summary>
    public interface IParentChain
    {
        /// <summary>
        /// Depth offset of the object.
        /// </summary>
        public float Depth { get; set; }

        /// <summary>
        /// If the object is parented to the camera.
        /// </summary>
        public bool CameraParent { get; set; }

        /// <summary>
        /// If the position of a parent is applied.
        /// </summary>
        public bool PositionParent { get; set; }

        /// <summary>
        /// If the scale of a parent is applied.
        /// </summary>
        public bool ScaleParent { get; set; }

        /// <summary>
        /// If the rotation of a parent is applied.
        /// </summary>
        public bool RotationParent { get; set; }

        /// <summary>
        /// Position parent parallax.
        /// </summary>
        public float PositionParentOffset { get; set; }

        /// <summary>
        /// Scale parent parallax.
        /// </summary>
        public float ScaleParentOffset { get; set; }

        /// <summary>
        /// Rotation parent parallax.
        /// </summary>
        public float RotationParentOffset { get; set; }

        /// <summary>
        /// Top position offset.
        /// </summary>
        public Vector3 TopPosition { get; set; }

        /// <summary>
        /// Top scale offset.
        /// </summary>
        public Vector3 TopScale { get; set; }

        /// <summary>
        /// Top rotation offset.
        /// </summary>
        public Vector3 TopRotation { get; set; }

        /// <summary>
        /// Parent of the runtime object.
        /// </summary>
        public Transform Parent { get; set; }

        /// <summary>
        /// The current scale of the object. If scale is 0, 0 then the objects collider and renderer will be disabled.
        /// </summary>
        public Vector3 CurrentScale { get; set; } // if scale is 0, 0 then disable collider and renderer

        /// <summary>
        /// List of parent objects.
        /// </summary>
        public List<ParentObject> ParentObjects { get; set; }

        /// <summary>
        /// Checks the scale of the parent chain.
        /// </summary>
        public void CheckScale();
    }

    public static class ParentChainExtension
    {
        /// <summary>
        /// Sets the parent of a parent chain.
        /// </summary>
        /// <param name="parent">New parent to set.</param>
        public static void SetParent(this IParentChain parentChain, Transform parent) => parentChain.Parent?.SetParent(parent);

        /// <summary>
        /// Updates the camera parent.
        /// </summary>
        /// <param name="parentChain">Parent chain reference.</param>
        /// <param name="inBackground">If the object renders in the background.</param>
        public static void UpdateCameraParent(this IParentChain parentChain, bool inBackground = false)
        {
            if (!parentChain.CameraParent)
            {
                parentChain.Parent.localPosition = new Vector3(0f, 0f, inBackground ? 20f : 0f) + parentChain.TopPosition;
                parentChain.Parent.localScale = parentChain.TopScale;
                parentChain.Parent.localRotation = Quaternion.Euler(parentChain.TopRotation);
                return;
            }

            // Update Camera Parent
            if (parentChain.PositionParent)
            {
                var pos = EventManager.inst.cam.transform.position;
                parentChain.Parent.localPosition = (new Vector3(pos.x, pos.y, 0f) * parentChain.PositionParentOffset)
                    + new Vector3(0f, 0f, inBackground ? 20f : 0f)
                    + parentChain.TopPosition;
            }
            else
                parentChain.Parent.localPosition = new Vector3(0f, 0f, inBackground ? 20f : 0f) + parentChain.TopPosition;

            if (parentChain.ScaleParent)
            {
                float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f - 1f;
                parentChain.Parent.localScale = RTMath.Scale((new Vector3(camOrthoZoom, camOrthoZoom, 1f) * parentChain.ScaleParentOffset) + Vector3.one, parentChain.TopScale);
            }
            else
                parentChain.Parent.localScale = parentChain.TopScale;

            if (parentChain.RotationParent)
            {
                var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;
                parentChain.Parent.localRotation = Quaternion.Euler((camRot * parentChain.RotationParentOffset) + parentChain.TopRotation);
            }
            else
                parentChain.Parent.localRotation = Quaternion.Euler(parentChain.TopRotation);
        }

        /// <summary>
        /// Interpolates the parent chain.
        /// </summary>
        /// <param name="parentChain">Parent chain reference.</param>
        /// <param name="time">Time to interpolate.</param>
        /// <param name="fromPrefab">If the object is from a prefab.</param>
        public static void InterpolateParentChain(this IParentChain parentChain, float time, bool fromPrefab = false, bool isPrefab = false)
        {
            // Update parents
            float positionOffset = 0.0f;
            float scaleOffset = 0.0f;
            float rotationOffset = 0.0f;

            float positionAddedOffset = 0.0f;
            float scaleAddedOffset = 0.0f;
            float rotationAddedOffset = 0.0f;

            bool animatePosition = true;
            bool animateScale = true;
            bool animateRotation = true;

            float positionParallax = 1f;
            float scaleParallax = 1f;
            float rotationParallax = 1f;

            bool desync = false;
            float syncOffset = 0f;
            float prefabOffset = 0f;

            var totalScale = Vector3.one;

            for (int i = 0; i < parentChain.ParentObjects.Count; i++)
            {
                var localTime = time;
                var parentObject = parentChain.ParentObjects[i];
                var timeOffset = parentObject.beatmapObject.StartTime;

                if (totalScale == Vector3.zero) // stop interpolating entire parent chain if any of the parents scale is zero due to scale being multiplied.
                    break;

                if (parentObject.spawned && desync) // continue if parent has desync setting on and was spawned.
                    continue;

                if (parentObject.beatmapObject.detatched && desync) // for modifier use
                    continue;

                if (!isPrefab && fromPrefab && !parentObject.beatmapObject.fromPrefab)
                    localTime = RTLevel.Current.CurrentTime;
                if (parentObject.beatmapObject.fromPrefab)
                    prefabOffset = 0f;

                parentObject.spawned = true;

                if (parentObject.parentAdditivePosition)
                    positionAddedOffset += parentObject.parentOffsetPosition;
                if (parentObject.parentAdditiveScale)
                    scaleAddedOffset += parentObject.parentOffsetScale;
                if (parentObject.parentAdditiveRotation)
                    rotationAddedOffset += parentObject.parentOffsetRotation;

                // If last parent is position parented, animate position
                if (animatePosition)
                {
                    var value =
                        parentObject.positionSequence.GetValue(desync ? syncOffset + prefabOffset - timeOffset - (positionOffset + positionAddedOffset) : localTime - timeOffset - (positionOffset + positionAddedOffset)) +
                        parentObject.beatmapObject.reactivePositionOffset + parentObject.beatmapObject.PositionOffset + parentObject.beatmapObject.fullTransform.position;

                    float z = parentChain.Depth * BeatmapObject.DEPTH_MULTIPLY + (value.z / 10f);

                    parentObject.transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, z);
                }

                // If last parent is scale parented, animate scale
                if (animateScale)
                {
                    var r = parentObject.beatmapObject.reactiveScaleOffset + parentObject.beatmapObject.scaleOffset;
                    var value = parentObject.scaleSequence.GetValue(desync ? syncOffset + prefabOffset - timeOffset - (scaleOffset + scaleAddedOffset) : localTime - timeOffset - (scaleOffset + scaleAddedOffset)) + new Vector2(r.x, r.y);
                    var scale = RTMath.Scale(new Vector3(value.x * scaleParallax, value.y * scaleParallax, 1.0f + r.z), parentObject.beatmapObject.fullTransform.scale);
                    parentObject.transform.localScale = scale;
                    totalScale = RTMath.Scale(totalScale, scale);
                }

                // If last parent is rotation parented, animate rotation
                if (animateRotation)
                {
                    var value = Quaternion.AngleAxis(
                        (parentObject.rotationSequence.GetValue(desync ? syncOffset + prefabOffset - timeOffset - (rotationOffset + rotationAddedOffset) : localTime - timeOffset - (rotationOffset + rotationAddedOffset)) + parentObject.beatmapObject.reactiveRotationOffset) * rotationParallax,
                        Vector3.forward);
                    parentObject.transform.localRotation = Quaternion.Euler(value.eulerAngles + parentObject.beatmapObject.rotationOffset + parentObject.beatmapObject.fullTransform.rotation);
                }

                // Cache parent values to use for next parent
                positionOffset = parentObject.parentOffsetPosition;
                scaleOffset = parentObject.parentOffsetScale;
                rotationOffset = parentObject.parentOffsetRotation;

                animatePosition = parentObject.parentAnimatePosition;
                animateScale = parentObject.parentAnimateScale;
                animateRotation = parentObject.parentAnimateRotation;

                positionParallax = parentObject.parentParallaxPosition;
                scaleParallax = parentObject.parentParallaxScale;
                rotationParallax = parentObject.parentParallaxRotation;

                if (desync) // don't reset desync as the intention is the object "detatches" itself from the parent object.
                    continue;

                desync = parentObject.desync || parentObject.beatmapObject.detatched;
                syncOffset = timeOffset + parentObject.desyncOffset;
                if (parentObject.beatmapObject.fromPrefab)
                    prefabOffset = parentObject.beatmapObject.GetPrefabOffsetTime();
            }

            parentChain.CurrentScale = totalScale;

            parentChain.CheckScale();
        }

        /// <summary>
        /// Updates the parent values.
        /// </summary>
        public static void UpdateParentValues(this IParentChain parentChain)
        {
            var firstParent = parentChain.ParentObjects[parentChain.ParentObjects.Count - 1];
            parentChain.Parent = firstParent.transform.parent;

            var beatmapParent = firstParent.beatmapObject;

            parentChain.CameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

            parentChain.PositionParent = beatmapParent.GetParentType(0);
            parentChain.ScaleParent = beatmapParent.GetParentType(1);
            parentChain.RotationParent = beatmapParent.GetParentType(2);

            parentChain.PositionParentOffset = beatmapParent.parallaxSettings[0];
            parentChain.ScaleParentOffset = beatmapParent.parallaxSettings[1];
            parentChain.RotationParentOffset = beatmapParent.parallaxSettings[2];
        }
    }
}
