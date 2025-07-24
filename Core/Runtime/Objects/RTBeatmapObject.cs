using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTBeatmapObject : Exists, IRTObject, ICustomActivatable
    {
        public RTBeatmapObject(BeatmapObject beatmapObject, List<ParentObject> parentObjects, VisualObject visualObject)
        {
            this.beatmapObject = beatmapObject;

            StartTime = beatmapObject.StartTime;
            KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
            depth = beatmapObject.Depth;

            this.parentObjects = parentObjects;
            this.visualObject = visualObject;

            isImage = beatmapObject.ShapeType == ShapeType.Image;

            try
            {
                top = this.parentObjects[this.parentObjects.Count - 1].transform.parent;

                var pc = beatmapObject.GetParentChain();

                if (pc != null && !pc.IsEmpty())
                {
                    var beatmapParent = pc[pc.Count - 1];

                    cameraParent = beatmapParent.Parent == BeatmapObject.CAMERA_PARENT;

                    positionParent = beatmapParent.GetParentType(0);
                    scaleParent = beatmapParent.GetParentType(1);
                    rotationParent = beatmapParent.GetParentType(2);

                    positionParentOffset = beatmapParent.parallaxSettings[0];
                    scaleParentOffset = beatmapParent.parallaxSettings[1];
                    rotationParentOffset = beatmapParent.parallaxSettings[2];
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{RTLevel.className}a\n{ex}");
            }
        }

        #region Values

        public float StartTime { get; set; }
        public float KillTime { get; set; }

        public BeatmapObject beatmapObject;

        public List<ParentObject> parentObjects;
        public VisualObject visualObject;
        
        public float depth;

        public bool cameraParent;
        public bool positionParent;
        public bool scaleParent;
        public bool rotationParent;

        public bool isImage;
        public float positionParentOffset;
        public float scaleParentOffset;
        public float rotationParentOffset;

        public Vector3 topPositionOffset;
        public Vector3 topScaleOffset;
        public Vector3 topRotationOffset;

        public Transform top;

        #region Internal

        bool active = false;

        Vector3 currentScale; // if scale is 0, 0 then disable collider and renderer

        #endregion

        #endregion

        #region Methods

        public void Clear()
        {
            if (parentObjects != null)
            {
                for (int i = 0; i < parentObjects.Count; i++)
                {
                    var parentObject = parentObjects[i];
                    parentObject.beatmapObject = null;
                    parentObject.gameObject = null;
                    parentObject.id = null;
                    parentObject.positionSequence = null;
                    parentObject.scaleSequence = null;
                    parentObject.rotationSequence = null;
                }
                parentObjects.Clear();
            }

            if (top)
                Helpers.CoreHelper.Destroy(top.gameObject);

            parentObjects = null;
            top = null;
            beatmapObject = null;

            visualObject?.Clear();
            visualObject = null;
        }

        public void SetActive(bool active)
        {
            if (!parentObjects.IsEmpty())
                parentObjects[parentObjects.Count - 1].gameObject?.SetActive(active);

            if (active && !this.active)
                UpdateSpawning();

            // original method
            //if (!active && this.active)
            //    UpdateSpawning();

            this.active = active;
        }

        void UpdateSpawning()
        {
            // stop objects' own keyframes due to stopping parent chain keyframes causing the homing keyframes to bug out.
            var parentObject = parentObjects[0];
            for (int j = 0; j < parentObject.positionSequence.keyframes.Length; j++)
                parentObject.positionSequence.keyframes[j].Stop();
            for (int j = 0; j < parentObject.scaleSequence.keyframes.Length; j++)
                parentObject.scaleSequence.keyframes[j].Stop();
            for (int j = 0; j < parentObject.rotationSequence.keyframes.Length; j++)
                parentObject.rotationSequence.keyframes[j].Stop();

            // despawn entire parent chain to get desync to work
            for (int i = 0; i < parentObjects.Count; i++)
                parentObjects[i].spawned = false;

            // this is what was originally done. however this caused problems as said above.
            //for (int i = 0; i < parentObjects.Count; i++)
            //{
            //    var parentObject = parentObjects[i];
            //    parentObject.spawned = false;
            //    for (int j = 0; j < parentObject.positionSequence.keyframes.Length; j++)
            //        parentObject.positionSequence.keyframes[j].Stop();
            //    for (int j = 0; j < parentObject.scaleSequence.keyframes.Length; j++)
            //        parentObject.scaleSequence.keyframes[j].Stop();
            //    for (int j = 0; j < parentObject.rotationSequence.keyframes.Length; j++)
            //        parentObject.rotationSequence.keyframes[j].Stop();
            //}

            for (int i = 0; i < visualObject.colorSequence.keyframes.Length; i++)
                visualObject.colorSequence.keyframes[i].Stop();
        }

        /// <summary>
        /// If the object is active.
        /// </summary>
        public bool topActive = true;

        /// <summary>
        /// Sets the top object active.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetCustomActive(bool active)
        {
            if (!top)
                return;

            top.gameObject.SetActive(active);
            topActive = active;
        }

        public void Interpolate(float time)
        {
            if (!topActive)
                return;

            if (!top)
                return;

            // Set visual object color
            visualObject.InterpolateColor(time - StartTime);

            if (isImage)
                visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f)); // fixes origin being off.

            // Update Camera Parent
            if (positionParent && cameraParent)
            {
                var x = EventManager.inst.cam.transform.position.x;
                var y = EventManager.inst.cam.transform.position.y;

                top.localPosition = (new Vector3(x, y, 0f) * positionParentOffset)
                    + new Vector3(0f, 0f, beatmapObject.renderLayerType == BeatmapObject.RenderLayerType.Background ? 20f : 0f)
                    + topPositionOffset;
            }
            else
                top.localPosition = new Vector3(0f, 0f, beatmapObject.renderLayerType == BeatmapObject.RenderLayerType.Background ? 20f : 0f)
                    + topPositionOffset;

            if (scaleParent && cameraParent)
            {
                float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f - 1f;

                top.localScale = (new Vector3(camOrthoZoom, camOrthoZoom, 1f) * scaleParentOffset) + Vector3.one + topScaleOffset;
            }
            else
                top.localScale = Vector3.one + topScaleOffset;

            if (rotationParent && cameraParent)
            {
                var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                top.localRotation = Quaternion.Euler((camRot * rotationParentOffset) + topRotationOffset);
            }
            else
                top.localRotation = Quaternion.Euler(topRotationOffset);

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

            for (int i = 0; i < parentObjects.Count; i++)
            {
                var parentObject = parentObjects[i];
                var timeOffset = parentObject.timeOffset;

                if (totalScale == Vector3.zero) // stop interpolating entire parent chain if any of the parents scale is zero due to scale being multiplied.
                    break;

                if (parentObject.spawned && desync) // continue if parent has desync setting on and was spawned.
                    continue;

                if (parentObject.beatmapObject.detatched && desync) // for modifier use, probably
                    continue;

                if (beatmapObject.fromPrefab && !parentObject.beatmapObject.fromPrefab)
                {
                    var prefab = beatmapObject.GetPrefab();
                    var prefabObject = beatmapObject.GetPrefabObject();
                    if (prefab && prefabObject)
                        timeOffset -= (prefabObject.StartTime * prefabObject.Speed) + prefab.offset;
                }

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
                        parentObject.positionSequence.Interpolate(desync ? syncOffset - timeOffset - (positionOffset + positionAddedOffset) : time - timeOffset - (positionOffset + positionAddedOffset)) +
                        parentObject.beatmapObject.reactivePositionOffset +
                        parentObject.beatmapObject.positionOffset;

                    float z = depth * 0.0005f + (value.z / 10f);

                    parentObject.transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, z);
                }

                // If last parent is scale parented, animate scale
                if (animateScale)
                {
                    var r = parentObject.beatmapObject.reactiveScaleOffset + parentObject.beatmapObject.scaleOffset;
                    var value = parentObject.scaleSequence.Interpolate(desync ? syncOffset - timeOffset - (scaleOffset + scaleAddedOffset) : time - timeOffset - (scaleOffset + scaleAddedOffset)) + new Vector2(r.x, r.y);
                    var scale = new Vector3(value.x * scaleParallax, value.y * scaleParallax, 1.0f + parentObject.beatmapObject.scaleOffset.z);
                    parentObject.transform.localScale = scale;
                    totalScale = RTMath.Scale(totalScale, scale);
                }

                // If last parent is rotation parented, animate rotation
                if (animateRotation)
                {
                    var value = Quaternion.AngleAxis(
                        (parentObject.rotationSequence.Interpolate(desync ? syncOffset - timeOffset - (rotationOffset + rotationAddedOffset) : time - timeOffset - (rotationOffset + rotationAddedOffset)) + parentObject.beatmapObject.reactiveRotationOffset) * rotationParallax,
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
            }

            currentScale = totalScale;

            CheckCollision();
        }

        void CheckCollision()
        {
            var active = RTMath.Distance(0f, currentScale.x) > 0.001f && RTMath.Distance(0f, currentScale.y) > 0.001f && RTMath.Distance(0f, currentScale.z) > 0.001f;

            if (visualObject.collider)
                visualObject.collider.enabled = visualObject.colliderEnabled && active;
            if (visualObject.renderer)
                visualObject.renderer.enabled = active;
        }

        #endregion
    }
}