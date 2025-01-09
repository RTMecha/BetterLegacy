using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization.Objects.Visual;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects
{
    public class LevelObject : Exists, ILevelObject
    {
        public float StartTime { get; set; }
        public float KillTime { get; set; }

        public string ID { get; }
        public BeatmapObject beatmapObject;

        public List<LevelParentObject> parentObjects;
        public VisualObject visualObject;
        public GradientObject gradientObject;
        
        public Sequence<Color> colorSequence;
        public Sequence<Color> secondaryColorSequence;
        public float depth;

        public bool cameraParent;
        public bool positionParent;
        public bool scaleParent;
        public bool rotationParent;

        public bool isImage;
        public bool isGradient;
        public float positionParentOffset;
        public float scaleParentOffset;
        public float rotationParentOffset;

        public Vector3 topPositionOffset;
        public Vector3 topScaleOffset;
        public Vector3 topRotationOffset;

        public Vector3 prefabOffsetPosition;
        public Vector3 prefabOffsetScale;
        public Vector3 prefabOffsetRotation;

        public Transform top;

        public void Clear()
        {
            if (parentObjects != null)
            {
                for (int i = 0; i < parentObjects.Count; i++)
                {
                    var parentObject = parentObjects[i];
                    parentObject.BeatmapObject = null;
                    parentObject.gameObject = null;
                    parentObject.id = null;
                    parentObject.position3DSequence = null;
                    parentObject.positionSequence = null;
                    parentObject.scaleSequence = null;
                    parentObject.rotationSequence = null;
                }
                parentObjects.Clear();
            }

            parentObjects = null;
            colorSequence = null;
            top = null;
            beatmapObject = null;

            if (visualObject != null)
            {
                visualObject.GameObject = null;
                visualObject.Collider = null;
                visualObject.Renderer = null;
            }
            visualObject = null;
        }

        public LevelObject(BeatmapObject beatmapObject, Sequence<Color> colorSequence, Sequence<Color> secondaryColorSequence, List<LevelParentObject> parentObjects, VisualObject visualObject,
            Vector3 prefabOffsetPosition, Vector3 prefabOffsetScale, Vector3 prefabOffsetRotation)
        {
            this.beatmapObject = beatmapObject;

            ID = beatmapObject.id;
            StartTime = beatmapObject.StartTime;
            KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
            depth = beatmapObject.depth;

            this.parentObjects = parentObjects;
            this.visualObject = visualObject;
            
            this.colorSequence = colorSequence;
            this.secondaryColorSequence = secondaryColorSequence;

            this.isGradient = this.secondaryColorSequence != null;
            if (isGradient)
                gradientObject = (GradientObject)visualObject;
            isImage = visualObject is ImageObject;

            this.prefabOffsetPosition = prefabOffsetPosition;
            this.prefabOffsetScale = prefabOffsetScale;
            this.prefabOffsetRotation = prefabOffsetRotation;

            desyncParentIndex = this.parentObjects.Count;

            try
            {
                top = this.parentObjects[this.parentObjects.Count - 1].transform.parent;

                var pc = beatmapObject.GetParentChain();

                if (pc != null && pc.Count > 0)
                {
                    var beatmapParent = pc[pc.Count - 1];

                    cameraParent = beatmapParent.parent == BeatmapObject.CAMERA_PARENT;

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
                Debug.LogError($"{Updater.className}a\n{ex}");
            }
        }

        bool active = false;
        public void SetActive(bool active)
        {
            if (parentObjects.Count > 0)
                parentObjects[parentObjects.Count - 1].gameObject?.SetActive(active);

            if (!active && this.active)
            {
                desyncParentIndex = parentObjects.Count;
                spawned = false;

                var parentObject = parentObjects[0];
                for (int j = 0; j < parentObject.position3DSequence.keyframes.Length; j++)
                    parentObject.position3DSequence.keyframes[j].Stop();
                for (int j = 0; j < parentObject.scaleSequence.keyframes.Length; j++)
                    parentObject.scaleSequence.keyframes[j].Stop();
                for (int j = 0; j < parentObject.rotationSequence.keyframes.Length; j++)
                    parentObject.rotationSequence.keyframes[j].Stop();
                for (int i = 0; i < colorSequence.keyframes.Length; i++)
                    colorSequence.keyframes[i].Stop();
            }

            this.active = active;
        }

        float prevStartTime = 0f;
        bool spawned = false;
        int desyncParentIndex;
        int syncParentIndex;

        Vector3 currentScale;

        void CheckCollision()
        {
            var active = RTMath.Distance(0f, currentScale.x) > 0.001f && RTMath.Distance(0f, currentScale.y) > 0.001f && RTMath.Distance(0f, currentScale.z) > 0.001f;

            if (visualObject.Collider)
                visualObject.Collider.enabled = visualObject.ColliderEnabled && active;
            if (visualObject.Renderer)
                visualObject.Renderer.enabled = active;
        }

        public void Interpolate(float time)
        {
            // Set visual object color
            if(!isGradient)
                visualObject.SetColor(colorSequence.Interpolate(time - StartTime));
            else
                gradientObject.SetColor(colorSequence.Interpolate(time - StartTime), secondaryColorSequence.Interpolate(time - StartTime));
            
            if (isImage)
                visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.depth * 0.1f)); // fixes origin being off.

            // Update Camera Parent
            if (positionParent && cameraParent)
            {
                var x = EventManager.inst.cam.transform.position.x;
                var y = EventManager.inst.cam.transform.position.y;

                top.localPosition = (new Vector3(x, y, 0f) * positionParentOffset)
                    + new Vector3(prefabOffsetPosition.x, prefabOffsetPosition.y, beatmapObject.background ? 20f : 0f)
                    + topPositionOffset;
            }
            else
                top.localPosition = new Vector3(prefabOffsetPosition.x, prefabOffsetPosition.y, beatmapObject.background ? 20f : 0f)
                    + topPositionOffset;

            if (scaleParent && cameraParent)
            {
                float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f - 1f;

                top.localScale = (new Vector3(camOrthoZoom, camOrthoZoom, 1f) * scaleParentOffset) + prefabOffsetScale + topScaleOffset;
            }
            else
                top.localScale = prefabOffsetScale + topScaleOffset;

            if (rotationParent && cameraParent)
            {
                var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                top.localRotation = Quaternion.Euler((camRot * rotationParentOffset) + prefabOffsetRotation + topRotationOffset);
            }
            else
                top.localRotation = Quaternion.Euler(prefabOffsetRotation + topRotationOffset);

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

            if (prevStartTime != beatmapObject.startTime)
            {
                prevStartTime = beatmapObject.startTime;
                spawned = false;
            }

            var totalScale = Vector3.one;
            int desyncParentIndex = 0;
            bool hasSpawned = false;
            for (int i = 0; i < parentObjects.Count; i++)
            {
                var parentObject = parentObjects[i];

                if (totalScale == Vector3.zero) // stop interpolating entire parent chain if any of the parents scale is zero due to scale being multiplied.
                    break;

                if (!spawned || i >= syncParentIndex && i < this.desyncParentIndex)
                {
                    if (parentObject.parentAdditivePosition)
                        positionAddedOffset += parentObject.parentOffsetPosition;
                    if (parentObject.parentAdditiveScale)
                        scaleAddedOffset += parentObject.parentOffsetScale;
                    if (parentObject.parentAdditiveRotation)
                        rotationAddedOffset += parentObject.parentOffsetRotation;

                    // If last parent is position parented, animate position
                    if (animatePosition)
                    {
                        if (parentObject.position3DSequence != null)
                        {
                            var value = parentObject.position3DSequence.Interpolate(time - parentObject.timeOffset - (positionOffset + positionAddedOffset)) + parentObject.BeatmapObject.reactivePositionOffset + parentObject.BeatmapObject.positionOffset;

                            float z = depth * 0.0005f + (value.z / 10f);

                            parentObject.transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, z);
                        }
                        else
                        {
                            var value = parentObject.positionSequence.Interpolate(time - parentObject.timeOffset - (positionOffset + positionAddedOffset));
                            parentObject.transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, depth * 0.0005f);
                        }
                    }

                    // If last parent is scale parented, animate scale
                    if (animateScale)
                    {
                        var r = parentObject.BeatmapObject.reactiveScaleOffset + parentObject.BeatmapObject.reactiveScaleOffset + parentObject.BeatmapObject.scaleOffset;
                        var value = parentObject.scaleSequence.Interpolate(time - parentObject.timeOffset - (scaleOffset + scaleAddedOffset)) + new Vector2(r.x, r.y);
                        var scale = new Vector3(value.x * scaleParallax, value.y * scaleParallax, 1.0f + parentObject.BeatmapObject.scaleOffset.z);
                        parentObject.transform.localScale = scale;
                        totalScale = RTMath.Multiply(totalScale, scale);
                    }

                    // If last parent is rotation parented, animate rotation
                    if (animateRotation)
                    {
                        var value = Quaternion.AngleAxis(
                            (parentObject.rotationSequence.Interpolate(time - parentObject.timeOffset - (rotationOffset + rotationAddedOffset)) + parentObject.BeatmapObject.reactiveRotationOffset) * rotationParallax,
                            Vector3.forward);
                        parentObject.transform.localRotation = Quaternion.Euler(value.eulerAngles + parentObject.BeatmapObject.rotationOffset);
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

                    if (!spawned)
                        syncParentIndex = i;

                    if (parentObject.desync && !spawned)
                    {
                        hasSpawned = true;
                        spawned = true;
                        desyncParentIndex = i + 1;
                    }
                }
            }

            currentScale = totalScale;

            if (hasSpawned)
                this.desyncParentIndex = desyncParentIndex;

            CheckCollision();
        }
    }
}