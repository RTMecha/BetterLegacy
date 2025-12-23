using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTBeatmapObject : Exists, IRTObject, ICustomActivatable, IParentChain
    {
        public RTBeatmapObject(BeatmapObject beatmapObject, List<ParentObject> parentObjects, VisualObject visualObject, RTLevelBase parentRuntime)
        {
            this.beatmapObject = beatmapObject;

            ParentRuntime = parentRuntime;
            StartTime = beatmapObject.StartTime;
            KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;
            Depth = beatmapObject.Depth;

            this.parentObjects = parentObjects;
            this.visualObject = visualObject;

            isImage = beatmapObject.ShapeType == ShapeType.Image;

            this.UpdateParentValues();
        }

        #region Values

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }
        public float KillTime { get; set; }
        public bool Active { get; set; }

        public bool CustomActive { get; set; } = true;

        public BeatmapObject beatmapObject;

        public List<ParentObject> parentObjects;
        public List<ParentObject> ParentObjects { get => parentObjects; set => parentObjects = value; }
        public VisualObject visualObject;
        
        public float Depth { get; set; }

        public bool CameraParent { get; set; }
        public bool PositionParent { get; set; }
        public bool ScaleParent { get; set; }
        public bool RotationParent { get; set; }

        public bool isImage;
        public float PositionParentOffset { get; set; }
        public float ScaleParentOffset { get; set; }
        public float RotationParentOffset { get; set; }

        public Vector3 TopPosition { get; set; }
        public Vector3 TopScale { get; set; } = Vector3.one;
        public Vector3 TopRotation { get; set; }

        public Transform Parent { get; set; }

        /// <summary>
        /// If the object is active.
        /// </summary>
        public bool topActive = true;

        #region Internal

        bool active = false;

        public Vector3 CurrentScale { get; set; } // if scale is 0, 0 then disable collider and renderer

        bool cachedColliderEnabled;
        bool cachedRendererEnabled;

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

            if (Parent)
                CoreHelper.Delete(Parent);

            parentObjects = null;
            Parent = null;
            beatmapObject = null;

            visualObject?.Clear();
            visualObject = null;
        }

        public void SetActive(bool active)
        {
            Active = active;
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
            if (parentObjects == null || parentObjects.IsEmpty())
                return;

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
        /// Sets the top object active.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetCustomActive(bool active)
        {
            CustomActive = active;

            if (!Parent)
                return;

            Parent.gameObject.SetActive(active);
            topActive = active;
        }

        public void Interpolate(float time)
        {
            if (!topActive)
                return;

            if (!Parent)
                return;

            // Set visual object color
            visualObject.InterpolateColor(time - StartTime);

            if (isImage)
                visualObject.SetOrigin(new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * BeatmapObject.DEPTH_VISUAL_MULTIPLY)); // fixes origin being off.

            this.UpdateCameraParent(beatmapObject.renderLayerType == BeatmapObject.RenderLayerType.Background);
            this.InterpolateParentChain(time, beatmapObject.fromPrefab);
        }

        public void CheckScale()
        {
            var active = RTMath.Distance(0f, CurrentScale.x) > 0.001f && RTMath.Distance(0f, CurrentScale.y) > 0.001f && RTMath.Distance(0f, CurrentScale.z) > 0.001f;

            var colliderEnabled = visualObject.HasCollision && active;
            if (visualObject.collider)
            {
                if (cachedColliderEnabled != colliderEnabled)
                {
                    cachedColliderEnabled = colliderEnabled;
                    visualObject.collider.enabled = colliderEnabled;
                }
            }
            if (visualObject.renderer)
            {
                if (cachedRendererEnabled != active)
                {
                    cachedRendererEnabled = active;
                    visualObject.renderer.enabled = active;
                }
            }
        }

        public override string ToString() => beatmapObject?.ToString() ?? string.Empty;

        #endregion
    }
}