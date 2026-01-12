using System;
using System.Linq;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
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

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public MultiObjectEditorDialog Dialog { get; set; }

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

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachTimelineObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                action?.Invoke(timelineObject);
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachBeatmapObject(Action<BeatmapObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                action?.Invoke(timelineObject.GetData<BeatmapObject>());
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachBeatmapObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                action?.Invoke(timelineObject);
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachPrefabObject(Action<PrefabObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedPrefabObjects)
                action?.Invoke(timelineObject.GetData<PrefabObject>());
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachPrefabObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedPrefabObjects)
                action?.Invoke(timelineObject);
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachBackgroundObject(Action<BackgroundObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBackgroundObjects)
                action?.Invoke(timelineObject.GetData<BackgroundObject>());
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachBackgroundObject(Action<TimelineObject> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBackgroundObjects)
                action?.Invoke(timelineObject);
        }

        /// <summary>
        /// Performs an action for each selected object.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void ForEachModifyable(Action<IModifyable> action)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.TryGetData(out IModifyable modifyable))
                    action?.Invoke(modifyable);
            }
        }

        /// <summary>
        /// Resets a specific keyframe type list to the default for each selected object.
        /// </summary>
        /// <param name="type">Type of the keyframe.</param>
        public void ClearKeyframes(int type) => RTEditor.inst.ShowWarningPopup($"You are about to clear the {KeyframeTimeline.IntToTypeName(type).ToLower()} keyframes from all selected objects, this <b>CANNOT</b> be undone!", () => ForEachBeatmapObject(timelineObject =>
        {
            var beatmapObject = timelineObject.GetData<BeatmapObject>();
            beatmapObject.TimelineKeyframes.ForLoopReverse((timelineKeyframe, index) =>
            {
                if (timelineKeyframe.Type != type)
                    return;
                CoreHelper.Delete(timelineKeyframe.GameObject);
                beatmapObject.TimelineKeyframes.RemoveAt(index);
            });
            beatmapObject.events[type].Sort((a, b) => a.time.CompareTo(b.time));
            var firstKF = beatmapObject.events[type][0].Copy(false);
            beatmapObject.events[type].Clear();
            beatmapObject.events[type].Add(firstKF);
            if (EditorTimeline.inst.SelectedObjects.Count == 1)
            {
                ObjectEditor.inst.Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(beatmapObject);
            }

            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
        }));

        /// <summary>
        /// Sets the parent toggle for each selected object.
        /// </summary>
        /// <param name="type">Parent type to set the toggle of.</param>
        /// <param name="operation">How the parent toggle should be changed.<br></br>
        /// 0 = false<br></br>
        /// 1 = true<br></br>
        /// 2 = swap</param>
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

        /// <summary>
        /// Sets the parent offset (delay) for each selected object.
        /// </summary>
        /// <param name="type">Parent type to set the offset of.</param>
        /// <param name="value">Value to use.</param>
        /// <param name="operation">Operation to apply.</param>
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

        /// <summary>
        /// Sets the parent additive for each selected object.
        /// </summary>
        /// <param name="type">Parent type to set the additive of.</param>
        /// <param name="operation">How the parent additive should be changed.<br></br>
        /// 0 = false<br></br>
        /// 1 = true<br></br>
        /// 2 = swap</param>
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

        /// <summary>
        /// Sets the parent parallax for each selected object.
        /// </summary>
        /// <param name="type">Parent type to set the parallax of.</param>
        /// <param name="value">Value to use.</param>
        /// <param name="operation">Operation to apply.</param>
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

        /// <summary>
        /// Sets the detail mode for each selected object.
        /// </summary>
        /// <param name="detailMode">Detail mode to set.</param>
        public void SetDetailMode(DetailMode detailMode) => ForEachTimelineObject(timelineObject =>
        {
            switch (timelineObject.TimelineReference)
            {
                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.detailMode = detailMode;
                        RTLevel.Current?.UpdateObject(beatmapObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                        backgroundObject.detailMode = detailMode;
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                        break;
                    }
                case TimelineObject.TimelineReferenceType.PrefabObject: {
                        var prefabObject = timelineObject.GetData<PrefabObject>();
                        prefabObject.detailMode = detailMode;
                        RTLevel.Current?.UpdatePrefab(prefabObject);
                        break;
                    }
            }
        });

        /// <summary>
        /// Sets the object type for each selected object.
        /// </summary>
        /// <param name="objectType">Object type to set.</param>
        public void SetObjectType(BeatmapObject.ObjectType objectType) => ForEachBeatmapObject(timelineObject =>
        {
            var beatmapObject = timelineObject.GetData<BeatmapObject>();
            beatmapObject.objectType = objectType;

            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.OBJECT_TYPE);
        });

        /// <summary>
        /// Sets the color blend mode for each selected object.
        /// </summary>
        /// <param name="colorBlendMode">Color blend mode to set.</param>
        public void SetColorBlendMode(ColorBlendMode colorBlendMode) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.colorBlendMode = colorBlendMode;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });

        /// <summary>
        /// Sets the gradient type for each selected object.
        /// </summary>
        /// <param name="gradientType">Gradient type to set.</param>
        public void SetGradientType(GradientType gradientType) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.gradientType = gradientType;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });

        /// <summary>
        /// Sets the render layer type for each selected object.
        /// </summary>
        /// <param name="renderLayerType">Render layer type to set.</param>
        public void SetRenderLayerType(BeatmapObject.RenderLayerType renderLayerType) => ForEachBeatmapObject(beatmapObject =>
        {
            beatmapObject.renderLayerType = renderLayerType;
            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
        });

        #endregion
    }
}
