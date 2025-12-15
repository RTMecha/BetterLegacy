using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Events;
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
        /* TODO:
        - Cleanup UI generation code.
         */

        #region Values

        public override EventEditor BaseInstance { get => EventEditor.inst; set => EventEditor.inst = value; }

        public EventEditorDialog Dialog { get; set; }
        public MultiKeyframeEditorDialog MultiDialog { get; set; }

        #region Selection

        public TimelineKeyframe CurrentSelectedTimelineKeyframe => EditorTimeline.inst.timelineKeyframes.Find(x => x.Type == EventEditor.inst.currentEventType && x.Index == EventEditor.inst.currentEvent);
        public EventKeyframe CurrentSelectedKeyframe => !GameData.Current ? null : GameData.Current.events[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent];

        public List<TimelineKeyframe> SelectedKeyframes => EditorTimeline.inst.timelineKeyframes.FindAll(x => x.Selected);

        public List<TimelineKeyframe> copiedEventKeyframes = new List<TimelineKeyframe>();

        #endregion

        #region UI

        public Transform eventEditorDialog;

        public static string[] EventTypes => new string[]
        {
            "Move",
            "Zoom",
            "Rotate",
            "Shake",
            "Theme",
            "Chroma",
            "Bloom",
            "Vignette",
            "Lens",
            "Grain",
            "Color Grading",
            "Ripples",
            "Radial Blur",
            "Color Split",
            "Offset",
            "Gradient",
            "Double Vision",
            "Scan Lines",
            "Blur",
            "Pixelize",
            "BG",
            "Invert",
            "Timeline",
            "Player",
            "Follow Player",
            "Audio",
            "Video BG Parent",
            "Video BG",
            "Sharpen",
            "Bars",
            "Danger",
            "3D Rotation",
            "Camera Depth",
            "Window Base",
            "Window Position X",
            "Window Position Y",
            "Player Force",
            "Mosaic",
            "Analog Glitch",
            "Digital Glitch",
        };

        public static List<Color> EventLayerColors => new List<Color>
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

        public Transform eventCopies;
        public Dictionary<string, GameObject> uiDictionary = new Dictionary<string, GameObject>();

        public bool debug = false;

        public List<Image> EventBins { get; set; } = new List<Image>();
        public List<Text> EventLabels { get; set; } = new List<Text>();

        public List<EventBin> eventBins = new List<EventBin>();

        #endregion

        #region Constants

        // Timeline will only ever have up to 15 "bins" and since the 15th bin is the checkpoints, we only need the first 14 bins.
        public const int EVENT_LIMIT = 14;

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

            eventEditorDialog = EditorManager.inst.GetDialog("Event Editor").Dialog;
            EventEditor.inst.EventColors = EventLayerColors;

            EventEditor.inst.dialogLeft = eventEditorDialog.Find("data/left");
            EventEditor.inst.dialogRight = eventEditorDialog.Find("data/right");
            SetEventActive(false);

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
                EventBins.Add(child.GetComponent<Image>());
                EventLabels.Add(child.GetChild(0).GetComponent<Text>());
            }

            for (int i = 0; i < GameData.DefaultKeyframes.Count; i++)
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

        public void DeleteKeyframe(int _type, int _event)
        {
            if (_event == 0)
            {
                EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error);
                return;
            }

            GameData.Current.events[_type].RemoveAt(_event);
            CreateEventObjects();
            RTLevel.Current?.UpdateEvents(_type);
            SetCurrentEvent(_type, _event - 1);
        }

        public IEnumerator DeleteKeyframes() => DeleteKeyframes(SelectedKeyframes);

        public IEnumerator DeleteKeyframes(List<TimelineKeyframe> list)
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

            SetCurrentEvent(type, index);

            EditorManager.inst.DisplayNotification($"Deleted Event Keyframes [ {count} ]", 1f, EditorManager.NotificationType.Success);

            yield break;
        }

        #endregion

        #region Copy / Paste

        public void CopyAllSelectedEvents()
        {
            copiedEventKeyframes.Clear();
            float num = float.PositiveInfinity;
            foreach (var keyframeSelection in SelectedKeyframes)
            {
                if (GameData.Current.events[keyframeSelection.Type][keyframeSelection.Index].time < num)
                    num = GameData.Current.events[keyframeSelection.Type][keyframeSelection.Index].time;
            }

            foreach (var keyframeSelection2 in SelectedKeyframes)
            {
                int type = keyframeSelection2.Type;
                int index = keyframeSelection2.Index;
                var eventKeyframe = GameData.Current.events[type][index].Copy(false);
                eventKeyframe.time -= num;
                var timelineKeyframe = new TimelineKeyframe(eventKeyframe);
                timelineKeyframe.Type = type;
                timelineKeyframe.Index = index;
                copiedEventKeyframes.Add(timelineKeyframe);
            }

            try
            {
                var jn = Parser.NewJSONObject();

                for (int i = 0; i < GameData.Current.events.Count; i++)
                {
                    jn["events"][GameData.EventTypes[i]] = new JSONArray();
                    int add = 0;
                    for (int j = 0; j < GameData.Current.events[i].Count; j++)
                    {
                        if (copiedEventKeyframes.TryFind(x => x.ID == GameData.Current.events[i][j].id, out TimelineKeyframe timelineKeyframe))
                        {
                            var eventKeyframe = timelineKeyframe.eventKeyframe;
                            eventKeyframe.id = LSText.randomNumString(8);

                            jn["events"][GameData.EventTypes[i]][add] = eventKeyframe.ToJSON();

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

        public void PasteEvents(bool setTime = true)
        {
            if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.FileExists($"{Application.persistentDataPath}/copied_events.lsev"))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile($"{Application.persistentDataPath}/copied_events.lsev"));

                copiedEventKeyframes.Clear();

                for (int i = 0; i < GameData.EventTypes.Length; i++)
                {
                    if (jn["events"][GameData.EventTypes[i]] != null)
                    {
                        for (int j = 0; j < jn["events"][GameData.EventTypes[i]].Count; j++)
                        {
                            var d = GameData.DefaultKeyframes[i];
                            var timelineObject = new TimelineKeyframe(EventKeyframe.Parse(jn["events"][GameData.EventTypes[i]][j], i, d.values.Length, d.randomValues.Length, d.values, d.randomValues));
                            timelineObject.Type = i;
                            timelineObject.Index = j;
                            copiedEventKeyframes.Add(timelineObject);
                        }
                    }
                }
            }

            PasteEvents(copiedEventKeyframes, setTime);
        }

        public List<TimelineKeyframe> PasteEvents(List<TimelineKeyframe> kfs, bool setTime = true)
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

                var kf = CreateEventObject(keyframeSelection.Type, index);
                kf.Render();
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

            RenderEventObjects();
            OpenDialog();
            yield break;
        }

        public void DeselectAllKeyframes()
        {
            if (SelectedKeyframes.Count > 0)
                foreach (var timelineObject in SelectedKeyframes)
                    timelineObject.Selected = false;
        }

        public void CreateNewEventObject(int type = 0) => CreateNewEventObject(RTLevel.Current.FixedTime, type);

        public void CreateNewEventObject(float time, int type)
        {
            EventKeyframe eventKeyframe = null;

            if (RTEditor.inst.editorInfo.bpmSnapActive)
                time = RTEditor.SnapToBPM(time);

            int prevIndex = GameData.Current.events[type].FindLastIndex(x => x.time <= time);

            if (prevIndex >= 0)
            {
                eventKeyframe = GameData.Current.events[type][prevIndex].Copy();
                eventKeyframe.time = time;
            }
            else
            {
                eventKeyframe = GameData.DefaultKeyframes[type].Copy();
                eventKeyframe.time = 0f;
            }

            eventKeyframe.locked = false;

            if (type == 2 && EditorConfig.Instance.RotationEventKeyframeResets.Value)
                eventKeyframe.SetValues(new float[1]);

            GameData.Current.events[type].Insert(prevIndex + 1, eventKeyframe);

            var kf = CreateEventObject(type, GameData.Current.events[type].IndexOf(eventKeyframe));
            kf.Render();
            EditorTimeline.inst.timelineKeyframes.Add(kf);

            RTLevel.Current?.UpdateEvents();
            SetCurrentEvent(type, kf.Index);
        }

        public void NewKeyframeFromTimeline(int type)
        {
            if (!(GameData.Current.events.Count > type))
            {
                EditorManager.inst.DisplayNotification("Keyframe type doesn't exist!", 4f, EditorManager.NotificationType.Warning);
                return;
            }

            CreateNewEventObject(EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value), type);
        }

        public void AddSelectedEvent(KeyframeCoord keyframeCoord) => AddSelectedEvent(keyframeCoord.type, keyframeCoord.index);

        public void AddSelectedEvent(int type, int index)
        {
            int kfIndex = 0;
            if (!EditorTimeline.inst.timelineKeyframes.TryFindIndex(x => x.Type == type && x.Index == index, out kfIndex))
            {
                CreateEventObjects();
                kfIndex = EditorTimeline.inst.timelineKeyframes.FindIndex(x => x.Type == type && x.Index == index);
            }

            var kf = EditorTimeline.inst.timelineKeyframes[kfIndex];

            kf.Selected = SelectedKeyframes.Count <= 1 || !kf.Selected;

            EventEditor.inst.currentEventType = type;
            EventEditor.inst.currentEvent = index;
            RenderEventObjects();
            OpenDialog();
        }

        public void SetCurrentEvent(KeyframeCoord keyframeCoord) => SetCurrentEvent(keyframeCoord.type, keyframeCoord.index);

        public void SetCurrentEvent(int type, int index)
        {
            DeselectAllKeyframes();
            AddSelectedEvent(type, index);
        }

        /// <summary>
        /// Gets a keyframe coordinate of the currently selected keyframe.
        /// </summary>
        /// <returns>Returns a <see cref="KeyframeCoord"/>.</returns>
        public KeyframeCoord GetSelectionCoord() => new KeyframeCoord(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);

        #endregion

        #region Timeline Objects

        public void CreateEventObjects()
        {
            foreach (var kf in EditorTimeline.inst.timelineKeyframes)
                Destroy(kf.GameObject);

            EditorTimeline.inst.timelineKeyframes.Clear();

            EventEditor.inst.eventDrag = false;

            for (int type = 0; type < GameData.Current.events.Count; type++)
            {
                for (int index = 0; index < GameData.Current.events[type].Count; index++)
                {
                    var kf = CreateEventObject(type, index);

                    kf.Render();

                    EditorTimeline.inst.timelineKeyframes.Add(kf);
                }
            }
        }

        public TimelineKeyframe CreateEventObject(int type, int index)
        {
            var eventKeyframe = GameData.Current.events[type][index] as EventKeyframe;

            var kf = new TimelineKeyframe(eventKeyframe);
            eventKeyframe.timelineKeyframe = kf;
            kf.Type = type;
            kf.Index = index;
            kf.Init();

            return kf;
        }

        public GameObject EventGameObject(TimelineKeyframe kf) => EventEditor.inst.TimelinePrefab.Duplicate(GetTimelineParent(kf.Type), $"keyframe - {kf.Type}");

        public void RenderEventObjects()
        {
            for (int type = 0; type < GameData.Current.events.Count; type++)
            {
                for (int index = 0; index < GameData.Current.events[type].Count; index++)
                {
                    var kf = EditorTimeline.inst.timelineKeyframes.Find(x => x.Type == type && x.Index == index);

                    if (!kf)
                        kf = EditorTimeline.inst.timelineKeyframes.Find(x => x.ID == GameData.Current.events[type][index].id);

                    if (!kf)
                    {
                        kf = CreateEventObject(type, index);
                        EditorTimeline.inst.timelineKeyframes.Add(kf);
                    }
                    if (!kf.GameObject)
                        kf.GameObject = EventGameObject(kf);
                    kf.Render();
                }
            }
        }

        #endregion

        #region Dialogs

        public void HideDialogs() => LSHelpers.SetActiveChildren(EventEditor.inst.dialogRight, false);

        public void OpenDialog()
        {
            if (SelectedKeyframes.Count > 1 && !SelectedKeyframes.All(x => x.Type == SelectedKeyframes.Min(y => y.Type)))
            {
                MultiDialog.Open();
                RenderMultiEventsDialog();
            }
            else if (SelectedKeyframes.Count > 0)
            {
                Dialog.Open();

                EventEditor.inst.currentEventType = SelectedKeyframes[0].Type;
                EventEditor.inst.currentEvent = SelectedKeyframes[0].Index;

                if (EventEditor.inst.dialogRight.childCount > EventEditor.inst.currentEventType)
                {
                    Debug.Log($"{EventEditor.inst.className}Dialog: {EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType).name}");
                    Dialog.OpenKeyframeDialog(EventEditor.inst.currentEventType);
                    RenderEventsDialog();
                    RenderEventObjects();
                }
                else
                    Debug.LogError($"{EventEditor.inst.className}Keyframe Type {EventEditor.inst.currentEventType} does not currently exist.");
            }
            else
                RTCheckpointEditor.inst.SetCurrentCheckpoint(0);
        }

        public void RenderMultiEventsDialog()
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
                RenderEventObjects();
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
                RenderEventObjects();
            });
            timeStorage.middleButton.onClick.NewListener(() =>
            {
                if (!float.TryParse(time.text, out float num))
                    return;

                num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    kf.eventKeyframe.time = num;

                RTLevel.Current?.UpdateEvents();
                RenderEventObjects();
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
                RenderEventObjects();
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
                RenderEventObjects();
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

        public void RenderEventsDialog()
        {
            RTThemeEditor.inst.Dialog.Editor.SetActive(false);

            RenderTitle(EventEditor.inst.currentEventType);

            Dialog.keyframeDialogs[EventEditor.inst.currentEventType].Render();
        }

        public void CopyKeyframeData(TimelineKeyframe currentKeyframe)
        {
            if (copiedKeyframeDatas.Count > currentKeyframe.Type)
            {
                copiedKeyframeDatas[currentKeyframe.Type] = currentKeyframe.eventKeyframe.Copy();
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            }
            else
                EditorManager.inst.DisplayNotification("Keyframe type does not exist yet.", 2f, EditorManager.NotificationType.Error);
        }

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

                RenderEventsDialog();
                RTLevel.Current?.UpdateEvents(type);
                EditorManager.inst.DisplayNotification($"Pasted {EventTypes[type]} keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
            }
            else if (copiedKeyframeDatas.Count > type)
                EditorManager.inst.DisplayNotification($"{EventTypes[type]} keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification("Keyframe type does not exist yet.", 2f, EditorManager.NotificationType.Error);
        }

        public List<EventKeyframe> copiedKeyframeDatas = new List<EventKeyframe>();

        public void SetKeyframeValue(int index, string input)
        {
            if (RTMath.TryParse(input, 0f, out float value))
                SetKeyframeValue(index, value);
        }

        public void SetKeyframeValue(int index, float value) => SetKeyframeValue(EventEditor.inst.currentEventType, index, value);

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
            var theme = EditorThemeManager.CurrentTheme;
            var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
            var image = title.GetChild(0).GetComponent<Image>();
            image.color = theme.ContainsGroup($"Event Color {GetEventTypeIndex(i) % EVENT_LIMIT + 1} Editor") ? theme.GetColor($"Event Color {GetEventTypeIndex(i) % EVENT_LIMIT + 1} Editor") : Color.white;
            image.color = RTColors.FadeColor(image.color, 1f);
            image.rectTransform.sizeDelta = new Vector2(17f, 0f);
            title.GetChild(1).GetComponent<Text>().text = $"- {(eventBins.TryFind(x => x.index == i, out EventBin eventBin) ? eventBin.name : EventTypes[i])} Editor - ";
        }

        public void RenderLayerBins()
        {
            if (!GameData.Current)
                return;

            var renderLeft = EditorConfig.Instance.EventLabelsRenderLeft.Value;

            var layer = EditorTimeline.inst.Layer + 1;
            //int num = Mathf.Clamp(layer * EVENT_LIMIT, 0, (RTEditor.ShowModdedUI ? layer * EVENT_LIMIT : 10));

            //for (int i = 0; i < GameData.Current.events.Count; i++)
            //{
            //    var text = EventLabels[i % EVENT_LIMIT];

            //    if (eventBins.TryGetAt(i, out EventBin eventBin))
            //    {
            //        if (i >= num - EVENT_LIMIT && i < num)
            //            text.text = eventBin.name;
            //        else if (i < num)
            //            text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NO_EVENT_LABEL;
            //    }
            //    else if (i < EventTypes.Length)
            //    {
            //        if (i >= num - EVENT_LIMIT && i < num)
            //            text.text = EventTypes[i];
            //        else if (i < num)
            //            text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NO_EVENT_LABEL;
            //    }
            //    else
            //        text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NO_EVENT_LABEL;

            //    text.alignment = renderLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

            //    if (!RTEditor.ShowModdedUI && EditorTimeline.inst.Layer > 0)
            //        text.text = "No Event";
            //}

            var theme = EditorThemeManager.CurrentTheme;
            for (int i = 0; i < EVENT_LIMIT + 1; i++) // include checkpoints
            {
                var text = EventLabels[i];
                var img = EventBins[i];

                text.alignment = renderLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

                if (i == EVENT_LIMIT) // checkpoint
                {
                    img.enabled = true;
                    text.enabled = true;
                    img.color = theme.ContainsGroup($"Event Color {i % EVENT_LIMIT + 1}") ? theme.GetColor($"Event Color {i % EVENT_LIMIT + 1}") : Color.white;
                    continue;
                }

                bool enabled;
                var index = i + (EVENT_LIMIT * EditorTimeline.inst.Layer);
                if (eventBins.TryGetAt(index, out EventBin eventBin))
                {
                    enabled = eventBin.IsActive;
                    text.text = eventBin.name;
                }
                else
                {
                    enabled = layer == 69 || layer == 555;
                    text.text = GetNullEventTypeName(layer);
                }

                text.enabled = enabled;
                img.enabled = enabled;
                if (enabled)
                    img.color = theme.ContainsGroup($"Event Color {i % EVENT_LIMIT + 1}") ? theme.GetColor($"Event Color {i % EVENT_LIMIT + 1}") : Color.white;
            }
        }

        public string GetNullEventTypeName(int layer) => layer switch
        {
            69 => "lol",
            555 => "Hahaha",
            _ => NO_EVENT_LABEL,
        };

        public void SetEventActive(bool active)
        {
            EventEditor.inst.EventLabels.SetActive(active);
            EventEditor.inst.EventHolders.SetActive(active);
        }

        public Transform GetTimelineParent(int type)
        {
            //var currentEvent = (eventBins.TryFind(x => x.index == type, out EventBin eventBin) ? eventBin.index : type) % EVENT_LIMIT;
            //var currentEvent = (eventBins.TryFindIndex(x => x.index == type, out int eventBin) ? eventBin : type) % EVENT_LIMIT;
            return BaseInstance.EventHolders.transform.TryGetChild(GetEventTypeIndex(type) % EVENT_LIMIT);
        }

        public int GetEventTypeIndex(int type) => eventBins.TryFindIndex(x => x.index == type, out int eventBin) ? eventBin : type;

        public int GetEventType(int type) => eventBins.TryGetAt(type, out EventBin eventBin) ? eventBin.index : type;

        #endregion

        #endregion
    }
}
