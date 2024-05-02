using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Components.Editor
{
    /// <summary>
    /// Component for handling drag rotation.
    /// </summary>
    public class RTRotator : MonoBehaviour
    {
        public static float RotatorRadius { get; set; } = 22f;

        EventKeyframe selectedKeyframe;
        bool dragging;
        float dragOffset;
        float dragKeyframeValues;
        bool setKeyframeValues;

        void Update()
        {
            transform.localScale = new Vector3(RotatorRadius, RotatorRadius, 1f);
        }

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                ObjectEditor.inst.RenderObjectKeyframesDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                PrefabEditorManager.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());
        }

        void OnMouseUp()
        {
            dragging = false;
            selectedKeyframe = null;
            setKeyframeValues = false;
        }

        void OnMouseDrag()
        {
            if (!EditorManager.inst || !EditorManager.inst.isEditing)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);

            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                selectedKeyframe = (EventKeyframe)ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().events[2];

                dragging = true;

                Drag(vector2);

                return;
            }

            if (!dragging)
            {
                dragging = true;
                selectedKeyframe = RTObject.SetCurrentKeyframe(2, ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            }

            Drag(vector2);
        }

        void Drag(Vector3 vector2)
        {
            if (selectedKeyframe == null)
                return;

            var pos = new Vector3(
                ObjectEditor.inst.CurrentSelection.IsPrefabObject ? ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().events[0].eventValues[0] : transform.position.x,
                ObjectEditor.inst.CurrentSelection.IsPrefabObject ? ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().events[0].eventValues[1] : transform.position.y,
                0f);

            if (!setKeyframeValues)
            {
                setKeyframeValues = true;
                dragKeyframeValues = selectedKeyframe.eventValues[0];
                dragOffset = Input.GetKey(KeyCode.LeftShift) ? RTMath.roundToNearest(-RTMath.VectorAngle(pos, vector2), 15f) : -RTMath.VectorAngle(pos, vector2);
            }

            selectedKeyframe.eventValues[0] =
                Input.GetKey(KeyCode.LeftShift) ? RTMath.roundToNearest(dragKeyframeValues - dragOffset + -RTMath.VectorAngle(pos, vector2), 15f) : dragKeyframeValues - dragOffset + -RTMath.VectorAngle(pos, vector2);

            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                Updater.UpdatePrefab(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>(), "Offset");
            else
                Updater.UpdateProcessor(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), "Keyframes");
        }
    }
}
