using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Optimization.Objects.Visual;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects
{
    public class LevelObject : Exists, ILevelObject
    {
        public float StartTime { get; set; }
        public float KillTime { get; set; }

        public string ID { get; }
        Sequence<Color> colorSequence;
        Sequence<float> opacitySequence;
        Sequence<float> hueSequence;
        Sequence<float> satSequence;
        Sequence<float> valSequence;

        public float depth;
        public List<LevelParentObject> parentObjects;
        public VisualObject visualObject;

        public Data.BeatmapObject beatmapObject;

        public bool cameraParent;
        public bool positionParent;
        public bool scaleParent;
        public bool rotationParent;

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
                    parentObject.GameObject = null;
                    parentObject.ID = null;
                    parentObject.Position3DSequence = null;
                    parentObject.PositionSequence = null;
                    parentObject.ScaleSequence = null;
                    parentObject.RotationSequence = null;
                }
                parentObjects.Clear();
            }

            parentObjects = null;
            colorSequence = null;
            opacitySequence = null;
            hueSequence = null;
            satSequence = null;
            valSequence = null;
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

        public void SetSequences(Sequence<Color> colorSequence, Sequence<float> opacitySequence, Sequence<float> hueSequence, Sequence<float> satSequence, Sequence<float> valSequence)
        {
            this.colorSequence = colorSequence;
            this.opacitySequence = opacitySequence;
            this.hueSequence = hueSequence;
            this.satSequence = satSequence;
            this.valSequence = valSequence;
        }

        public LevelObject(Data.BeatmapObject beatmapObject, Sequence<Color> colorSequence, List<LevelParentObject> parentObjects, VisualObject visualObject,
            Sequence<float> opacitySequence, Sequence<float> hueSequence, Sequence<float> satSequence, Sequence<float> valSequence,
            Vector3 prefabOffsetPosition, Vector3 prefabOffsetScale, Vector3 prefabOffsetRotation)
        {
            this.beatmapObject = beatmapObject;

            ID = beatmapObject.id;
            StartTime = beatmapObject.StartTime;
            KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(_oldStyle: true);
            depth = beatmapObject.depth;

            this.parentObjects = parentObjects;
            this.visualObject = visualObject;

            this.colorSequence = colorSequence;
            this.opacitySequence = opacitySequence;
            this.hueSequence = hueSequence;
            this.satSequence = satSequence;
            this.valSequence = valSequence;

            this.prefabOffsetPosition = prefabOffsetPosition;
            this.prefabOffsetScale = prefabOffsetScale;
            this.prefabOffsetRotation = prefabOffsetRotation;

            desyncParentIndex = this.parentObjects.Count;

            try
            {
                top = this.parentObjects[this.parentObjects.Count - 1].Transform.parent;

                var pc = beatmapObject.GetParentChain();

                if (pc != null && pc.Count > 0)
                {
                    var beatmapParent = (Data.BeatmapObject)pc[pc.Count - 1];

                    cameraParent = beatmapParent.parent == "CAMERA_PARENT";

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
                parentObjects[parentObjects.Count - 1].GameObject?.SetActive(active);

            if (!active && this.active)
            {
                desyncParentIndex = parentObjects.Count;
                spawned = false;
                //for (int i = 0; i < parentObjects.Count; i++)
                //{
                    var parentObject = parentObjects[0];
                    for (int j = 0; j < parentObject.Position3DSequence.keyframes.Length; j++)
                        parentObject.Position3DSequence.keyframes[j].Stop();
                    for (int j = 0; j < parentObject.ScaleSequence.keyframes.Length; j++)
                        parentObject.ScaleSequence.keyframes[j].Stop();
                    for (int j = 0; j < parentObject.RotationSequence.keyframes.Length; j++)
                        parentObject.RotationSequence.keyframes[j].Stop();
                //}
                for (int i = 0; i < colorSequence.keyframes.Length; i++)
                    colorSequence.keyframes[i].Stop();
            }

            this.active = active;
        }

        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            double num;
            double saturation;
            double value;
            LSFunctions.LSColors.ColorToHSV(color, out num, out saturation, out value);
            return LSFunctions.LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        float prevStartTime = 0f;
        bool spawned = false;
        int desyncParentIndex;
        int syncParentIndex;

        public void Interpolate(float time)
        {
            // Set visual object color
            if (opacitySequence != null && hueSequence != null && satSequence != null && valSequence != null)
            {
                Color color = colorSequence.Interpolate(time - StartTime);
                float opacity = opacitySequence.Interpolate(time - StartTime);

                float hue = hueSequence.Interpolate(time - StartTime);
                float sat = satSequence.Interpolate(time - StartTime);
                float val = valSequence.Interpolate(time - StartTime);

                float a = -(opacity - 1f);

                visualObject.SetColor(LSFunctions.LSColors.fadeColor(ChangeColorHSV(color, hue, sat, val), a >= 0f && a <= 1f ? color.a * a : color.a));
            }
            else if (opacitySequence != null)
            {
                Color color = colorSequence.Interpolate(time - StartTime);
                float opacity = opacitySequence.Interpolate(time - StartTime);

                float a = -(opacity - 1f);

                visualObject.SetColor(LSFunctions.LSColors.fadeColor(color, a >= 0f && a <= 1f ? color.a * a : color.a));
            }
            else
            {
                Color color = colorSequence.Interpolate(time - StartTime);
                visualObject.SetColor(color);
            }

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

            int desyncParentIndex = 0;
            bool hasSpawned = false;
            for (int i = 0; i < parentObjects.Count; i++)
            {
                var parentObject = parentObjects[i];

                if (!spawned || i >= syncParentIndex && i < this.desyncParentIndex)
                {
                    if (parentObject.ParentAdditivePosition)
                        positionAddedOffset += parentObject.ParentOffsetPosition;
                    if (parentObject.ParentAdditiveScale)
                        scaleAddedOffset += parentObject.ParentOffsetScale;
                    if (parentObject.ParentAdditiveRotation)
                        rotationAddedOffset += parentObject.ParentOffsetRotation;

                    // If last parent is position parented, animate position
                    if (animatePosition)
                    {
                        if (parentObject.Position3DSequence != null)
                        {
                            var value = parentObject.Position3DSequence.Interpolate(time - parentObject.TimeOffset - (positionOffset + positionAddedOffset)) + parentObject.BeatmapObject.reactivePositionOffset + parentObject.BeatmapObject.positionOffset;

                            float z = depth * 0.0005f + (value.z / 10f);

                            parentObject.Transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, z);
                        }
                        else
                        {
                            var value = parentObject.PositionSequence.Interpolate(time - parentObject.TimeOffset - (positionOffset + positionAddedOffset));
                            parentObject.Transform.localPosition = new Vector3(value.x * positionParallax, value.y * positionParallax, depth * 0.0005f);
                        }
                    }

                    // If last parent is scale parented, animate scale
                    if (animateScale)
                    {
                        var r = parentObject.BeatmapObject.reactiveScaleOffset + parentObject.BeatmapObject.reactiveScaleOffset + parentObject.BeatmapObject.scaleOffset;
                        var value = parentObject.ScaleSequence.Interpolate(time - parentObject.TimeOffset - (scaleOffset + scaleAddedOffset)) + new Vector2(r.x, r.y);
                        parentObject.Transform.localScale = new Vector3(value.x * scaleParallax, value.y * scaleParallax, 1.0f + parentObject.BeatmapObject.scaleOffset.z);
                    }

                    // If last parent is rotation parented, animate rotation
                    if (animateRotation)
                    {
                        var value = Quaternion.AngleAxis(
                            (parentObject.RotationSequence.Interpolate(time - parentObject.TimeOffset - (rotationOffset + rotationAddedOffset)) + parentObject.BeatmapObject.reactiveRotationOffset) * rotationParallax,
                            Vector3.forward);
                        parentObject.Transform.localRotation = Quaternion.Euler(value.eulerAngles + parentObject.BeatmapObject.rotationOffset);
                    }

                    // Cache parent values to use for next parent
                    positionOffset = parentObject.ParentOffsetPosition;
                    scaleOffset = parentObject.ParentOffsetScale;
                    rotationOffset = parentObject.ParentOffsetRotation;

                    animatePosition = parentObject.ParentAnimatePosition;
                    animateScale = parentObject.ParentAnimateScale;
                    animateRotation = parentObject.ParentAnimateRotation;

                    positionParallax = parentObject.ParentParallaxPosition;
                    scaleParallax = parentObject.ParentParallaxScale;
                    rotationParallax = parentObject.ParentParallaxRotation;

                    if (!spawned)
                        syncParentIndex = i;

                    if (parentObject.BeatmapObject.desync && !spawned)
                    {
                        hasSpawned = true;
                        spawned = true;
                        desyncParentIndex = i + 1;
                    }
                }
            }

            if (hasSpawned)
                this.desyncParentIndex = desyncParentIndex;
        }
    }
}