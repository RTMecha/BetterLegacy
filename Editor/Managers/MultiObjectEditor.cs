using System;
using System.Linq;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Managers
{
    public class MultiObjectEditor : BaseManager<MultiObjectEditor, EditorManagerSettings>
    {
        #region Values

        public MultiObjectEditorDialog Dialog { get; set; }

        /// <summary>
        /// String to format from.
        /// </summary>
        public const string DEFAULT_TEXT = "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nBG Count: {5}/{6}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new MultiObjectEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public override void OnTick()
        {
            if (!Dialog || !Dialog.IsCurrent)
                return;

            Dialog.SelectedObjectCountLabel?.SetText($"Selected Object Count [{EditorTimeline.inst.SelectedBeatmapObjects.Count}]");
            Dialog.SelectedBackgroundObjectCountLabel?.SetText($"Selected Background Object Count [{EditorTimeline.inst.SelectedBackgroundObjects.Count}]");
            Dialog.SelectedPrefabObjectCountLabel?.SetText($"Selected Prefab Object Count [{EditorTimeline.inst.SelectedPrefabObjects.Count}]");
            Dialog.SelectedTotalCountLabel?.SetText($"Selected Total Count [{EditorTimeline.inst.SelectedObjects.Count}]");
        }

        public void ForEachTimelineObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                action?.Invoke(timelineObject);
        }

        public void ForEachBeatmapObject(Action<BeatmapObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                action?.Invoke(timelineObject.GetData<BeatmapObject>());
        }
        
        public void ForEachBeatmapObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                action?.Invoke(timelineObject);
        }

        public void ForEachPrefabObject(Action<PrefabObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedPrefabObjects)
                action?.Invoke(timelineObject.GetData<PrefabObject>());
        }
        
        public void ForEachPrefabObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedPrefabObjects)
                action?.Invoke(timelineObject);
        }
        
        public void ForEachBackgroundObject(Action<BackgroundObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBackgroundObjects)
                action?.Invoke(timelineObject.GetData<BackgroundObject>());
        }
        
        public void ForEachBackgroundObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBackgroundObjects)
                action?.Invoke(timelineObject);
        }

        public void ForEachModifyable(Action<Core.Data.Modifiers.IModifyable> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.TryGetData(out Core.Data.Modifiers.IModifyable modifyable))
                    action?.Invoke(modifyable);
            }
        }

        public void ClearKeyframes(int type) => RTEditor.inst.ShowWarningPopup($"You are about to clear the {KeyframeTimeline.IntToTypeName(type).ToLower()} keyframes from all selected objects, this <b>CANNOT</b> be undone!", () =>
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
            {
                var bm = timelineObject.GetData<BeatmapObject>();
                bm.TimelineKeyframes.ForLoopReverse((timelineKeyframe, index) =>
                {
                    if (timelineKeyframe.Type != type)
                        return;
                    CoreHelper.Delete(timelineKeyframe.GameObject);
                    bm.TimelineKeyframes.RemoveAt(index);
                });
                bm.events[type].Sort((a, b) => a.time.CompareTo(b.time));
                var firstKF = bm.events[type][0].Copy(false);
                bm.events[type].Clear();
                bm.events[type].Add(firstKF);
                if (EditorTimeline.inst.SelectedObjects.Count == 1)
                {
                    ObjectEditor.inst.Dialog.Timeline.ResizeKeyframeTimeline(bm);
                    ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(bm);
                }

                RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
        });

        public void SetParentToggle(int type, int operation) => ForEachTimelineObject(timelineObject =>
        {
            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                switch (operation)
                {
                    case 0: {
                            beatmapObject.SetParentType(type, false);
                            break;
                        }
                    case 1: {
                            beatmapObject.SetParentType(type, true);
                            break;
                        }
                    case 2: {
                            beatmapObject.SetParentType(type, !beatmapObject.GetParentType(type));
                            break;
                        }
                }
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT);
            }
            if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                switch (operation)
                {
                    case 0: {
                            prefabObject.SetParentType(type, false);
                            break;
                        }
                    case 1: {
                            prefabObject.SetParentType(type, true);
                            break;
                        }
                    case 2: {
                            prefabObject.SetParentType(type, !prefabObject.GetParentType(type));
                            break;
                        }
                }
                RTLevel.Current?.UpdatePrefab(prefabObject);
            }
        });

        public void SetParentOffset(int type, float value, MathOperation operation) => ForEachTimelineObject(timelineObject =>
        {
            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                RTMath.Operation(ref beatmapObject.parentOffsets[type], value, operation);
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT);
            }
            if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                RTMath.Operation(ref prefabObject.parentOffsets[type], value, operation);
                RTLevel.Current?.UpdatePrefab(prefabObject);
            }
        });

        public void SetParentAdditive(int type, int operation) => ForEachTimelineObject(timelineObject =>
        {
            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                switch (operation)
                {
                    case 0: {
                            beatmapObject.SetParentAdditive(type, false);
                            break;
                        }
                    case 1: {
                            beatmapObject.SetParentAdditive(type, true);
                            break;
                        }
                    case 2: {
                            beatmapObject.SetParentAdditive(type, !beatmapObject.GetParentAdditive(type));
                            break;
                        }
                }
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT);
            }
            if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                switch (operation)
                {
                    case 0: {
                            prefabObject.SetParentAdditive(type, false);
                            break;
                        }
                    case 1: {
                            prefabObject.SetParentAdditive(type, true);
                            break;
                        }
                    case 2: {
                            prefabObject.SetParentAdditive(type, !prefabObject.GetParentAdditive(type));
                            break;
                        }
                }
                RTLevel.Current?.UpdatePrefab(prefabObject);
            }
        });

        public void SetParentParallax(int type, float value, MathOperation operation) => ForEachTimelineObject(timelineObject =>
        {
            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                RTMath.Operation(ref beatmapObject.ParentParallax[type], value, operation);
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT);
            }
            if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                RTMath.Operation(ref prefabObject.ParentParallax[type], value, operation);
                RTLevel.Current?.UpdatePrefab(prefabObject);
            }
        });

        public void SetObjectType(BeatmapObject.ObjectType objectType) => ForEachBeatmapObject(timelineObject =>
        {
            var beatmapObject = timelineObject.GetData<BeatmapObject>();
            beatmapObject.objectType = objectType;

            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.OBJECT_TYPE);
        });
        
        public void SetColorBlendMode(ColorBlendMode colorBlendMode) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.colorBlendMode = colorBlendMode;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });

        public void SetGradientType(GradientType gradientType) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.gradientType = gradientType;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });
        
        public void SetRenderLayerType(BeatmapObject.RenderLayerType renderLayerType) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.renderLayerType = renderLayerType;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });

        #endregion
    }
}
