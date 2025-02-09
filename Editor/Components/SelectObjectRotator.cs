﻿using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for handling drag rotation.
    /// </summary>
    public class SelectObjectRotator : MonoBehaviour
    {
        /// <summary>
        /// How large the ring radius should be.
        /// </summary>
        public static float RotatorRadius { get; set; } = 22f;

        EventKeyframe selectedKeyframe;
        bool dragging;
        float dragOffset;
        float dragKeyframeValues;
        bool setKeyframeValues;

        void Update() => transform.localScale = new Vector3(RotatorRadius, RotatorRadius, 1f);

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderObjectKeyframesDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectDialog(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
        }

        void OnMouseUp()
        {
            dragging = false;
            selectedKeyframe = null;
            setKeyframeValues = false;
        }

        void OnMouseDrag()
        {
            if (CoreHelper.InEditorPreview)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);

            if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
            {
                selectedKeyframe = (EventKeyframe)EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events[2];

                dragging = true;

                Drag(vector2);

                return;
            }

            if (!dragging)
            {
                dragging = true;
                selectedKeyframe = SelectObject.SetCurrentKeyframe(2, EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            }

            Drag(vector2);
        }

        void Drag(Vector3 vector2)
        {
            if (selectedKeyframe == null)
                return;

            var pos = new Vector3(
                EditorTimeline.inst.CurrentSelection.isPrefabObject ? EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events[0].eventValues[0] : transform.position.x,
                EditorTimeline.inst.CurrentSelection.isPrefabObject ? EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events[0].eventValues[1] : transform.position.y,
                0f);

            if (!setKeyframeValues)
            {
                setKeyframeValues = true;
                dragKeyframeValues = selectedKeyframe.eventValues[0];
                dragOffset = Input.GetKey(KeyCode.LeftShift) ? RTMath.RoundToNearestNumber(-RTMath.VectorAngle(pos, vector2), 15f) : -RTMath.VectorAngle(pos, vector2);
            }

            selectedKeyframe.eventValues[0] =
                Input.GetKey(KeyCode.LeftShift) ? RTMath.RoundToNearestNumber(dragKeyframeValues - dragOffset + -RTMath.VectorAngle(pos, vector2), 15f) : dragKeyframeValues - dragOffset + -RTMath.VectorAngle(pos, vector2);

            if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                Updater.UpdatePrefab(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>(), "Offset");
            else
                Updater.UpdateObject(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), "Keyframes");
        }
    }
}
