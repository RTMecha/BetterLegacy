using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Components.Editor
{
    /// <summary>
    /// Component for handling drag scale.
    /// </summary>
    public class RTScaler : MonoBehaviour
    {
        public static float ScalerOffset { get; set; } = 6f;
        public static float ScalerScale { get; set; } = 1.6f;

        bool dragging;

        EventKeyframe selectedKeyframe;
        bool setKeyframeValues;
        Vector2 dragKeyframeValues;
        Vector2 dragOffset;

        public RTObject.Axis axis = RTObject.Axis.Static;

        void Update()
        {
            switch (axis)
            {
                case RTObject.Axis.PosX:
                    transform.localPosition = new Vector3(ScalerOffset, 0f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case RTObject.Axis.PosY:
                    transform.localPosition = new Vector3(0f, ScalerOffset);
                    transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
                case RTObject.Axis.NegX:
                    transform.localPosition = new Vector3(-ScalerOffset, 0f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    break;
                case RTObject.Axis.NegY:
                    transform.localPosition = new Vector3(0f, -ScalerOffset);
                    transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    break;
            }
            transform.localScale = new Vector3(ScalerScale, ScalerScale, 1f);
        }

        void FixedUpdate()
        {
            if (!dragging)
                return;

            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                ObjectEditor.inst.RenderObjectKeyframesDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());
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
            var vector2 = Camera.main.ScreenToWorldPoint(vector) * 0.2f;
            var vector3 = new Vector3(RTMath.RoundToNearestDecimal(vector2.x, 1), RTMath.RoundToNearestDecimal(vector2.y, 1), transform.localPosition.z);

            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                selectedKeyframe = (EventKeyframe)ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().events[1];

                dragging = true;

                Drag(vector2, vector3);

                return;
            }

            if (!dragging)
            {
                dragging = true;
                selectedKeyframe = RTObject.SetCurrentKeyframe(1, ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            }

            Drag(vector2, vector3);
        }

        void Drag(Vector3 vector2, Vector3 vector3)
        {
            if (selectedKeyframe == null)
                return;

            if (!setKeyframeValues)
            {
                setKeyframeValues = true;
                dragKeyframeValues = new Vector2(selectedKeyframe.eventValues[0], selectedKeyframe.eventValues[1]);
                dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
            }

            var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                float total = Vector2.Distance(finalVector, dragOffset);

                if (axis == RTObject.Axis.PosX && dragOffset.x - finalVector.x > 0f)
                    total = -total;

                if (axis == RTObject.Axis.PosY && dragOffset.y - finalVector.y > 0f)
                    total = -total;

                if (axis == RTObject.Axis.NegX && dragOffset.x - finalVector.x < 0f)
                    total = -total;

                if (axis == RTObject.Axis.NegY && dragOffset.y - finalVector.y < 0f)
                    total = -total;

                selectedKeyframe.eventValues[0] = dragKeyframeValues.x + total;
                selectedKeyframe.eventValues[1] = dragKeyframeValues.y + total;
            }
            else
            {
                if (axis == RTObject.Axis.PosX)
                    selectedKeyframe.eventValues[0] = dragKeyframeValues.x - dragOffset.x + finalVector.x;
                if (axis == RTObject.Axis.NegX)
                    selectedKeyframe.eventValues[0] = dragKeyframeValues.x + dragOffset.x - finalVector.x;
                if (axis == RTObject.Axis.PosY)
                    selectedKeyframe.eventValues[1] = dragKeyframeValues.y - dragOffset.y + finalVector.y;
                if (axis == RTObject.Axis.NegY)
                    selectedKeyframe.eventValues[1] = dragKeyframeValues.y + dragOffset.y - finalVector.y;
            }

            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                Updater.UpdatePrefab(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>(), "Offset");
            else
                Updater.UpdateProcessor(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), "Keyframes");
        }
    }
}
