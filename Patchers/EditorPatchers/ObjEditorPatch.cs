using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
        static ObjEditor Instance { get => ObjEditor.inst; set => ObjEditor.inst = value; }

        [HarmonyPatch(nameof(ObjEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(ObjEditor __instance)
        {
            // og code
            {
                if (!Instance)
                    Instance = __instance;
                else if (Instance != __instance)
                {
                    Destroy(__instance.gameObject);
                    return false;
                }

                CoreHelper.LogInit(__instance.className);

                var beginDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;
                    __instance.SelectionBoxImage.gameObject.SetActive(true);
                    __instance.DragStartPos = pointerEventData.position * EditorManager.inst.ScreenScaleInverse;
                    __instance.SelectionRect = default;
                });

                var dragTrigger = TriggerHelper.CreateEntry(EventTriggerType.Drag, eventData =>
                {
                    var vector = ((PointerEventData)eventData).position * EditorManager.inst.ScreenScaleInverse;

                    __instance.SelectionRect.xMin = vector.x < __instance.DragStartPos.x ? vector.x : __instance.DragStartPos.x;
                    __instance.SelectionRect.xMax = vector.x < __instance.DragStartPos.x ? __instance.DragStartPos.x : vector.x;
                    __instance.SelectionRect.yMin = vector.y < __instance.DragStartPos.y ? vector.y : __instance.DragStartPos.y;
                    __instance.SelectionRect.yMax = vector.y < __instance.DragStartPos.y ? __instance.DragStartPos.y : vector.y;

                    __instance.SelectionBoxImage.rectTransform.offsetMin = __instance.SelectionRect.min;
                    __instance.SelectionBoxImage.rectTransform.offsetMax = __instance.SelectionRect.max;
                });

                var endDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.EndDrag, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;
                    __instance.DragEndPos = pointerEventData.position;
                    __instance.SelectionBoxImage.gameObject.SetActive(false);

                    CoroutineHelper.StartCoroutine(ObjectEditor.inst.GroupSelectKeyframes(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
                });

                foreach (var gameObject in __instance.SelectionArea)
                    TriggerHelper.AddEventTriggers(gameObject, beginDragTrigger, dragTrigger, endDragTrigger);
            }

            EditorHelper.LogAvailableInstances<ObjEditor>();

            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.zoomBounds = EditorConfig.Instance.KeyframeZoomBounds.Value;
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.SetMainTimelineZoom))]
        [HarmonyPrefix]
        static bool SetMainTimelineZoomPrefix(float __0, bool __1 = true)
        {
            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            if (__1)
            {
                ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
                ObjectEditor.inst.RenderKeyframes(beatmapObject);
            }
            float f = ObjEditor.inst.objTimelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                f = time / objectLifeLength;
            }

            Instance.StartCoroutine(UpdateTimelineScrollRect(0f, f));

            return false;
        }

        public static IEnumerator UpdateTimelineScrollRect(float _delay, float _val)
        {
            yield return CoroutineHelper.Seconds(_delay);
            if (ObjectEditor.inst.timelinePosScrollbar)
                ObjectEditor.inst.timelinePosScrollbar.value = _val;

            yield break;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentObj))]
        [HarmonyPrefix]
        static bool SetCurrentObjPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.UpdateHighlightedKeyframe))]
        [HarmonyPrefix]
        static bool UpdateHighlightedKeyframePrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.DeRenderObject))]
        [HarmonyPrefix]
        static bool DeRenderObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RenderTimelineObject))]
        [HarmonyPrefix]
        static bool RenderTimelineObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RenderTimelineObjects))]
        [HarmonyPrefix]
        static bool RenderTimelineObjectsPrefix()
        {
            EditorTimeline.inst.RenderTimelineObjects();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.DeleteObject))]
        [HarmonyPrefix]
        static bool DeleteObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.DeleteObjects))]
        [HarmonyPrefix]
        static bool DeleteObjectsPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddPrefabExpandedToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabExpandedToLevelPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddSelectedObject))]
        [HarmonyPrefix]
        static bool AddSelectedObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddSelectedObjectOnly))]
        [HarmonyPrefix]
        static bool AddSelectedObjectOnlyPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.ContainedInSelectedObjects))]
        [HarmonyPrefix]
        static bool ContainedInSelectedObjectsPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RefreshParentGUI))]
        [HarmonyPrefix]
        static bool RefreshParentGUIPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.CopyAllSelectedEvents))]
        [HarmonyPrefix]
        static bool CopyAllSelectedEventsPrefix()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.CopyAllSelectedEvents(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.PasteKeyframes))]
        [HarmonyPrefix]
        static bool PasteKeyframesPrefix()
        {
            ObjectEditor.inst.PasteKeyframes();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            ObjectEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentKeyframe), new Type[] { typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetCurrentKeyframePrefix(int __0, bool __1 = false)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.SetCurrentKeyframe(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentKeyframe), new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetCurrentKeyframePrefix(int __0, int __1, bool __2 = false, bool __3 = false)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.SetCurrentKeyframe(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.AddCurrentKeyframe))]
        [HarmonyPrefix]
        static bool AddCurrentKeyframePrefix(int __0, bool __1 = false)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.AddCurrentKeyframe(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.ResizeKeyframeTimeline))]
        [HarmonyPrefix]
        static bool ResizeKeyframeTimelinePrefix()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.ResizeKeyframeTimeline(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetAudioTime))]
        [HarmonyPrefix]
        static bool SetAudioTimePrefix(float __0)
        {
            if (Instance.changingTime)
            {
                Instance.newTime = __0;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.GetKeyframeIcon))]
        [HarmonyPrefix]
        static bool GetKeyframeIconPrefix(ref Sprite __result, DataManager.LSAnimation __0, DataManager.LSAnimation __1)
        {
            __result = null;
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframes))]
        [HarmonyPrefix]
        static bool CreateKeyframesPrefix()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.CreateKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframeStartDragTrigger))]
        [HarmonyPrefix]
        static bool CreateKeyframeStartDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
            __result = TriggerHelper.CreateEntry(__0, eventData => { });
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframeEndDragTrigger))]
        [HarmonyPrefix]
        static bool CreateKeyframeEndDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
            __result = TriggerHelper.CreateEntry(__0, eventData => { });
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.DeRenderSelectedObjects))]
        [HarmonyPrefix]
        static bool DeRenderSelectedObjectsPrefix()
        {
            EditorTimeline.inst.DeselectAllObjects();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CopyObject))]
        [HarmonyPrefix]
        static bool CopyObjectPrefix()
        {
            ObjectEditor.inst.CopyObjects();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.PasteObject))]
        [HarmonyPrefix]
        static bool PasteObjectPrefix(float __0)
        {
            ObjectEditor.inst.PasteObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.AddEvent))]
        [HarmonyPrefix]
        static bool AddEventPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.ToggleLockCurrentSelection))]
        [HarmonyPrefix]
        static bool ToggleLockCurrentSelectionPrefix()
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.isBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.locked = !timelineObject.GetData<BeatmapObject>().editorData.locked;
                if (timelineObject.isPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.locked = !timelineObject.GetData<PrefabObject>().editorData.locked;

                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.UpdateKeyframeOrder))]
        [HarmonyPrefix]
        static bool UpdateKeyframeOrderPrefix(bool _setCurrent = true)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.UpdateKeyframeOrder(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SnapToBPM))]
        [HarmonyPrefix]
        static bool SnapToBPMPrefix(ref float __result, float __0)
        {
            __result = RTEditor.SnapToBPM(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.posCalc))]
        [HarmonyPrefix]
        static bool posCalcPrefix(ref float __result, float __0)
        {
            __result = ObjectEditor.TimeTimelineCalc(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.timeCalc))]
        [HarmonyPrefix]
        static bool timeCalcPrefix(ref float __result)
        {
            __result = ObjectEditor.MouseTimelineCalc();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.RefreshKeyframeGUI))]
        [HarmonyPrefix]
        static bool RefreshKeyframeGUIPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.CreateNewNormalObject))]
        [HarmonyPrefix]
        static bool CreateNewNormalObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewNormalObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewCircleObject))]
        [HarmonyPrefix]
        static bool CreateNewCircleObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewCircleObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewTriangleObject))]
        [HarmonyPrefix]
        static bool CreateNewTriangleObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewTriangleObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewTextObject))]
        [HarmonyPrefix]
        static bool CreateNewTextObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewTextObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewHexagonObject))]
        [HarmonyPrefix]
        static bool CreateNewHexagonObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewHexagonObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewHelperObject))]
        [HarmonyPrefix]
        static bool CreateNewHelperObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewHelperObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewDecorationObject))]
        [HarmonyPrefix]
        static bool CreateNewDecorationObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewDecorationObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewEmptyObject))]
        [HarmonyPrefix]
        static bool CreateNewEmptyObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewEmptyObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewPersistentObject))]
        [HarmonyPrefix]
        static bool CreateNewPersistentObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewNoAutokillObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.Zoom), MethodType.Setter)]
        [HarmonyPrefix]
        static bool ZoomSetterPrefix(ref float value)
        {
            ObjectEditor.inst.SetTimeline(value);
            return false;
        }
    }
}
