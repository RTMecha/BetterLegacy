using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing <see cref="EventKeyframe"/>s on the <see cref="EditorTimeline.LayerType.Events"/> layer.
    /// <br></br>Wraps <see cref="EventEditor"/>.
    /// </summary>
    public class RTEventEditor : BaseEditor<RTEventEditor, RTEventEditorSettings, EventEditor>
    {
        #region Values

        public override EventEditor BaseInstance { get => EventEditor.inst; set => EventEditor.inst = value; }

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public EventEditorDialog Dialog { get; set; }

        /// <summary>
        /// Multi selection dialog of the editor.
        /// </summary>
        public MultiKeyframeEditorDialog MultiDialog { get; set; }

        /// <summary>
        /// List of actual event bin elements in the event timeline.
        /// </summary>
        public List<EventBinElement> eventBinElements = new List<EventBinElement>();

        /// <summary>
        /// List of event bins used for customizing the event timeline.
        /// </summary>
        public List<EventBin> eventBins = new List<EventBin>();

        #region Selection

        /// <summary>
        /// The currently selected keyframe.
        /// </summary>
        public EventKeyframe CurrentSelectedKeyframe => GameData.Current?.GetEventKeyframe(GetSelectionCoord());

        /// <summary>
        /// List of all selected keyframes.
        /// </summary>
        public List<TimelineKeyframe> SelectedKeyframes => EditorTimeline.inst.timelineKeyframes.FindAll(x => x.Selected);

        /// <summary>
        /// List of copied keyframes.
        /// </summary>
        public List<TimelineKeyframe> copiedEventKeyframes = new List<TimelineKeyframe>();

        /// <summary>
        /// List of copied keyframe datas.
        /// </summary>
        public List<EventKeyframe> copiedKeyframeDatas = new List<EventKeyframe>();

        #endregion

        #region Constants

        /// <summary>
        /// The max displayed event bins.<br></br>
        /// The timeline will only ever have up to 15 "bins" and since the 15th bin is the checkpoints, we only need the first 14 bins.
        /// </summary>
        public const int EVENT_LIMIT = 14;

        /// <summary>
        /// Null event label.
        /// </summary>
        public const string NO_EVENT_LABEL = "??? (No event yet)";

        #endregion

        #endregion

        #region Functions

        public override void OnInit()
        {
            if (AssetPack.TryReadFromFile("editor/data/events.json", out string eventsFile))
            {
                var jn = JSON.Parse(eventsFile);
                for (int i = 0; i < jn["items"].Count; i++)
                    eventBins.Add(new EventBin
                    {
                        name = jn["items"][i]["name"],
                        index = jn["items"][i]["index"].AsInt,
                        complexityPath = jn["items"][i]["complexity_path"],
                    });
            }

            var eventEditorDialog = EditorManager.inst.GetDialog("Event Editor").Dialog;
            EventEditor.inst.EventColors = new List<Color>
            {
                LSColors.HexToColorAlpha("564B6A7F"), // 1
			    LSColors.HexToColorAlpha("41445E7F"), // 2
			    LSColors.HexToColorAlpha("44627A7F"), // 3
			    LSColors.HexToColorAlpha("315B6E7F"), // 4
			    LSColors.HexToColorAlpha("3E6D73FF"), // 5
			    LSColors.HexToColorAlpha("305653FF"), // 6
			    LSColors.HexToColorAlpha("5069517F"), // 7
			    LSColors.HexToColorAlpha("515E417F"), // 8
			    LSColors.HexToColorAlpha("6769457F"), // 9
			    LSColors.HexToColorAlpha("7263357F"), // 10
			    LSColors.HexToColorAlpha("FF98007F"), // 11
			    LSColors.HexToColorAlpha("FF58007F"), // 12
			    LSColors.HexToColorAlpha("FF25097F"), // 13
			    LSColors.HexToColorAlpha("FF0F0F7F"), // 14
			    LSColors.HexToColorAlpha("64B4F67F"), // 15
		    };

            EventEditor.inst.dialogLeft = eventEditorDialog.Find("data/left");
            EventEditor.inst.dialogRight = eventEditorDialog.Find("data/right");
            SetEventTimelineActive(false);

            EditorThemeManager.ApplyGraphic(eventEditorDialog.GetComponent<Image>(), ThemeGroup.Background_3);
            EditorThemeManager.ApplyGraphic(EventEditor.inst.dialogRight.GetComponent<Image>(), ThemeGroup.Background_1);

            HideDialogs();

            var detector = eventEditorDialog.gameObject.GetOrAddComponent<ActiveState>();
            detector.onStateChanged = _val =>
            {
                RTThemeEditor.inst.OnDialog(_val);
                if (!_val)
                    HideDialogs();
            };

            for (int i = 0; i < 15; i++)
            {
                var child = EventEditor.inst.EventLabels.transform.GetChild(i);
                eventBinElements.Add(new EventBinElement(child.GetComponent<Image>(), child.GetChild(0).GetComponent<Text>()));
            }

            for (int i = 0; i < EventLibrary.cachedDefaultKeyframes.Count; i++)
                copiedKeyframeDatas.Add(null);

            try
            {
                Dialog = new EventEditorDialog();
                Dialog.Init();
                MultiDialog = new MultiKeyframeEditorDialog();
                MultiDialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #region Deleting

        /// <summary>
        /// Deletes a singular event keyframe.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="index">Index of the event keyframe.</param>
        public void DeleteKeyframe(int type, int index) => DeleteKeyframe(new KeyframeCoord(type, index));

        /// <summary>
        /// Deletes a singular event keyframe.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the event keyframe.</param>
        public void DeleteKeyframe(KeyframeCoord keyframeCoord)
        {
            if (keyframeCoord.type == 0)
            {
                EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error);
                return;
            }

            GameData.Current.events[keyframeCoord.type].RemoveAt(keyframeCoord.index);
            CreateTimelineKeyframes();
            RTLevel.Current?.UpdateEvents(keyframeCoord.type);
            SetCurrentKeyframe(keyframeCoord.type, keyframeCoord.index - 1);
        }

        /// <summary>
        /// Deletes all selected keyframes.
        /// </summary>
        public void DeleteKeyframes() => CoroutineHelper.StartCoroutine(IDeleteKeyframes());

        /// <summary>
        /// Deletes all keyframes on a list.
        /// </summary>
        /// <param name="list">List of keyframes to delete.</param>
        public void DeleteKeyframes(List<TimelineKeyframe> list) => CoroutineHelper.StartCoroutine(IDeleteKeyframes(list));

        /// <summary>
        /// Deletes all selected keyframes.
        /// </summary>
        public IEnumerator IDeleteKeyframes() => IDeleteKeyframes(SelectedKeyframes);

        /// <summary>
        /// Deletes all keyframes on a list.
        /// </summary>
        /// <param name="list">List of keyframes to delete.</param>
        public IEnumerator IDeleteKeyframes(List<TimelineKeyframe> list)
        {
            var count = list.Count;
            var types = list.Select(x => x.Type);
            var typesCount = types.Count();

            int type = 0;
            int index = 0;
            if (count > 0)
            {
                if (typesCount == 1)
                {
                    type = list[0].Type;
                    index = list[0].Index - 1;
                    if (index < 0)
                        index = 0;
                }
                else
                    type = list[0].Type;
            }

            EditorTimeline.inst.timelineKeyframes.ForLoopReverse((timelineKeyframe, index) =>
            {
                if (!timelineKeyframe.Selected || timelineKeyframe.Index == 0)
                    return;

                CoreHelper.Delete(timelineKeyframe.GameObject);
                EditorTimeline.inst.timelineKeyframes.RemoveAt(index);
                GameData.Current.events[timelineKeyframe.Type].Remove(timelineKeyframe.eventKeyframe);
            });

            RTLevel.Current?.UpdateEvents();

            SetCurrentKeyframe(type, index);

            EditorManager.inst.DisplayNotification($"Deleted Event Keyframes [ {count} ]", 1f, EditorManager.NotificationType.Success);

            yield break;
        }

        #endregion

        #region Copy / Paste

        /// <summary>
        /// Copies all selected keyframes.
        /// </summary>
        public void CopyKeyframes()
        {
            copiedEventKeyframes.Clear();
            float num = float.PositiveInfinity;
            var selectedKeyframes = SelectedKeyframes;
            foreach (var selectedTimelineKeyframe in selectedKeyframes)
            {
                var eventKeyframe = selectedTimelineKeyframe.eventKeyframe;
                if (eventKeyframe.time < num)
                    num = eventKeyframe.time;
            }

            foreach (var selectedTimelineKeyframe in selectedKeyframes)
            {
                var coord = selectedTimelineKeyframe.GetCoord();
                var eventKeyframe = GameData.Current.GetEventKeyframe(coord).Copy(false);
                eventKeyframe.time -= num;
                var timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                timelineKeyframe.SetCoord(coord);
                copiedEventKeyframes.Add(timelineKeyframe);
            }

            try
            {
                var jn = Parser.NewJSONObject();

                for (int i = 0; i < GameData.Current.events.Count; i++)
                {
                    jn["events"][EventLibrary.jsonNames[i]] = new JSONArray();
                    int add = 0;
                    for (int j = 0; j < GameData.Current.events[i].Count; j++)
                    {
                        if (copiedEventKeyframes.TryFind(x => x.ID == GameData.Current.events[i][j].id, out TimelineKeyframe timelineKeyframe))
                        {
                            var eventKeyframe = timelineKeyframe.eventKeyframe;
                            eventKeyframe.id = LSText.randomNumString(8);

                            jn["events"][EventLibrary.jsonNames[i]][add] = eventKeyframe.ToJSON();

                            add++;
                        }
                    }
                }

                RTFile.WriteToFile($"{Application.persistentDataPath}/copied_events.lsev", jn.ToString());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't save persistent copy.\n{ex}");
            }
        }

        /// <summary>
        /// Pastes the copied keyframes into the level.
        /// </summary>
        /// <param name="setTime">If the keyframe time should be modified.</param>
        public void PasteKeyframes(bool setTime = true)
        {
            if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.FileExists($"{Application.persistentDataPath}/copied_events.lsev"))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile($"{Application.persistentDataPath}/copied_events.lsev"));

                copiedEventKeyframes.Clear();

                for (int i = 0; i < EventLibrary.jsonNames.Length; i++)
                {
                    if (jn["events"][EventLibrary.jsonNames[i]] != null)
                    {
                        for (int j = 0; j < jn["events"][EventLibrary.jsonNames[i]].Count; j++)
                        {
                            var d = EventLibrary.cachedDefaultKeyframes[i];
                            var timelineKeyframe = new TimelineKeyframe(EventKeyframe.Parse(jn["events"][EventLibrary.jsonNames[i]][j], i, d.values.Length, d.randomValues.Length, d.values, d.randomValues));
                            timelineKeyframe.SetCoord(new KeyframeCoord(i, j));
                            copiedEventKeyframes.Add(timelineKeyframe);
                        }
                    }
                }
            }

            PasteKeyframes(copiedEventKeyframes, setTime);
        }

        /// <summary>
        /// Pastes a list of keyframes into the level.
        /// </summary>
        /// <param name="kfs">Keyframes to paste.</param>
        /// <param name="setTime">If the keyframe time should be modified.</param>
        /// <returns>Returns the pasted list of keyframes.</returns>
        public List<TimelineKeyframe> PasteKeyframes(List<TimelineKeyframe> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                CoreHelper.LogError($"No copied event yet!");
                return null;
            }

            var pastedKeyframes = new List<TimelineKeyframe>();

            var selectPasted = EditorConfig.Instance.SelectPasted.Value;
            if (selectPasted)
                EditorTimeline.inst.timelineKeyframes.ForEach(x => x.Selected = false);

            var time = EditorManager.inst.CurrentAudioPos;
            if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPasted.Value)
                time = RTEditor.SnapToBPM(time);

            foreach (var keyframeSelection in kfs)
            {
                var eventKeyframe = keyframeSelection.eventKeyframe.Copy();
                if (setTime)
                    eventKeyframe.time = time + eventKeyframe.time;

                var index = GameData.Current.events[keyframeSelection.Type].FindIndex(x => x.time > eventKeyframe.time) - 1;
                if (index < 0)
                    index = GameData.Current.events[keyframeSelection.Type].Count;

                GameData.Current.events[keyframeSelection.Type].Insert(index, eventKeyframe);

                var kf = CreateTimelineKeyframe(keyframeSelection.Type, index);
                if (selectPasted)
                    kf.Selected = true;
                EditorTimeline.inst.timelineKeyframes.Add(kf);
                pastedKeyframes.Add(kf);
            }

            RTLevel.Current?.UpdateEvents();
            OpenDialog();
            return pastedKeyframes;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Handles selection box drag.
        /// </summary>
        /// <param name="add">If selection should be added to.</param>
        /// <param name="remove">If the currently selected keyframes should be deselected.</param>
        public IEnumerator GroupSelectKeyframes(bool add, bool remove)
        {
            var list = EditorTimeline.inst.timelineKeyframes;

            if (!add && !remove)
                DeselectAllKeyframes();

            list.Where(x => x.IsCurrentLayer && RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList()
            .ForEach(x =>
            {
                x.Selected = true;
                x.timeOffset = 0f;
            });

            RenderTimelineKeyframes();
            OpenDialog();
            yield break;
        }

        /// <summary>
        /// Deselects all selected keyframes.
        /// </summary>
        public void DeselectAllKeyframes()
        {
            var selectedKeyframes = SelectedKeyframes;
            if (selectedKeyframes.Count > 0)
                foreach (var timelineKeyframe in selectedKeyframes)
                    timelineKeyframe.Selected = false;
        }

        /// <summary>
        /// Creates a new event keyframe.
        /// </summary>
        /// <param name="type">Type of the event keyframe to create.</param>
        public void CreateNewEventKeyframe(int type) => CreateNewEventKeyframe(RTLevel.Current.FixedTime, type);

        /// <summary>
        /// Creates a new event keyframe.
        /// </summary>
        /// <param name="time">Time of the keyframe.</param>
        /// <param name="type">Type of the event keyframe to create.</param>
        public void CreateNewEventKeyframe(float time, int type)
        {
            if (RTEditor.inst.editorInfo.bpmSnapActive)
                time = RTEditor.SnapToBPM(time);

            // handle empty list.
            if (GameData.Current.events[type].IsEmpty())
                GameData.Current.events[type].Add(EventLibrary.cachedDefaultKeyframes[type].Copy());

            int prevIndex = GameData.Current.events[type].FindLastIndex(x => x.time <= time);
            if (prevIndex < 0)
            {
                EditorManager.inst.DisplayNotification("No previous keyframe was found.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var eventKeyframe = GameData.Current.events[type][prevIndex].Copy();
            eventKeyframe.time = time;

            eventKeyframe.locked = false;

            if (type == 2 && EditorConfig.Instance.RotationEventKeyframeResets.Value)
                eventKeyframe.SetValues(new float[1]);

            GameData.Current.events[type].Insert(prevIndex + 1, eventKeyframe);

            var kf = CreateTimelineKeyframe(type, Mathf.Clamp(prevIndex + 1, 0, GameData.Current.events[type].Count - 1));
            EditorTimeline.inst.timelineKeyframes.Add(kf);

            RTLevel.Current?.UpdateEvents();
            SetCurrentKeyframe(type, kf.Index);
        }

        /// <summary>
        /// Selects a keyframe at a coordinate.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="index">Index of the event keyframe.</param>
        public void AddSelectedKeyframe(int type, int index) => AddSelectedKeyframe(new KeyframeCoord(type, index));

        /// <summary>
        /// Selects a keyframe at a coordinate.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the keyframe to select.</param>
        public void AddSelectedKeyframe(KeyframeCoord keyframeCoord)
        {
            CoreHelper.Log($"Selecting {keyframeCoord}");

            var eventKeyframe = GameData.Current.GetEventKeyframe(keyframeCoord);
            if (!eventKeyframe.timelineKeyframe)
                CreateTimelineKeyframe(eventKeyframe, keyframeCoord);

            eventKeyframe.timelineKeyframe.Selected = SelectedKeyframes.Count <= 1 || !eventKeyframe.timelineKeyframe.Selected;

            SetSelectionCoord(keyframeCoord);
            OpenDialog();
        }

        /// <summary>
        /// Sets the current keyframe at a coordinate.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="index">Index of the event keyframe.</param>
        public void SetCurrentKeyframe(int type, int index) => SetCurrentKeyframe(new KeyframeCoord(type, index));

        /// <summary>
        /// Sets the current keyframe at a coordinate.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the keyframe to select.</param>
        public void SetCurrentKeyframe(KeyframeCoord keyframeCoord)
        {
            DeselectAllKeyframes();
            AddSelectedKeyframe(keyframeCoord);
        }

        /// <summary>
        /// Gets a keyframe coordinate of the currently selected keyframe.
        /// </summary>
        /// <returns>Returns a <see cref="KeyframeCoord"/>.</returns>
        public KeyframeCoord GetSelectionCoord() => new KeyframeCoord(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);

        /// <summary>
        /// Sets the currently selected keyframe coordinate.
        /// </summary>
        /// <param name="keyframeCoord">Keyframe coordinates to set.</param>
        public void SetSelectionCoord(KeyframeCoord keyframeCoord)
        {
            EventEditor.inst.currentEventType = keyframeCoord.type;
            EventEditor.inst.currentEvent = keyframeCoord.index;
        }

        #endregion

        #region Timeline Keyframes

        /// <summary>
        /// Initializes the timeline keyframes.
        /// </summary>
        public void CreateTimelineKeyframes()
        {
            EventEditor.inst.eventDrag = false;

            foreach (var kf in EditorTimeline.inst.timelineKeyframes)
            {
                if (kf.eventKeyframe)
                    kf.eventKeyframe.timelineKeyframe = null;
                kf.eventKeyframe = null;
                CoreHelper.Delete(kf.GameObject);
            }
            EditorTimeline.inst.timelineKeyframes.Clear();

            for (int type = 0; type < GameData.Current.events.Count; type++)
            {
                for (int index = 0; index < GameData.Current.events[type].Count; index++)
                {
                    var coord = new KeyframeCoord(type, index);
                    var eventKeyframe = GameData.Current.GetEventKeyframe(coord);
                    EditorTimeline.inst.timelineKeyframes.Add(CreateTimelineKeyframe(eventKeyframe, coord));
                }
            }
        }

        /// <summary>
        /// Initializes a single timeline keyframe.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="index">Index of the event keyframe.</param>
        /// <returns>Returns a new <see cref="TimelineKeyframe"/> for the found event keyframe.</returns>
        public TimelineKeyframe CreateTimelineKeyframe(int type, int index) => CreateTimelineKeyframe(new KeyframeCoord(type, index));

        /// <summary>
        /// Initializes a single timeline keyframe.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the keyframe.</param>
        /// <returns>Returns a new <see cref="TimelineKeyframe"/> for the found event keyframe.</returns>
        public TimelineKeyframe CreateTimelineKeyframe(KeyframeCoord keyframeCoord) => CreateTimelineKeyframe(GameData.Current.GetEventKeyframe(keyframeCoord), keyframeCoord);

        /// <summary>
        /// Initializes a single timeline keyframe.
        /// </summary>
        /// <param name="eventKeyframe">Event keyframe to create a timeline keyframe for.</param>
        /// <param name="keyframeCoord">Coordinates of the keyframe.</param>
        /// <returns>Returns a new <see cref="TimelineKeyframe"/> for the event keyframe.</returns>
        public TimelineKeyframe CreateTimelineKeyframe(EventKeyframe eventKeyframe, KeyframeCoord keyframeCoord)
        {
            var timelineKeyframe = new TimelineKeyframe(eventKeyframe);
            timelineKeyframe.SetCoord(keyframeCoord);
            timelineKeyframe.Init(true);
            return timelineKeyframe;
        }

        /// <summary>
        /// Renders the timeline keyframes.
        /// </summary>
        public void RenderTimelineKeyframes()
        {
            for (int type = 0; type < GameData.Current.events.Count; type++)
                RenderTimelineKeyframes(type);
        }

        /// <summary>
        /// Renders the timeline keyframes.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        public void RenderTimelineKeyframes(int type)
        {
            for (int index = 0; index < GameData.Current.events[type].Count; index++)
                RenderTimelineKeyframe(new KeyframeCoord(type, index));
        }

        /// <summary>
        /// Renders a timeline keyframe at a coordinate.
        /// </summary>
        /// <param name="coord">Coordinates of the keyframe.</param>
        public void RenderTimelineKeyframe(KeyframeCoord coord)
        {
            var eventKeyframe = GameData.Current.GetEventKeyframe(coord);
            if (!eventKeyframe.timelineKeyframe)
                EditorTimeline.inst.timelineKeyframes.Add(CreateTimelineKeyframe(eventKeyframe, coord));
            else if (!eventKeyframe.timelineKeyframe.GameObject)
                eventKeyframe.timelineKeyframe.Init(true);
            else
                eventKeyframe.timelineKeyframe.Render();
        }

        #endregion

        #region Dialogs

        /// <summary>
        /// Hides all sub-dialogs of the event editor dialog.
        /// </summary>
        public void HideDialogs() => LSHelpers.SetActiveChildren(EventEditor.inst.dialogRight, false);

        /// <summary>
        /// Opens the event editor dialog.
        /// </summary>
        public void OpenDialog()
        {
            var selectedKeyframes = SelectedKeyframes;
            if (selectedKeyframes.Count > 1 && !selectedKeyframes.All(x => x.Type == selectedKeyframes.Min(y => y.Type)))
                OpenMultiDialog();
            else if (selectedKeyframes.Count > 0)
                OpenSingleDialog();
            else
                RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
        }

        /// <summary>
        /// Opens the multi events dialog.
        /// </summary>
        public void OpenMultiDialog()
        {
            MultiDialog.Open();
            RenderMultiDialog();
        }

        /// <summary>
        /// Renders the multi events dialog.
        /// </summary>
        public void RenderMultiDialog()
        {
            var dialog = MultiDialog.GameObject.transform.Find("data");
            var timeStorage = dialog.Find("time").GetComponent<InputFieldStorage>();
            var time = timeStorage.inputField;
            timeStorage.leftGreaterButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                {
                    var eventKeyframe = kf.eventKeyframe;
                    eventKeyframe.time = Mathf.Clamp(eventKeyframe.time - (num * 10f), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                }

                RTLevel.Current?.UpdateEvents();
                RenderTimelineKeyframes();
            });
            timeStorage.leftButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                {
                    var eventKeyframe = kf.eventKeyframe;
                    eventKeyframe.time = Mathf.Clamp(eventKeyframe.time + num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                }

                RTLevel.Current?.UpdateEvents();
                RenderTimelineKeyframes();
            });
            timeStorage.middleButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    kf.eventKeyframe.time = num;

                RTLevel.Current?.UpdateEvents();
                RenderTimelineKeyframes();
            });
            timeStorage.rightButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                {
                    var eventKeyframe = kf.eventKeyframe;
                    eventKeyframe.time = Mathf.Clamp(eventKeyframe.time - num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                }

                RTLevel.Current?.UpdateEvents();
                RenderTimelineKeyframes();
            });
            timeStorage.rightGreaterButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                {
                    var eventKeyframe = kf.eventKeyframe;
                    eventKeyframe.time = Mathf.Clamp(eventKeyframe.time + (num * 10f), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                }

                RTLevel.Current?.UpdateEvents();
                RenderTimelineKeyframes();
            });

            TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

            var curves = dialog.Find("curves").GetComponent<Dropdown>();
            curves.onValueChanged.NewListener(_val =>
            {
                var anim = RTEditor.inst.GetEasing(_val);
                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    kf.eventKeyframe.curve = anim;

                RTLevel.Current?.UpdateEvents();
            });

            var valueIndexStorage = dialog.Find("value index").GetComponent<InputFieldStorage>();
            valueIndexStorage.OnValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int n))
                    valueIndexStorage.inputField.text = "0";
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(valueIndexStorage.inputField, t: valueIndexStorage.transform);
            TriggerHelper.AddEventTriggers(valueIndexStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(valueIndexStorage.inputField));

            var valueStorage = dialog.Find("value").GetComponent<InputFieldStorage>();
            valueStorage.leftGreaterButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(valueStorage.inputField.text, out float num))
                    return;

                foreach (var kf in SelectedKeyframes)
                {
                    var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                    index = Mathf.Clamp(index, 0, kf.eventKeyframe.values.Length - 1);
                    kf.eventKeyframe.values[index] -= num * 10f;
                }
            });
            valueStorage.leftButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(valueStorage.inputField.text, out float num))
                    return;

                foreach (var kf in SelectedKeyframes)
                {
                    var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                    index = Mathf.Clamp(index, 0, kf.eventKeyframe.values.Length - 1);
                    kf.eventKeyframe.values[index] -= num;
                }
            });
            valueStorage.middleButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(valueStorage.inputField.text, out float num))
                    return;

                foreach (var kf in SelectedKeyframes)
                {
                    var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                    index = Mathf.Clamp(index, 0, kf.eventKeyframe.values.Length - 1);
                    kf.eventKeyframe.values[index] = num;
                }
            });
            valueStorage.rightButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(valueStorage.inputField.text, out float num))
                    return;

                foreach (var kf in SelectedKeyframes)
                {
                    var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                    index = Mathf.Clamp(index, 0, kf.eventKeyframe.values.Length - 1);
                    kf.eventKeyframe.values[index] += num;
                }
            });
            valueStorage.rightGreaterButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(valueStorage.inputField.text, out float num))
                    return;

                foreach (var kf in SelectedKeyframes)
                {
                    var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                    index = Mathf.Clamp(index, 0, kf.eventKeyframe.values.Length - 1);
                    kf.eventKeyframe.values[index] += num * 10f;
                }
            });

            TriggerHelper.AddEventTriggers(valueStorage.inputField.gameObject, TriggerHelper.ScrollDelta(valueStorage.inputField));
        }

        /// <summary>
        /// Opens the current event editor dialog.
        /// </summary>
        public void OpenSingleDialog()
        {
            Dialog.Open();

            var coord = SelectedKeyframes[0].GetCoord();
            SetSelectionCoord(coord);

            if (coord.type < EventLibrary.displayNames.Length)
            {
                Debug.Log($"{EventEditor.inst.className}Editing {EventLibrary.displayNames[coord.type]}");
                Dialog.OpenKeyframeDialog(coord.type);
                RenderDialog();
                RenderTimelineKeyframes();
            }
            else
                Debug.LogError($"{EventEditor.inst.className}Keyframe Type {coord.type} does not currently exist.");
        }

        /// <summary>
        /// Renders the current event editor dialog.
        /// </summary>
        public void RenderDialog()
        {
            RTThemeEditor.inst.Dialog.Editor.SetActive(false);
            RenderTitle(EventEditor.inst.currentEventType);
            Dialog.keyframeDialogs[EventEditor.inst.currentEventType].Render();
        }

        /// <summary>
        /// Copies a keyframe's data.
        /// </summary>
        /// <param name="currentKeyframe">Keyframe to copy the data of.</param>
        public void CopyKeyframeData(TimelineKeyframe currentKeyframe)
        {
            if (!currentKeyframe)
            {
                EditorManager.inst.DisplayNotification("No selected keyframe!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (copiedKeyframeDatas.Count > currentKeyframe.Type)
            {
                copiedKeyframeDatas[currentKeyframe.Type] = currentKeyframe.eventKeyframe.Copy();
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            }
            else
                EditorManager.inst.DisplayNotification("Keyframe type does not exist yet.", 2f, EditorManager.NotificationType.Error);
        }

        /// <summary>
        /// Pastes the copied keyframe data onto all selected keyframes.
        /// </summary>
        /// <param name="type">Type of the keyframe to paste.</param>
        public void PasteKeyframeData(int type)
        {
            if (copiedKeyframeDatas.Count > type && copiedKeyframeDatas[type] != null)
            {
                foreach (var keyframe in SelectedKeyframes.Where(x => x.Type == type))
                {
                    var kf = keyframe.eventKeyframe;

                    kf.curve = copiedKeyframeDatas[type].curve;
                    kf.values = copiedKeyframeDatas[type].values.Copy();
                    kf.randomValues = copiedKeyframeDatas[type].randomValues.Copy();
                    kf.random = copiedKeyframeDatas[type].random;
                    kf.relative = copiedKeyframeDatas[type].relative;
                }

                RenderDialog();
                RTLevel.Current?.UpdateEvents(type);
                EditorManager.inst.DisplayNotification($"Pasted {EventLibrary.displayNames[type]} keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
            }
            else if (copiedKeyframeDatas.Count > type)
                EditorManager.inst.DisplayNotification($"{EventLibrary.displayNames[type]} keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification("Keyframe type does not exist yet.", 2f, EditorManager.NotificationType.Error);
        }

        /// <summary>
        /// Sets the value of all selected keyframes that match the type.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <param name="input">Input to parse.</param>
        public void SetKeyframeValue(int index, string input)
        {
            if (RTMath.TryParse(input, 0f, out float value))
                SetKeyframeValue(index, value);
        }

        /// <summary>
        /// Sets the value of all selected keyframes that match the type.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <param name="value">Value to set.</param>
        public void SetKeyframeValue(int index, float value) => SetKeyframeValue(EventEditor.inst.currentEventType, index, value);

        /// <summary>
        /// Sets the value of all selected keyframes that match the type.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="index">Index of the value.</param>
        /// <param name="value">Value to set.</param>
        public void SetKeyframeValue(int type, int index, float value)
        {
            foreach (var timelineKeyframe in SelectedKeyframes.Where(x => x.Type == type))
                timelineKeyframe.eventKeyframe.values[index] = value;
            RTLevel.Current?.UpdateEvents(type);
        }

        #endregion

        #region Rendering

        void RenderTitle(int i)
        {
            var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
            var image = title.GetChild(0).GetComponent<Image>();
            image.color = EditorThemeManager.CurrentTheme.ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorEditorThemeGroup(GetEventTypeIndex(i) % EVENT_LIMIT), Color.white);
            image.color = RTColors.FadeColor(image.color, 1f);
            image.rectTransform.sizeDelta = new Vector2(17f, 0f);
            title.GetChild(1).GetComponent<Text>().text = $"- {(eventBins.TryFind(x => x.index == i, out EventBin eventBin) ? eventBin.name : EventLibrary.displayNames[i])} Editor - ";
        }

        /// <summary>
        /// Renders the event bins on the editor timeline.
        /// </summary>
        public void RenderLayerBins()
        {
            if (!GameData.Current)
                return;

            var renderLeft = EditorConfig.Instance.EventLabelsRenderLeft.Value;

            var layer = EditorTimeline.inst.Layer + 1;
            var theme = EditorThemeManager.CurrentTheme;
            for (int i = 0; i < EVENT_LIMIT + 1; i++) // include checkpoints
            {
                var eventBinElement = eventBinElements[i];
                eventBinElement.SetAlignment(renderLeft);

                if (i == EVENT_LIMIT) // checkpoint
                {
                    eventBinElement.SetActive(true);
                    eventBinElement.Color = theme.ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorThemeGroup(i % EVENT_LIMIT), Color.white);
                    continue;
                }

                bool enabled;
                var index = i + (EVENT_LIMIT * EditorTimeline.inst.Layer);
                if (eventBins.TryGetAt(index, out EventBin eventBin))
                {
                    enabled = eventBin.IsActive;
                    eventBinElement.Text = eventBin.name;
                }
                else
                {
                    enabled = layer == 69 || layer == 555;
                    eventBinElement.Text = GetNullEventTypeName(layer);
                }

                eventBinElement.SetActive(enabled);
                if (enabled)
                    eventBinElement.Color = theme.ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorThemeGroup(i % EVENT_LIMIT), Color.white);
            }
        }

        static string GetNullEventTypeName(int layer) => layer switch
        {
            69 => "lol",
            555 => "Hahaha",
            _ => NO_EVENT_LABEL,
        };

        /// <summary>
        /// Sets the event timeline active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetEventTimelineActive(bool active)
        {
            EventEditor.inst.EventLabels.SetActive(active);
            EventEditor.inst.EventHolders.SetActive(active);
        }

        /// <summary>
        /// Gets the bin a keyframe should be parented to.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns the found parent.</returns>
        public Transform GetTimelineParent(int type)
        {
            //var currentEvent = (eventBins.TryFind(x => x.index == type, out EventBin eventBin) ? eventBin.index : type) % EVENT_LIMIT;
            //var currentEvent = (eventBins.TryFindIndex(x => x.index == type, out int eventBin) ? eventBin : type) % EVENT_LIMIT;
            return BaseInstance.EventHolders.transform.TryGetChild(GetEventTypeIndex(type) % EVENT_LIMIT);
        }

        /// <summary>
        /// Converts the actual event index to an event bin row. Used for custom event bin order via Asset Packs.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns the converted event.</returns>
        public int GetEventTypeIndex(int type) => eventBins.TryFindIndex(x => x.index == type, out int eventBin) ? eventBin : type;

        /// <summary>
        /// Converts the event bin row to an actual event index. Used for custom event bin order via Asset Packs.
        /// </summary>
        /// <param name="type">Index of the event bin.</param>
        /// <returns>Returns the converted event.</returns>
        public int GetEventType(int type) => eventBins.TryGetAt(type, out EventBin eventBin) ? eventBin.index : type;

        #endregion

        #endregion

        /// <summary>
        /// Represents an actual event bin in the event timeline.
        /// </summary>
        public class EventBinElement : Exists
        {
            public EventBinElement(Image image, Text label)
            {
                this.image = image;
                this.label = label;
            }

            /// <summary>
            /// Image of the event bin.
            /// </summary>
            public Image image;

            /// <summary>
            /// Label of the event bin.
            /// </summary>
            public Text label;

            /// <summary>
            /// Text of the event bin label.
            /// </summary>
            public string Text
            {
                get => label.text;
                set => label.text = value;
            }

            /// <summary>
            /// Color of the event bin.
            /// </summary>
            public Color Color
            {
                get => image.color;
                set => image.color = value;
            }

            /// <summary>
            /// Sets the active state of the event bin.
            /// </summary>
            /// <param name="active">Active state to set.</param>
            public void SetActive(bool active)
            {
                image.enabled = active;
                label.enabled = active;
            }

            /// <summary>
            /// Sets the alignment of the label.
            /// </summary>
            /// <param name="renderLeft">Alignment to set to the label.</param>
            public void SetAlignment(bool renderLeft) => label.alignment = renderLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        }
    }
}
