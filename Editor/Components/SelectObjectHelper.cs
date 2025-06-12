using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;

using Axis = BetterLegacy.Editor.Components.SelectObject.Axis;

namespace BetterLegacy.Editor.Components
{
    public class SelectObjectHelper : MonoBehaviour
    {
        void Update()
        {
            cachedActive = Active;

            if (image)
                image.enabled = cachedActive;

            if (!cachedActive)
                return;

            if (EditorTimeline.inst.CurrentSelection.TryGetData(out ITransformable transformable))
                transform.position = Camera.main.WorldToScreenPoint(transformable.GetFullPosition());
        }

        public void PointerDown(BaseEventData eventData)
        {
            if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                return;

            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Go to Timeline Object", () => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.CurrentSelection, true)),
                new ButtonFunction(true),
                new ButtonFunction("Hide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.HIDE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), RTLevel.BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Unhide", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        timelineObject.Hidden = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.HIDE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.HIDE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), RTLevel.BackgroundObjectContext.HIDE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Preview Selectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = true;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                }),
                new ButtonFunction("Preview Unselectable", () =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBackgroundObject)
                            continue;

                        timelineObject.SelectableInPreview = false;
                        switch (timelineObject.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), RTLevel.ObjectContext.SELECTABLE);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.SELECTABLE);

                                    break;
                                }
                        }
                    }
                })
                );
        }

        public void BeginDrag(BaseEventData eventData)
        {
            if (!CoreHelper.IsEditing)
                return;

            CoreHelper.Log($"START DRAGGING");

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);
            var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

            dragTime = 0.1f;
            selectedKeyframe = EditorTimeline.inst.CurrentSelection.TimelineReference switch
            {
                TimelineObject.TimelineReferenceType.BeatmapObject => EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetOrCreateKeyframe(0, EditorConfig.Instance.ObjectDraggerCreatesKeyframe.Value),
                TimelineObject.TimelineReferenceType.PrefabObject => EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events[0],
                _ => null,
            };
            dragKeyframeValues = EditorTimeline.inst.CurrentSelection.isBackgroundObject ? EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>().pos : new Vector2(selectedKeyframe.values[0], selectedKeyframe.values[1]);
            dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
        }

        public void Drag(BaseEventData eventData)
        {
            dragTime -= Time.deltaTime;
            if (!CoreHelper.IsEditing || dragTime > 0f)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);
            var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

            dragging = true;

            Drag(vector2, vector3);
        }

        public void EndDrag(BaseEventData eventData)
        {
            dragging = false;
            selectedKeyframe = null;
            firstDirection = Axis.Static;
        }

        void Drag(Vector3 vector2, Vector3 vector3)
        {
            var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

            if (Input.GetKey(KeyCode.LeftControl) && firstDirection == Axis.Static)
            {
                if (dragOffset.x > finalVector.x)
                    firstDirection = Axis.PosX;

                if (dragOffset.x < finalVector.x)
                    firstDirection = Axis.NegX;

                if (dragOffset.y > finalVector.y)
                    firstDirection = Axis.PosY;

                if (dragOffset.y < finalVector.y)
                    firstDirection = Axis.NegY;
            }

            switch (EditorTimeline.inst.CurrentSelection.TimelineReference)
            {
                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                        if (selectedKeyframe == null)
                            return;

                        var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                            selectedKeyframe.values[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY)
                            selectedKeyframe.values[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                        ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.PrefabObject: {
                        if (selectedKeyframe == null)
                            return;

                        var prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                            selectedKeyframe.values[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY)
                            selectedKeyframe.values[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                        RTLevel.Current?.UpdatePrefab(prefabObject, RTLevel.PrefabContext.TRANSFORM_OFFSET);
                        RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                        var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                            backgroundObject.pos.x = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY)
                            backgroundObject.pos.y = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                        RTBackgroundEditor.inst.RenderPosition(backgroundObject);
                        break;
                    }
            }
        }

        public bool Active => CoreHelper.IsEditing && EditorConfig.Instance.ObjectDraggerHelper.Value && (ObjectEditor.inst.Dialog.IsCurrent || RTPrefabEditor.inst.PrefabObjectEditor.IsCurrent || RTBackgroundEditor.inst.Dialog.IsCurrent);

        bool cachedActive;

        #region Dragging

        float dragTime;

        public bool dragging;

        Vector2 dragKeyframeValues;
        public EventKeyframe selectedKeyframe;
        Vector2 dragOffset;
        Axis firstDirection = Axis.Static;

        #endregion

        public Image image;
    }
}
