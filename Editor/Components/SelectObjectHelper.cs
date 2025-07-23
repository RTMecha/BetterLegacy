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
            switch (((PointerEventData)eventData).button)
            {
                case PointerEventData.InputButton.Right: {
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
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

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
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

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
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

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
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                                break;
                                            }
                                    }
                                }
                            })
                            );
                        break;
                    }
                case PointerEventData.InputButton.Middle: {
                        var selectedKeyframe = EditorTimeline.inst.CurrentSelection.TimelineReference switch
                        {
                            TimelineObject.TimelineReferenceType.BeatmapObject => EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetOrCreateKeyframe((int)EditorConfig.Instance.ObjectDraggerHelperType.Value, EditorConfig.Instance.ObjectDraggerCreatesKeyframe.Value),
                            TimelineObject.TimelineReferenceType.PrefabObject => EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events.GetAt((int)EditorConfig.Instance.ObjectDraggerHelperType.Value),
                            _ => null,
                        };

                        break;
                    }
            }
        }

        public void Scroll(BaseEventData eventData)
        {
            var pointerEventData = eventData as PointerEventData;

            var selectedKeyframe = EditorTimeline.inst.CurrentSelection.TimelineReference switch
            {
                TimelineObject.TimelineReferenceType.BeatmapObject => EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetOrCreateKeyframe((int)EditorConfig.Instance.ObjectDraggerHelperType.Value, EditorConfig.Instance.ObjectDraggerCreatesKeyframe.Value),
                TimelineObject.TimelineReferenceType.PrefabObject => EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events.GetAt((int)EditorConfig.Instance.ObjectDraggerHelperType.Value),
                _ => null,
            };
            var shift = Input.GetKey(KeyCode.LeftShift);

            switch (EditorConfig.Instance.ObjectDraggerHelperType.Value)
            {
                case TransformType.Position: {
                        switch (EditorTimeline.inst.CurrentSelection.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    var prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                    RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();

                                    var val = shift ? backgroundObject.pos.y : backgroundObject.pos.x;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    if (shift)
                                        backgroundObject.pos.y = val;
                                    else
                                        backgroundObject.pos.x = val;

                                    RTBackgroundEditor.inst.RenderPosition(backgroundObject);

                                    break;
                                }
                        }
                        break;
                    }
                case TransformType.Scale: {
                        switch (EditorTimeline.inst.CurrentSelection.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    var prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                    RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();

                                    var val = shift ? backgroundObject.scale.y : backgroundObject.scale.x;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    if (shift)
                                        backgroundObject.scale.y = val;
                                    else
                                        backgroundObject.scale.x = val;

                                    RTBackgroundEditor.inst.RenderScale(backgroundObject);

                                    break;
                                }
                        }
                        break;
                    }
                case TransformType.Rotation: {
                        switch (EditorTimeline.inst.CurrentSelection.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 5f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 5f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                    var prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 5f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 5f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = val;

                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                    RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();

                                    var val = backgroundObject.rot;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 5f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 5f;

                                    val = float.Parse(val.ToString("f2"));

                                    backgroundObject.rot = val;
                                    RTBackgroundEditor.inst.RenderRotation(backgroundObject);

                                    break;
                                }
                        }
                        break;
                    }
                case TransformType.Color: {
                        switch (EditorTimeline.inst.CurrentSelection.TimelineReference)
                        {
                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                                    var val = selectedKeyframe.values[shift ? 1 : 0];
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= !shift ? 1f : -0.1f;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += !shift ? 1f : -0.1f;

                                    val = float.Parse(val.ToString("f2"));

                                    selectedKeyframe.values[shift ? 1 : 0] = !shift ? Mathf.Clamp(val, 0, BeatmapTheme.OBJECT_COLORS_COUNT - 1) : Mathf.Clamp(val, 0f, 1f);

                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);

                                    break;
                                }
                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                    var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();

                                    var val = shift ? backgroundObject.fadeColor : backgroundObject.color;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        val -= 1;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        val += 1;

                                    if (shift)
                                        backgroundObject.fadeColor = Mathf.Clamp(val, 0, BeatmapTheme.BACKGROUND_COLORS_COUNT - 1);
                                    else
                                        backgroundObject.color = Mathf.Clamp(val, 0, BeatmapTheme.BACKGROUND_COLORS_COUNT - 1);

                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        public void BeginDrag(BaseEventData eventData)
        {
            if (!CoreHelper.IsEditing)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.localPosition.z);
            var vector2 = Camera.main.ScreenToWorldPoint(vector);
            var vector3 = new Vector3((float)((int)vector2.x), (float)((int)vector2.y), transform.localPosition.z);

            dragTime = 0.1f;
            selectedKeyframe = EditorTimeline.inst.CurrentSelection.TimelineReference switch
            {
                TimelineObject.TimelineReferenceType.BeatmapObject => EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetOrCreateKeyframe((int)EditorConfig.Instance.ObjectDraggerHelperType.Value, EditorConfig.Instance.ObjectDraggerCreatesKeyframe.Value),
                TimelineObject.TimelineReferenceType.PrefabObject => EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>().events.GetAt((int)EditorConfig.Instance.ObjectDraggerHelperType.Value),
                _ => null,
            };
            dragKeyframeValues = EditorTimeline.inst.CurrentSelection.isBackgroundObject ? EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>().pos : new Vector2(selectedKeyframe.values[0], (int)EditorConfig.Instance.ObjectDraggerHelperType.Value < 2 ? selectedKeyframe.values[1] : 0f);
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
                        {
                            var val = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                            if (EditorConfig.Instance.ObjectDraggerHelperType.Value == TransformType.Color)
                                val = Mathf.Clamp(RTMath.RoundToNearestNumber(val, 1f), 0, BeatmapTheme.OBJECT_COLORS_COUNT - 1);

                            selectedKeyframe.values[0] = val;
                        }
                        if ((int)EditorConfig.Instance.ObjectDraggerHelperType.Value < 2 && (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY))
                        {
                            var val = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                            selectedKeyframe.values[1] = val;
                        }

                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                        ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.PrefabObject: {
                        if (selectedKeyframe == null || (int)EditorConfig.Instance.ObjectDraggerHelperType.Value > 2)
                            return;

                        var prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                            selectedKeyframe.values[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                        if ((int)EditorConfig.Instance.ObjectDraggerHelperType.Value < 2 && (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY))
                            selectedKeyframe.values[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                        if ((int)EditorConfig.Instance.ObjectDraggerHelperType.Value > 2)
                            return;

                        var backgroundObject = EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();
                        if (firstDirection == Axis.Static || firstDirection == Axis.PosX || firstDirection == Axis.NegX)
                        {
                            var val = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                            switch (EditorConfig.Instance.ObjectDraggerHelperType.Value)
                            {
                                case TransformType.Position: {
                                        backgroundObject.pos.x = val;
                                        break;
                                    }
                                case TransformType.Scale: {
                                        backgroundObject.scale.x = val;
                                        break;
                                    }
                                case TransformType.Rotation: {
                                        backgroundObject.rot = val;
                                        break;
                                    }
                                case TransformType.Color: {
                                        backgroundObject.color = (int)RTMath.Clamp(RTMath.RoundToNearestNumber(val, 1f), 0, BeatmapTheme.OBJECT_COLORS_COUNT - 1);

                                        break;
                                    }
                            }
                        }
                        if ((int)EditorConfig.Instance.ObjectDraggerHelperType.Value < 2 && (firstDirection == Axis.Static || firstDirection == Axis.PosY || firstDirection == Axis.NegY))
                        {
                            var val = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);
                            switch (EditorConfig.Instance.ObjectDraggerHelperType.Value)
                            {
                                case TransformType.Position: {
                                        backgroundObject.pos.y = val;
                                        break;
                                    }
                                case TransformType.Scale: {
                                        backgroundObject.scale.y = val;
                                        break;
                                    }
                            }
                        }

                        RTBackgroundEditor.inst.RenderPosition(backgroundObject);
                        RTBackgroundEditor.inst.RenderScale(backgroundObject);
                        RTBackgroundEditor.inst.RenderRotation(backgroundObject);
                        RTBackgroundEditor.inst.RenderColor(backgroundObject);
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
