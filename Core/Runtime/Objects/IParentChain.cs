using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace BetterLegacy.Core.Runtime.Objects
{
    public interface IParentChain
    {
        public float Depth { get; set; }

        public bool CameraParent { get; set; }
        public bool PositionParent { get; set; }
        public bool ScaleParent { get; set; }
        public bool RotationParent { get; set; }

        public float PositionParentOffset { get; set; }
        public float ScaleParentOffset { get; set; }
        public float RotationParentOffset { get; set; }

        public Vector3 TopPositionOffset { get; set; }
        public Vector3 TopScaleOffset { get; set; }
        public Vector3 TopRotationOffset { get; set; }

        public Transform Parent { get; set; }

        public Vector3 CurrentScale { get; set; } // if scale is 0, 0 then disable collider and renderer

        public List<ParentObject> ParentObjects { get; set; }

        void CheckCollision();
    }

    public static class ParentChainExtension
    {
        public static void UpdateCameraParent(this IParentChain parentChain, bool inBackground = false)
        {
            if (!parentChain.CameraParent)
            {
                parentChain.Parent.localPosition = new Vector3(0f, 0f, inBackground ? 20f : 0f) + parentChain.TopPositionOffset;
                parentChain.Parent.localScale = Vector3.one + parentChain.TopScaleOffset;
                parentChain.Parent.localRotation = Quaternion.Euler(parentChain.TopRotationOffset);
                return;
            }

            // Update Camera Parent
            if (parentChain.PositionParent)
            {
                var x = EventManager.inst.cam.transform.position.x;
                var y = EventManager.inst.cam.transform.position.y;

                parentChain.Parent.localPosition = (new Vector3(x, y, 0f) * parentChain.PositionParentOffset)
                    + new Vector3(0f, 0f, inBackground ? 20f : 0f)
                    + parentChain.TopPositionOffset;
            }
            else
                parentChain.Parent.localPosition = new Vector3(0f, 0f, inBackground ? 20f : 0f) + parentChain.TopPositionOffset;

            if (parentChain.ScaleParent)
            {
                float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f - 1f;

                parentChain.Parent.localScale = (new Vector3(camOrthoZoom, camOrthoZoom, 1f) * parentChain.ScaleParentOffset) + Vector3.one + parentChain.TopScaleOffset;
            }
            else
                parentChain.Parent.localScale = Vector3.one + parentChain.TopScaleOffset;

            if (parentChain.RotationParent)
            {
                var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                parentChain.Parent.localRotation = Quaternion.Euler((camRot * parentChain.RotationParentOffset) + parentChain.TopRotationOffset);
            }
            else
                parentChain.Parent.localRotation = Quaternion.Euler(parentChain.TopRotationOffset);
        }

        public static void InterpolateParentChain(this IParentChain parentChain, float time, bool fromPrefab = false, bool inBackground = false)
        {
            UpdateCameraParent(parentChain, inBackground);

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

            var totalScale = Vector3.one;

            for (int i = 0; i < parentChain.ParentObjects.Count; i++)
            {
                var localTime = time;
                var parentObject = parentChain.ParentObjects[i];
                var timeOffset = parentObject.timeOffset;

                if (totalScale == Vector3.zero) // stop interpolating entire parent chain if any of the parents scale is zero due to scale being multiplied.
                    break;

                if (parentObject.spawned && desync) // continue if parent has desync setting on and was spawned.
                    continue;

                if (parentObject.beatmapObject.detatched && desync) // for modifier use, probably
                    continue;

                if (fromPrefab && !parentObject.beatmapObject.fromPrefab)
                    localTime = RTLevel.Current.CurrentTime;

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
                        parentObject.positionSequence.GetValue(desync ? syncOffset - timeOffset - (positionOffset + positionAddedOffset) : localTime - timeOffset - (positionOffset + positionAddedOffset)) +
                        parentObject.beatmapObject.reactivePositionOffset +
                        parentObject.beatmapObject.positionOffset;

                    float z = parentChain.Depth * 0.0005f + (value.z / 10f);

                    parentObject.transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, z);
                }

                // If last parent is scale parented, animate scale
                if (animateScale)
                {
                    var r = parentObject.beatmapObject.reactiveScaleOffset + parentObject.beatmapObject.scaleOffset;
                    var value = parentObject.scaleSequence.GetValue(desync ? syncOffset - timeOffset - (scaleOffset + scaleAddedOffset) : localTime - timeOffset - (scaleOffset + scaleAddedOffset)) + new Vector2(r.x, r.y);
                    var scale = new Vector3(value.x * scaleParallax, value.y * scaleParallax, 1.0f + parentObject.beatmapObject.scaleOffset.z);
                    parentObject.transform.localScale = scale;
                    totalScale = RTMath.Scale(totalScale, scale);
                }

                // If last parent is rotation parented, animate rotation
                if (animateRotation)
                {
                    var value = Quaternion.AngleAxis(
                        (parentObject.rotationSequence.GetValue(desync ? syncOffset - timeOffset - (rotationOffset + rotationAddedOffset) : localTime - timeOffset - (rotationOffset + rotationAddedOffset)) + parentObject.beatmapObject.reactiveRotationOffset) * rotationParallax,
                        Vector3.forward);
                    parentObject.transform.localRotation = Quaternion.Euler(value.eulerAngles + parentObject.beatmapObject.rotationOffset);
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
                    syncOffset += parentObject.beatmapObject.GetPrefabOffsetTime();
            }

            parentChain.CurrentScale = totalScale;

            parentChain.CheckCollision();
        }
    }
}
