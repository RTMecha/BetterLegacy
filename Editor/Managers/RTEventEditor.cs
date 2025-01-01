using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;

namespace BetterLegacy.Editor.Managers
{
    public class RTEventEditor : MonoBehaviour
    {
        public static RTEventEditor inst;

        #region Variables

        public static List<List<BaseEventKeyframe>> AllEvents => !GameData.IsValid ? null : GameData.Current.eventObjects.allEvents;

        #region Selection

        public TimelineObject CurrentSelectedTimelineKeyframe => RTEditor.inst.timelineKeyframes.Find(x => x.Type == EventEditor.inst.currentEventType && x.Index == EventEditor.inst.currentEvent);
        public EventKeyframe CurrentSelectedKeyframe => !GameData.IsValid ? null : (EventKeyframe)GameData.Current.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent];

        public List<TimelineObject> SelectedKeyframes => RTEditor.inst.timelineKeyframes.FindAll(x => x.Selected);

        public List<TimelineObject> copiedEventKeyframes = new List<TimelineObject>();

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

        #endregion

        #region Color Toggles

        public List<Toggle> vignetteColorButtons = new List<Toggle>();
        public List<Toggle> bloomColorButtons = new List<Toggle>();
        public List<Toggle> gradientColor1Buttons = new List<Toggle>();
        public List<Toggle> gradientColor2Buttons = new List<Toggle>();
        public List<Toggle> bgColorButtons = new List<Toggle>();
        public List<Toggle> overlayColorButtons = new List<Toggle>();
        public List<Toggle> timelineColorButtons = new List<Toggle>();
        public List<Toggle> dangerColorButtons = new List<Toggle>();

        #endregion

        #region Constants

        // Timeline will only ever have up to 15 "bins" and since the 15th bin is the checkpoints, we only need the first 14 bins.
        public const int EVENT_LIMIT = 14;

        public const string NO_EVENT_LABEL = "??? (No event yet)";

        #endregion

        #endregion

        public static void Init() => EventEditor.inst?.gameObject?.AddComponent<RTEventEditor>();

        void Awake()
        {
            inst = this;

            eventEditorDialog = EditorManager.inst.GetDialog("Event Editor").Dialog;
            EventEditor.inst.EventColors = EventLayerColors;

            EventEditor.inst.dialogLeft = eventEditorDialog.Find("data/left");
            EventEditor.inst.dialogRight = eventEditorDialog.Find("data/right");
            SetEventActive(false);

            EditorThemeManager.AddGraphic(eventEditorDialog.GetComponent<Image>(), ThemeGroup.Background_3);
            EditorThemeManager.AddGraphic(EventEditor.inst.dialogRight.GetComponent<Image>(), ThemeGroup.Background_1);

            for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
            {
                var dialog = EventEditor.inst.dialogRight.GetChild(i);
                dialog.gameObject.SetActive(false);
                dialog.Find("curves_label").GetChild(0).GetComponent<Text>().text = "Ease Type";

                var topPanel = dialog.GetChild(0);
                var bg = topPanel.GetChild(0).GetComponent<Image>();
                var title = topPanel.GetChild(1).GetComponent<Text>();
                bg.gameObject.AddComponent<ContrastColors>().Init(title, bg);

                var edit = dialog.Find("edit");
                for (int j = 0; j < edit.childCount; j++)
                {
                    var button = edit.GetChild(j);
                    var buttonComponent = button.GetComponent<Button>();

                    if (!buttonComponent)
                        continue;

                    if (button.name == "del")
                    {
                        var buttonBG = button.GetChild(0).GetComponent<Image>();

                        EditorThemeManager.AddGraphic(buttonBG, ThemeGroup.Delete_Keyframe_BG);
                        EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Delete_Keyframe_Button, false);

                        continue;
                    }

                    Destroy(button.GetComponent<Animator>());
                    buttonComponent.transition = Selectable.Transition.ColorTint;
                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                }

                // Labels
                for (int j = 0; j < dialog.childCount; j++)
                {
                    var label = dialog.GetChild(j);

                    if (!(label.name == "label" || label.name == "curves_label"))
                        continue;

                    for (int k = 0; k < label.childCount; k++)
                        EditorThemeManager.AddLightText(label.GetChild(k).GetComponent<Text>());
                }

                var timeBase = dialog.Find("time");
                var timeInput = timeBase.Find("time").GetComponent<InputField>();

                EditorThemeManager.AddInputField(timeInput);

                for (int j = 1; j < timeBase.childCount; j++)
                {
                    var button = timeBase.GetChild(j);
                    var buttonComponent = button.GetComponent<Button>();

                    if (!buttonComponent)
                        continue;

                    Destroy(button.GetComponent<Animator>());
                    buttonComponent.transition = Selectable.Transition.ColorTint;
                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                }

                EditorThemeManager.AddDropdown(dialog.Find("curves").GetComponent<Dropdown>());

                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 6:
                    case 8:
                        {
                            var valueNames = new Dictionary<int, string>
                            {
                                { 0, "position" },
                                { 1, "zoom" },
                                { 2, "rotation" },
                                { 3, "shake" },
                                { 5, "chroma" },
                                { 6, "bloom" },
                                { 8, "lens" },
                            };

                            var positionBase = dialog.Find(valueNames[i]);
                            EditorThemeManager.AddInputFields(positionBase.gameObject, true, "Event Editor");

                            break;
                        }
                    case 4:
                        {
                            var themesSearch = dialog.Find("theme-search").GetComponent<InputField>();

                            EditorThemeManager.AddInputField(themesSearch, ThemeGroup.Search_Field_2);

                            var themes = dialog.Find("themes").GetComponent<Image>();

                            var contextClickable = themes.gameObject.AddComponent<ContextClickable>();
                            contextClickable.onClick = eventData =>
                            {
                                if (eventData.button != PointerEventData.InputButton.Right)
                                    return;

                                RTEditor.inst.ShowContextMenu(300f,
                                    new RTEditor.ButtonFunction("Create folder", () =>
                                    {
                                        RTEditor.inst.ShowFolderCreator($"{RTFile.ApplicationDirectory}{RTEditor.themeListPath}", () => { RTEditor.inst.UpdateThemePath(true); RTEditor.inst.HideNameEditor(); });
                                    }),
                                    new RTEditor.ButtonFunction("Create theme", () => { RTThemeEditor.inst.RenderThemeEditor(); }),
                                    new RTEditor.ButtonFunction(true),
                                    new RTEditor.ButtonFunction("Paste", RTThemeEditor.inst.PasteTheme));
                            };
                            themes.gameObject.AddComponent<Button>();

                            EditorThemeManager.AddGraphic(themes, ThemeGroup.Background_3);
                            EditorThemeManager.AddGraphic(dialog.Find("themes/viewport").GetComponent<Image>(), ThemeGroup.Null, true);

                            EditorThemeManager.AddScrollbar(themes.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>(), scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

                            var current = dialog.Find("current_title");
                            current.AsRT().sizeDelta = new Vector2(366f, 24f);

                            for (int k = 0; k < current.childCount; k++)
                                EditorThemeManager.AddLightText(current.GetChild(k).GetComponent<Text>());

                            var objectCols = dialog.Find("object_cols/text").GetComponent<Text>();
                            var bgCols = dialog.Find("bg_cols/text").GetComponent<Text>();
                            var playerCols = dialog.Find("player_cols/text").GetComponent<Text>();

                            EditorThemeManager.AddLightText(dialog.Find("object_cols/text").GetComponent<Text>());
                            EditorThemeManager.AddLightText(dialog.Find("bg_cols/text").GetComponent<Text>());
                            EditorThemeManager.AddLightText(dialog.Find("player_cols/text").GetComponent<Text>());

                            dialog.Find("object_cols").AsRT().sizeDelta = new Vector2(366f, 24f);
                            dialog.Find("object_cols").GetComponent<HorizontalLayoutGroup>().spacing = 6f;
                            for (int j = 1; j < dialog.Find("object_cols").childCount; j++)
                            {
                                var child = dialog.Find("object_cols").GetChild(j);
                                child.AsRT().sizeDelta = new Vector2(24f, 24f);

                                EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Null, true);
                            }

                            dialog.Find("bg_cols").AsRT().sizeDelta = new Vector2(366f, 24f);
                            dialog.Find("bg_cols").GetComponent<HorizontalLayoutGroup>().spacing = 6f;
                            for (int j = 1; j < dialog.Find("bg_cols").childCount; j++)
                            {
                                var child = dialog.Find("bg_cols").GetChild(j);
                                child.AsRT().sizeDelta = new Vector2(24f, 24f);

                                EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Null, true);
                            }

                            dialog.Find("player_cols").AsRT().sizeDelta = new Vector2(366f, 24f);
                            dialog.Find("player_cols").GetComponent<HorizontalLayoutGroup>().spacing = 6f;
                            for (int j = 1; j < dialog.Find("player_cols").childCount; j++)
                            {
                                var child = dialog.Find("player_cols").GetChild(j);
                                child.AsRT().sizeDelta = new Vector2(24f, 24f);

                                EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Null, true);
                            }

                            break;
                        }
                    case 7:
                    case 9:
                        {
                            var valueNames = new List<string>
                            {
                                "intensity",
                                "size",
                                "colored",
                            };

                            if (i == 7)
                            {
                                valueNames = new List<string>
                                {
                                    "intensity",
                                    "smoothness",
                                    "roundness",
                                    "position",
                                };
                            }

                            for (int j = 0; j < valueNames.Count; j++)
                            {
                                var positionBase = dialog.Find(valueNames[j]);

                                if (valueNames[j] == "colored" || valueNames[j] == "roundness")
                                {
                                    var toggle = dialog.Find(valueNames[j] == "colored" ? "colored" : "roundness/rounded").GetComponent<Toggle>();
                                    EditorThemeManager.AddToggle(toggle);

                                    if (valueNames[j] == "colored")
                                        continue;
                                }

                                EditorThemeManager.AddInputFields(positionBase.gameObject, true, "Event Editor", false, valueNames[j] == "position");
                            }

                            break;
                        }
                }
            }

            var detector = EditorManager.inst.GetDialog("Event Editor").Dialog.gameObject.AddComponent<Clickable>();
            detector.onEnable = _val =>
            {
                if (!_val)
                    for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
                        EventEditor.inst.dialogRight.GetChild(i).gameObject.SetActive(false);
            };

            SetupCopies();

            for (int i = 0; i < 15; i++)
            {
                var child = EventEditor.inst.EventLabels.transform.GetChild(i);
                EventBins.Add(child.GetComponent<Image>());
                EventLabels.Add(child.GetChild(0).GetComponent<Text>());
            }

            for (int i = 0; i < GameData.DefaultKeyframes.Count; i++)
                copiedKeyframeDatas.Add(null);
        }

        #region Deleting

        public string DeleteKeyframe(int _type, int _event)
        {
            if (_event != 0)
            {
                string result = string.Format("Event [{0}][{1}]", _type, _event);
                GameData.Current.eventObjects.allEvents[_type].RemoveAt(_event);
                CreateEventObjects();
                EventManager.inst.updateEvents();
                SetCurrentEvent(_type, _type - 1);
                return result;
            }
            EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
            return "";
        }

        public IEnumerator DeleteKeyframes()
        {
            var strs = new List<string>();
            var list = SelectedKeyframes;
            var count = list.Count;
            foreach (var timelineObject in list)
            {
                strs.Add(timelineObject.ID);
            }

            var types = SelectedKeyframes.Select(x => x.Type);

            int num = 0;
            foreach (var type in types)
                num += type;

            if (types.Count() > 0)
                num /= types.Count();

            int index = 0;
            if (count == 1)
            {
                index = SelectedKeyframes[0].Index - 1;
                if (index < 0)
                    index = 0;
            }

            SelectedKeyframes.ForEach(x => Destroy(x.GameObject));
            RTEditor.inst.timelineKeyframes.RemoveAll(x => strs.Contains(x.ID));

            var allEvents = GameData.Current.eventObjects.allEvents;
            for (int i = 0; i < allEvents.Count; i++)
            {
                allEvents[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
            }

            UpdateEventOrder();
            EventManager.inst.updateEvents();

            SetCurrentEvent(num, index);

            EditorManager.inst.DisplayNotification($"Deleted Event Keyframes [ {count} ]", 1f, EditorManager.NotificationType.Success);

            yield break;
        }

        public IEnumerator DeleteKeyframes(List<TimelineObject> kfs)
        {
            var strs = new List<string>();
            var list = kfs;
            var count = list.Count;
            foreach (var timelineObject in list)
            {
                strs.Add(timelineObject.ID);
            }

            var types = SelectedKeyframes.Select(x => x.Type);

            int num = 0;
            foreach (var type in types)
                num += type;

            if (types.Count() > 0)
                num /= types.Count();

            int index = 0;
            if (count == 1)
            {
                index = SelectedKeyframes[0].Index - 1;
                if (index < 0)
                    index = 0;
            }

            SelectedKeyframes.ForEach(x => Destroy(x.GameObject));
            RTEditor.inst.timelineKeyframes.RemoveAll(x => strs.Contains(x.ID));

            var allEvents = GameData.Current.eventObjects.allEvents;
            for (int i = 0; i < allEvents.Count; i++)
            {
                allEvents[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
            }

            UpdateEventOrder();
            EventManager.inst.updateEvents();

            SetCurrentEvent(num, index);

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
                if (GameData.Current.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime < num)
                    num = GameData.Current.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime;
            }

            foreach (var keyframeSelection2 in SelectedKeyframes)
            {
                int type = keyframeSelection2.Type;
                int index = keyframeSelection2.Index;
                var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)GameData.Current.eventObjects.allEvents[type][index], false);
                eventKeyframe.eventTime -= num;
                var timelineObject = new TimelineObject(eventKeyframe);
                timelineObject.Type = type;
                timelineObject.Index = index;
                copiedEventKeyframes.Add(timelineObject);
            }

            try
            {
                var jn = JSON.Parse("{}");

                for (int i = 0; i < AllEvents.Count; i++)
                {
                    jn["events"][GameData.EventTypes[i]] = new JSONArray();
                    int add = 0;
                    for (int j = 0; j < AllEvents[i].Count; j++)
                    {
                        if (copiedEventKeyframes.TryFind(x => x.ID == ((EventKeyframe)AllEvents[i][j]).id, out TimelineObject timelineObject))
                        {
                            var eventKeyframe = timelineObject.GetData<EventKeyframe>();
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

        public void PasteEvents(bool setTime = true) => PasteEvents(copiedEventKeyframes, setTime);

        public void PasteEvents(List<TimelineObject> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                CoreHelper.LogError($"No copied event yet!");
                return;
            }

            var selectPasted = EditorConfig.Instance.SelectPasted.Value;
            if (selectPasted)
                RTEditor.inst.timelineKeyframes.ForEach(x => x.Selected = false);

            var time = EditorManager.inst.CurrentAudioPos;
            if (SettingEditor.inst.SnapActive)
                time = RTEditor.SnapToBPM(time);

            foreach (var keyframeSelection in kfs)
            {
                var eventKeyframe = EventKeyframe.DeepCopy(keyframeSelection.GetData<EventKeyframe>());
                if (setTime)
                {
                    eventKeyframe.eventTime = time + eventKeyframe.eventTime;
                }

                var index = AllEvents[keyframeSelection.Type].FindIndex(x => x.eventTime > eventKeyframe.eventTime) - 1;
                if (index < 0)
                    index = AllEvents[keyframeSelection.Type].Count;

                GameData.Current.eventObjects.allEvents[keyframeSelection.Type].Insert(index, eventKeyframe);

                var kf = CreateEventObject(keyframeSelection.Type, index);
                RenderTimelineObject(kf);
                if (selectPasted)
                    kf.Selected = true;
                RTEditor.inst.timelineKeyframes.Add(kf);
            }

            UpdateEventOrder();
            OpenDialog();
            EventManager.inst.updateEvents();
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectKeyframes(bool _add)
        {
            var list = RTEditor.inst.timelineKeyframes;

            if (!_add)
                DeselectAllKeyframes();

            list.Where(x => (x.Type / EVENT_LIMIT) == RTEditor.inst.Layer && RTEditor.inst.layerType == RTEditor.LayerType.Events &&
            RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList()
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

        public void CreateNewEventObject(int type = 0) => CreateNewEventObject(EditorManager.inst.CurrentAudioPos, type);

        public void CreateNewEventObject(float time, int type)
        {
            EventKeyframe eventKeyframe = null;

            if (SettingEditor.inst.SnapActive)
                time = RTEditor.SnapToBPM(time);

            int num = AllEvents[type].FindLastIndex(x => x.eventTime <= time);

            if (num >= 0)
            {
                eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)AllEvents[type][num]);
                eventKeyframe.eventTime = time;
            }
            else
            {
                eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)GameData.DefaultKeyframes[type]);
                eventKeyframe.eventTime = 0f;
            }

            eventKeyframe.locked = false;

            if (type == 2 && EditorConfig.Instance.RotationEventKeyframeResets.Value)
                eventKeyframe.SetEventValues(new float[1]);

            AllEvents[type].Add(eventKeyframe);

            UpdateEventOrder();

            EventManager.inst.updateEvents();

            var kf = CreateEventObject(type, AllEvents[type].IndexOf(eventKeyframe));
            RenderTimelineObject(kf);
            RTEditor.inst.timelineKeyframes.Add(kf);
            SetCurrentEvent(type, kf.Index);
        }

        public void NewKeyframeFromTimeline(int type)
        {
            if (!(AllEvents.Count > type))
            {
                EditorManager.inst.DisplayNotification("Keyframe type doesn't exist!", 4f, EditorManager.NotificationType.Warning);
                return;
            }

            CreateNewEventObject(RTEditor.inst.GetTimelineTime(), type);
        }

        public void AddSelectedEvent(int type, int index)
        {
            int kfIndex = 0;
            if (!RTEditor.inst.timelineKeyframes.TryFindIndex(x => x.Type == type && x.Index == index, out kfIndex))
            {
                CreateEventObjects();
                kfIndex = RTEditor.inst.timelineKeyframes.FindIndex(x => x.Type == type && x.Index == index);
            }

            var kf = RTEditor.inst.timelineKeyframes[kfIndex];

            kf.Selected = SelectedKeyframes.Count <= 1 || !kf.Selected;

            EventEditor.inst.currentEventType = type;
            EventEditor.inst.currentEvent = index;
            RenderEventObjects();
            OpenDialog();
        }

        public void SetCurrentEvent(int type, int index)
        {
            DeselectAllKeyframes();
            AddSelectedEvent(type, index);
        }

        #endregion

        #region Timeline Objects

        public void ClearEventObjects()
        {
            foreach (var kf in RTEditor.inst.timelineKeyframes)
            {
                kf.Selected = false;
                Destroy(kf.GameObject);
            }

            RTEditor.inst.timelineKeyframes.Clear();
        }

        public void CreateEventObjects()
        {
            ClearEventObjects();

            EventEditor.inst.eventDrag = false;

            for (int type = 0; type < AllEvents.Count; type++)
            {
                for (int index = 0; index < AllEvents[type].Count; index++)
                {
                    var kf = CreateEventObject(type, index);

                    RTEditor.inst.timelineKeyframes.Add(kf);
                }
            }

            RenderEventObjects();
        }

        public TimelineObject CreateEventObject(int type, int index)
        {
            var eventKeyframe = AllEvents[type][index] as EventKeyframe;

            var kf = new TimelineObject(eventKeyframe);
            eventKeyframe.timelineObject = kf;
            kf.Type = type;
            kf.Index = index;
            kf.GameObject = EventGameObject(kf);
            kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
            kf.RenderVisibleState();

            TriggerHelper.AddEventTriggers(kf.GameObject,
                TriggerHelper.CreateEventObjectTrigger(kf),
                TriggerHelper.CreateEventStartDragTrigger(kf),
                TriggerHelper.CreateEventEndDragTrigger(),
                TriggerHelper.CreateEventSelectTrigger(kf));

            return kf;
        }

        public GameObject EventGameObject(TimelineObject kf) => EventEditor.inst.TimelinePrefab.Duplicate(EventEditor.inst.EventHolders.transform.GetChild(kf.Type % EVENT_LIMIT), $"keyframe - {kf.Type}");

        public void RenderEventObjects()
        {
            for (int type = 0; type < AllEvents.Count; type++)
            {
                for (int index = 0; index < AllEvents[type].Count; index++)
                {
                    var kf = RTEditor.inst.timelineKeyframes.Find(x => x.Type == type && x.Index == index);

                    if (!kf)
                        kf = RTEditor.inst.timelineKeyframes.Find(x => x.ID == (AllEvents[type][index] as EventKeyframe).id);

                    if (!kf)
                    {
                        kf = CreateEventObject(type, index);
                        RTEditor.inst.timelineKeyframes.Add(kf);
                    }
                    if (!kf.GameObject)
                        kf.GameObject = EventGameObject(kf);
                    RenderTimelineObject(kf);
                }
            }
        }

        public void RenderTimelineObject(TimelineObject kf)
        {
            var events = AllEvents[kf.Type];
            if (events.TryFindIndex(x => (x as EventKeyframe).id == kf.ID, out int index))
                kf.Index = index;

            if (kf.Type / EVENT_LIMIT == RTEditor.inst.Layer)
            {
                kf.GameObject.transform.AsRT().anchoredPosition = new Vector2(events[kf.Index].eventTime * EditorManager.inst.Zoom - EditorManager.BaseUnit / 2, 0.0f);
                kf.GameObject.transform.AsRT().pivot = new Vector2(0f, 1f); // Fixes the keyframes being off center.

                kf.RenderSprite(events);
                kf.RenderIcons();
            }
        }

        #endregion

        #region Generate UI

        void SetupCopies()
        {
            var gameObject = new GameObject("UI Dictionary");
            eventCopies = gameObject.transform;
            eventCopies.transform.SetParent(transform);

            var uiCopy = Instantiate(EventEditor.inst.dialogRight.Find("grain").gameObject);
            uiCopy.transform.SetParent(eventCopies);

            while (uiCopy.transform.childCount > 8)
                DestroyImmediate(uiCopy.transform.GetChild(uiCopy.transform.childCount - 1).gameObject);

            uiDictionary.Add("UI Copy", uiCopy);

            var move = EventEditor.inst.dialogRight.GetChild(0);

            // Label Parent (includes two labels, can be set to any number using GenerateLabels)
            SetupCopy(move.GetChild(8).gameObject, eventCopies, "Label");
            SetupCopy(move.GetChild(9).gameObject, eventCopies, "Vector2");

            var single = Instantiate(move.GetChild(9).gameObject);

            single.transform.SetParent(eventCopies);
            DestroyImmediate(single.transform.GetChild(1).gameObject);

            uiDictionary.Add("Single", single);

            // Vector3
            {
                var vector3 = Instantiate(move.GetChild(9).gameObject);
                var z = Instantiate(vector3.transform.GetChild(1));
                z.name = "z";
                z.transform.SetParent(vector3.transform);
                z.transform.localScale = Vector3.one;

                vector3.transform.SetParent(eventCopies);
                vector3.transform.localScale = Vector3.one;

                for (int i = 0; i < vector3.transform.childCount; i++)
                {
                    ((RectTransform)vector3.transform.GetChild(i)).sizeDelta = new Vector2(122f, 32f);
                    ((RectTransform)vector3.transform.GetChild(i).GetChild(0)).sizeDelta = new Vector2(60f, 32f);
                }

                uiDictionary.Add("Vector3", vector3);
            }

            // Vector4
            {
                var vector4 = Instantiate(uiDictionary["Vector3"]);
                var w = Instantiate(vector4.transform.GetChild(1));
                w.name = "w";
                w.transform.SetParent(vector4.transform);
                w.transform.localScale = Vector3.one;

                vector4.transform.SetParent(eventCopies);
                vector4.transform.localScale = Vector3.one;

                for (int i = 0; i < vector4.transform.childCount; i++)
                {
                    ((RectTransform)vector4.transform.GetChild(i)).sizeDelta = new Vector2(85f, 32f);
                    ((RectTransform)vector4.transform.GetChild(i).GetChild(0)).sizeDelta = new Vector2(40f, 32f);
                }

                uiDictionary.Add("Vector4", vector4);
            }

            // Color
            {
                //var colorButtons = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");
                var colorButtons = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject;
                var colors = Instantiate(colorButtons);
                colors.transform.SetParent(eventCopies);
                colors.transform.localScale = Vector3.one;
                for (int i = 1; i < colors.transform.childCount; i++)
                {
                    Destroy(colors.transform.GetChild(i).gameObject);
                }

                var colorButton = colors.transform.GetChild(0).gameObject.Duplicate(eventCopies);

                uiDictionary.Add("Colors", colors);
                uiDictionary.Add("Color Button", colorButton);
            }

            // Bool
            {
                var boolean = Instantiate(EventEditor.inst.dialogRight.Find("grain/colored").gameObject);
                boolean.transform.SetParent(eventCopies);
                boolean.transform.localScale = Vector3.one;

                uiDictionary.Add("Bool", boolean);
            }

            GenerateEventDialogs();
        }

        void SetupCopy(GameObject gameObject, Transform parent, string name)
        {
            var copy = Instantiate(gameObject);
            copy.transform.SetParent(parent);
            copy.transform.localScale = Vector3.one;
            uiDictionary.Add(name, copy);
        }

        void GenerateLabels(Transform parent, params string[] labels)
        {
            LSHelpers.DeleteChildren(parent);

            var labelToCopy = uiDictionary["Label"].transform.GetChild(0).gameObject;
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labelToCopy.Duplicate(parent, "label");
                label.transform.localScale = Vector3.one;
                var labelText = label.GetComponent<Text>();
                labelText.text = labels[i];

                EditorThemeManager.AddLightText(labelText);
            }
        }

        Dictionary<string, GameObject> GenerateUIElement(string name, string toCopy, Transform parent, int index, params string[] labels)
        {
            if (!uiDictionary.TryGetValue(toCopy, out GameObject gameObjectToCopy))
                return null;

            var l = uiDictionary["Label"].Duplicate(parent, "label", index);
            l.transform.localScale = Vector3.one;
            GenerateLabels(l.transform, labels);

            var copy = gameObjectToCopy.Duplicate(parent, name, index + 1);
            copy.transform.localScale = Vector3.one;

            return new Dictionary<string, GameObject>()
            {
                { "Label", l },
                { "UI", copy }
            };
        }

        GameObject GenerateEventDialog(string name)
        {
            var dialog = Instantiate(uiDictionary["UI Copy"]);
            dialog.transform.SetParent(EventEditor.inst.dialogRight);
            dialog.transform.localScale = Vector3.one;
            dialog.name = name;

            var easing = dialog.transform.Find("curves").GetComponent<Dropdown>();

            TriggerHelper.AddEventTriggers(easing.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                if (!EditorConfig.Instance.ScrollOnEasing.Value)
                    return;

                var pointerEventData = (PointerEventData)eventData;
                if (pointerEventData.scrollDelta.y > 0f)
                    easing.value = easing.value == 0 ? easing.options.Count - 1 : easing.value - 1;
                if (pointerEventData.scrollDelta.y < 0f)
                    easing.value = easing.value == easing.options.Count - 1 ? 0 : easing.value + 1;
            }));


            var topPanel = dialog.transform.GetChild(0);
            var bg = topPanel.GetChild(0).GetComponent<Image>();
            var title = topPanel.GetChild(1).GetComponent<Text>();
            bg.gameObject.AddComponent<ContrastColors>().Init(title, bg);

            var edit = dialog.transform.Find("edit");
            for (int j = 0; j < edit.childCount; j++)
            {
                var button = edit.GetChild(j);
                var buttonComponent = button.GetComponent<Button>();

                if (!buttonComponent)
                    continue;

                if (button.name == "del")
                {
                    var buttonBG = button.GetChild(0).GetComponent<Image>();

                    EditorThemeManager.AddGraphic(buttonBG, ThemeGroup.Delete_Keyframe_BG);
                    EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Delete_Keyframe_Button, false);

                    continue;
                }

                Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
            }

            // Labels
            for (int j = 0; j < dialog.transform.childCount; j++)
            {
                var label = dialog.transform.GetChild(j);
                if (!(label.name == "label" || label.name == "curves_label"))
                    continue;

                for (int k = 0; k < label.childCount; k++)
                    EditorThemeManager.AddLightText(label.GetChild(k).GetComponent<Text>());
            }

            var timeBase = dialog.transform.Find("time");
            var timeInput = timeBase.Find("time").GetComponent<InputField>();

            EditorThemeManager.AddInputField(timeInput);

            for (int j = 1; j < timeBase.childCount; j++)
            {
                var button = timeBase.GetChild(j);
                var buttonComponent = button.GetComponent<Button>();

                if (!buttonComponent)
                    continue;

                Destroy(button.GetComponent<Animator>());
                buttonComponent.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
            }

            EditorThemeManager.AddDropdown(easing);

            return dialog;
        }

        GameObject SetupColorButtons(string name, string label, Transform parent, int index, List<Toggle> toggles, int colorCount = 19)
        {
            var colors = GenerateUIElement(name, "Colors", parent, index, label);
            var colorsObject = colors["UI"];

            colorsObject.GetComponent<GridLayoutGroup>().spacing = new Vector2(5f, 5f);
            ((RectTransform)colorsObject.transform).sizeDelta = new Vector2(366f, 64f);

            LSHelpers.DeleteChildren(colorsObject.transform);

            for (int i = 0; i < colorCount; i++)
            {
                var toggle = uiDictionary["Color Button"].Duplicate(colorsObject.transform, (i + 1).ToString());

                var t = toggle.GetComponent<Toggle>();
                t.image.enabled = true;
                t.enabled = true;

                toggle.AddComponent<Mask>();

                EditorThemeManager.AddGraphic(t.image, ThemeGroup.Null, true);
                EditorThemeManager.AddGraphic(t.graphic, ThemeGroup.Background_1);

                toggles.Add(t);
            }

            return colorsObject;
        }

        void GenerateEventDialogs()
        {
            var shake = EventEditor.inst.dialogRight.Find("shake");
            {
                var direction = GenerateUIElement("direction", "Vector2", shake, 10, "Direction X", "Direction Y");

                var labelBase = Instantiate(uiDictionary["Label"]);
                labelBase.name = "notice-label";
                labelBase.transform.SetParent(shake);
                labelBase.transform.localScale = Vector3.one;
                labelBase.transform.AsRT().sizeDelta = new Vector2(366f, 42f);

                LSHelpers.DeleteChildren(labelBase.transform);

                var label = Instantiate(uiDictionary["Label"].transform.GetChild(0).gameObject);
                label.name = "label";
                label.transform.SetParent(labelBase.transform);
                label.transform.localScale = Vector3.one;
                label.transform.AsRT().sizeDelta = new Vector2(366f, 42f);
                var labelText = label.GetComponent<Text>();
                labelText.text = "(Requires Catalyst Shake)";

                var interpolation = GenerateUIElement("interpolation", "Single", shake, 13, "Smoothness");
                var speed = GenerateUIElement("speed", "Single", shake, 15, "Speed");

                EditorThemeManager.AddLightText(labelText);
                EditorThemeManager.AddInputFields(direction["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(interpolation["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(speed["UI"], true, "Event Editor");
            }

            var bloom = EventEditor.inst.dialogRight.Find("bloom");
            {
                var diffusion = GenerateUIElement("diffusion", "Single", bloom, 10, "Diffusion");
                var threshold = GenerateUIElement("threshold", "Single", bloom, 12, "Threshold");
                var ratio = GenerateUIElement("anamorphic ratio", "Single", bloom, 14, "Anamorphic Ratio");
                var colors = SetupColorButtons("colors", "Colors", bloom, 16, bloomColorButtons);

                var colorShift = GenerateUIElement("colorshift", "Vector3", bloom.transform, 18, "Hue", "Sat", "Val");

                EditorThemeManager.AddInputFields(diffusion["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(threshold["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(ratio["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(colorShift["UI"], true, "Event Editor");
            }

            var vignette = EventEditor.inst.dialogRight.Find("vignette");
            {
                var colors = SetupColorButtons("colors", "Colors", vignette, 18, vignetteColorButtons);

                var colorShift = GenerateUIElement("colorshift", "Vector3", vignette.transform, 20, "Hue", "Sat", "Val");
                EditorThemeManager.AddInputFields(colorShift["UI"], true, "Event Editor");
            }

            var lens = EventEditor.inst.dialogRight.Find("lens");
            {
                var center = GenerateUIElement("center", "Vector2", lens, 10, "Center X", "Center Y");
                var intensity = GenerateUIElement("intensity", "Vector2", lens, 12, "Intensity X", "Intensity Y");
                var scale = GenerateUIElement("scale", "Single", lens, 14, "Scale");

                EditorThemeManager.AddInputFields(center["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(scale["UI"], true, "Event Editor");
            }

            var colorGrading = GenerateEventDialog("colorgrading");
            {
                var hueShift = GenerateUIElement("hueshift", "Single", colorGrading.transform, 8, "Hueshift");
                var contrast = GenerateUIElement("contrast", "Single", colorGrading.transform, 10, "Contrast");
                var gamma = GenerateUIElement("gamma", "Vector4", colorGrading.transform, 12, "Gamma X", "Gamma Y", "Gamma Z", "Gamma W");
                var saturation = GenerateUIElement("saturation", "Single", colorGrading.transform, 12, "Saturation");
                var temperature = GenerateUIElement("temperature", "Single", colorGrading.transform, 14, "Temperature");
                var tint = GenerateUIElement("tint", "Single", colorGrading.transform, 16, "Tint");

                EditorThemeManager.AddInputFields(hueShift["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(contrast["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(gamma["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(saturation["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(temperature["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(tint["UI"], true, "Event Editor");
            }

            var ripples = GenerateEventDialog("ripples");
            {
                var strength = GenerateUIElement("strength", "Single", ripples.transform, 8, "Strength");
                var speed = GenerateUIElement("speed", "Single", ripples.transform, 10, "Speed");
                var distance = GenerateUIElement("distance", "Single", ripples.transform, 12, "Distance");
                var size = GenerateUIElement("size", "Vector2", ripples.transform, 14, "Height", "Width");

                var modeLabel = strength["Label"].Duplicate(ripples.transform);
                GenerateLabels(modeLabel.transform, "Mode");

                var mode = ripples.transform.Find("curves").gameObject.Duplicate(ripples.transform, "mode");
                var modeDropdown = mode.GetComponent<Dropdown>();
                modeDropdown.options = CoreHelper.StringToOptionData("Radial", "Omni-Directional");

                EditorThemeManager.AddDropdown(modeDropdown);

                EditorThemeManager.AddInputFields(strength["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(speed["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(distance["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(size["UI"], true, "Event Editor");
            }

            var radialBlur = GenerateEventDialog("radialblur");
            {
                var intensity = GenerateUIElement("intensity", "Single", radialBlur.transform, 8, "Intensity");
                var iterations = GenerateUIElement("iterations", "Single", radialBlur.transform, 10, "Iterations");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(iterations["UI"], true, "Event Editor");
            }

            var colorSplit = GenerateEventDialog("colorsplit");
            {
                var offset = GenerateUIElement("offset", "Single", colorSplit.transform, 8, "Offset");

                var modeLabel = offset["Label"].Duplicate(colorSplit.transform);
                GenerateLabels(modeLabel.transform, "Mode");

                var mode = colorSplit.transform.Find("curves").gameObject.Duplicate(colorSplit.transform, "mode");
                var modeDropdown = mode.GetComponent<Dropdown>();
                modeDropdown.options = CoreHelper.StringToOptionData("Single", "Single Box Filtered", "Double", "Double Box Filtered");

                EditorThemeManager.AddInputFields(offset["UI"], true, "Event Editor");
                EditorThemeManager.AddDropdown(modeDropdown);
            }

            var cameraOffset = GenerateEventDialog("camoffset");
            {
                var position = GenerateUIElement("position", "Vector2", cameraOffset.transform, 8, "Offset X", "Offset Y");
                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
            }

            var gradient = GenerateEventDialog("gradient");
            {
                var intensity = GenerateUIElement("introt", "Vector2", gradient.transform, 8, "Intensity", "Rotation");
                var colorsTop = SetupColorButtons("colors1", "Colors Top", gradient.transform, 10, gradientColor1Buttons, 20);
                var colorShiftTop = GenerateUIElement("colorshift1", "Vector4", gradient.transform, 12, "Opacity", "Hue", "Sat", "Val");
                var colorsBottom = SetupColorButtons("colors2", "Colors Bottom", gradient.transform, 14, gradientColor2Buttons, 20);
                var colorShiftBottom = GenerateUIElement("colorshift2", "Vector4", gradient.transform, 16, "Opacity", "Hue", "Sat", "Val");

                var modeLabel = intensity["Label"].Duplicate(gradient.transform);
                GenerateLabels(modeLabel.transform, "Mode");

                var mode = gradient.transform.Find("curves").gameObject.Duplicate(gradient.transform, "mode");
                var modeDropdown = mode.GetComponent<Dropdown>();
                modeDropdown.options = CoreHelper.StringToOptionData("Linear", "Additive", "Multiply", "Screen");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(colorShiftTop["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(colorShiftBottom["UI"], true, "Event Editor");
                EditorThemeManager.AddDropdown(modeDropdown);
            }

            var doubleVision = GenerateEventDialog("doublevision");
            {
                var intensity = GenerateUIElement("intensity", "Single", doubleVision.transform, 8, "Intensity");

                var modeLabel = intensity["Label"].Duplicate(doubleVision.transform);
                GenerateLabels(modeLabel.transform, "Mode");

                var mode = doubleVision.transform.Find("curves").gameObject.Duplicate(doubleVision.transform, "mode");
                var modeDropdown = mode.GetComponent<Dropdown>();
                modeDropdown.options = CoreHelper.StringToOptionData("Split", "Edges");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddDropdown(modeDropdown);

            }

            var scanLines = GenerateEventDialog("scanlines");
            {
                var intensity = GenerateUIElement("intensity", "Single", scanLines.transform, 8, "Intensity");
                var amount = GenerateUIElement("amount", "Single", scanLines.transform, 10, "Amount Horizontal");
                var speed = GenerateUIElement("speed", "Single", scanLines.transform, 12, "Speed");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(amount["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(speed["UI"], true, "Event Editor");
            }

            var blur = GenerateEventDialog("blur");
            {
                var intensity = GenerateUIElement("intensity", "Single", blur.transform, 8, "Intensity");
                var iterations = GenerateUIElement("iterations", "Single", blur.transform, 10, "Iterations");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(iterations["UI"], true, "Event Editor");
            }

            var pixelize = GenerateEventDialog("pixelize");
            {
                var amount = GenerateUIElement("amount", "Single", pixelize.transform, 8, "Amount");

                EditorThemeManager.AddInputFields(amount["UI"], true, "Event Editor");
            }

            var bg = GenerateEventDialog("bg");
            {
                var colors = SetupColorButtons("colors", "Colors", bg.transform, 8, bgColorButtons);
                var colorShift = GenerateUIElement("colorshift", "Vector3", bg.transform, 10, "Hue", "Sat", "Val");

                var active = GenerateUIElement("active", "Bool", bg.transform, 12, "Background Objects Active");
                var activeText = active["UI"].transform.Find("Text").GetComponent<Text>();
                activeText.text = "Active";

                EditorThemeManager.AddInputFields(colorShift["UI"], true, "Event Editor");
                EditorThemeManager.AddToggle(active["UI"].GetComponent<Toggle>(), graphic: activeText);
            }

            var invert = GenerateEventDialog("invert");
            {
                var intensity = GenerateUIElement("amount", "Single", invert.transform, 8, "Invert Amount");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
            }

            var timeline = GenerateEventDialog("timeline");
            {
                var active = GenerateUIElement("active", "Bool", timeline.transform, 8, "Active");
                var activeText = active["UI"].transform.Find("Text").GetComponent<Text>();
                activeText.text = "Active";

                var position = GenerateUIElement("position", "Vector2", timeline.transform, 10, "Position X", "Position Y");
                var scale = GenerateUIElement("scale", "Vector2", timeline.transform, 12, "Scale X", "Scale Y");
                var rotation = GenerateUIElement("rotation", "Single", timeline.transform, 14, "Rotation");
                var colors = SetupColorButtons("colors", "Colors", timeline.transform, 16, timelineColorButtons);
                var colorShift = GenerateUIElement("colorshift", "Vector4", timeline.transform, 18, "Opacity", "Hue", "Sat", "Val");

                EditorThemeManager.AddToggle(active["UI"].GetComponent<Toggle>(), graphic: activeText);
                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(scale["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(rotation["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(colorShift["UI"], true, "Event Editor");
            }

            var player = GenerateEventDialog("player");
            {
                var active = GenerateUIElement("active", "Bool", player.transform, 8, "Active");
                var activeText = active["UI"].transform.Find("Text").GetComponent<Text>();
                activeText.text = "Active";

                var moveable = GenerateUIElement("move", "Bool", player.transform, 10, "Can Move");
                var moveableText = moveable["UI"].transform.Find("Text").GetComponent<Text>();
                moveableText.text = "Moveable";

                var position = GenerateUIElement("position", "Vector2", player.transform, 12, "Position X", "Position Y");

                var rotation = GenerateUIElement("rotation", "Single", player.transform, 14, "Rotation");

                var outOfBounds = GenerateUIElement("oob", "Bool", player.transform, 16, "Can Exit Bounds");
                var outOfBoundsText = outOfBounds["UI"].transform.Find("Text").GetComponent<Text>();
                outOfBoundsText.text = "Out of Bounds";

                EditorThemeManager.AddToggle(active["UI"].GetComponent<Toggle>(), graphic: activeText);
                EditorThemeManager.AddToggle(moveable["UI"].GetComponent<Toggle>(), graphic: moveableText);
                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(rotation["UI"], true, "Event Editor");
                EditorThemeManager.AddToggle(outOfBounds["UI"].GetComponent<Toggle>(), graphic: outOfBoundsText);

            }

            var follow = GenerateEventDialog("follow");
            {
                var active = GenerateUIElement("active", "Bool", follow.transform, 8, "Active");
                var activeText = active["UI"].transform.Find("Text").GetComponent<Text>();
                activeText.text = "Active";

                var moveable = GenerateUIElement("move", "Bool", follow.transform, 10, "Move Enabled");
                var moveableText = moveable["UI"].transform.Find("Text").GetComponent<Text>();
                moveableText.text = "Move";

                var rotateable = GenerateUIElement("rotate", "Bool", follow.transform, 12, "Rotate Enabled");
                var rotateableText = rotateable["UI"].transform.Find("Text").GetComponent<Text>();
                rotateableText.text = "Rotate";

                var position = GenerateUIElement("position", "Vector2", follow.transform, 14, "Sharpness", "Offset");
                var limitHorizontal = GenerateUIElement("limit horizontal", "Vector2", follow.transform, 16, "Limit Left", "Limit Right");
                var limitVertical = GenerateUIElement("limit vertical", "Vector2", follow.transform, 18, "Limit Up", "Limit Down");
                var anchor = GenerateUIElement("anchor", "Single", follow.transform, 20, "Anchor");

                EditorThemeManager.AddToggle(active["UI"].GetComponent<Toggle>(), graphic: activeText);
                EditorThemeManager.AddToggle(moveable["UI"].GetComponent<Toggle>(), graphic: moveableText);
                EditorThemeManager.AddToggle(rotateable["UI"].GetComponent<Toggle>(), graphic: rotateableText);
                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(limitHorizontal["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(limitVertical["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(anchor["UI"], true, "Event Editor");
            }

            var audio = GenerateEventDialog("audio");
            {
                var pitchVol = GenerateUIElement("music", "Vector2", audio.transform, 8, "Pitch", "Volume");

                EditorThemeManager.AddInputFields(pitchVol["UI"], true, "Event Editor");
            }

            var videoBGParent = GenerateEventDialog("videobgparent");
            {
                var position = GenerateUIElement("position", "Vector3", videoBGParent.transform, 8, "Position X", "Position Y", "Position Z");
                var scale = GenerateUIElement("scale", "Vector3", videoBGParent.transform, 10, "Scale X", "Scale Y", "Scale Z");
                var rotation = GenerateUIElement("rotation", "Vector3", videoBGParent.transform, 12, "Rotation X", "Rotation Y", "Rotation Z");

                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(scale["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(rotation["UI"], true, "Event Editor");
            }

            var videoBG = GenerateEventDialog("videobg");
            {
                var position = GenerateUIElement("position", "Vector3", videoBG.transform, 8, "Position X", "Position Y", "Position Z");
                var scale = GenerateUIElement("scale", "Vector3", videoBG.transform, 10, "Scale X", "Scale Y", "Scale Z");
                var rotation = GenerateUIElement("rotation", "Vector3", videoBG.transform, 12, "Rotation X", "Rotation Y", "Rotation Z");

                var modeLabel = position["Label"].Duplicate(videoBG.transform);
                GenerateLabels(modeLabel.transform, "Render Type");

                var renderTypeD = gradient.transform.Find("curves").gameObject.Duplicate(videoBG.transform, "rendertype");
                var renderTypeDropdown = renderTypeD.GetComponent<Dropdown>();
                renderTypeDropdown.options = CoreHelper.StringToOptionData("Background", "Foreground");

                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(scale["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(rotation["UI"], true, "Event Editor");
                EditorThemeManager.AddDropdown(renderTypeDropdown);
            }

            var sharpen = GenerateEventDialog("sharpen");
            {
                var intensity = GenerateUIElement("intensity", "Single", sharpen.transform, 8, "Intensity");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
            }

            var bars = GenerateEventDialog("bars");
            {
                var intensity = GenerateUIElement("intensity", "Single", bars.transform, 8, "Intensity");

                var modeLabel = intensity["Label"].Duplicate(bars.transform);
                GenerateLabels(modeLabel.transform, "Direction");

                var direction = gradient.transform.Find("curves").gameObject.Duplicate(bars.transform, "direction");
                var directionDropdown = direction.GetComponent<Dropdown>();
                directionDropdown.options = CoreHelper.StringToOptionData("Horizontal", "Vertical");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddDropdown(directionDropdown);
            }

            var danger = GenerateEventDialog("danger");
            {
                var intensity = GenerateUIElement("intensity", "Single", danger.transform, 8, "Intensity");
                var size = GenerateUIElement("size", "Single", danger.transform, 10, "Size");
                var colors = SetupColorButtons("colors", "Colors", danger.transform, 12, dangerColorButtons);
                var colorShift = GenerateUIElement("colorshift", "Vector4", danger.transform, 18, "Opacity", "Hue", "Sat", "Val");

                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(size["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(colorShift["UI"], true, "Event Editor");
            }

            var rotxy = GenerateEventDialog("3d rotation");
            {
                var rotation = GenerateUIElement("rotation", "Vector2", rotxy.transform, 8, "Rotation X", "Rotation Y");

                EditorThemeManager.AddInputFields(rotation["UI"], true, "Event Editor");
            }

            var cameraDepth = GenerateEventDialog("cameradepth");
            {
                var depth = GenerateUIElement("depth", "Single", cameraDepth.transform, 8, "Depth");
                var perspectiveZoom = GenerateUIElement("zoom", "Single", cameraDepth.transform, 10, "Zoom");

                var global = GenerateUIElement("global", "Bool", cameraDepth.transform, 12, "Set Global Position");
                var globalText = global["UI"].transform.Find("Text").GetComponent<Text>();
                globalText.text = "Global";

                EditorThemeManager.AddInputFields(depth["UI"], true, "Event Editor");
                EditorThemeManager.AddInputFields(perspectiveZoom["UI"], true, "Event Editor");
                EditorThemeManager.AddToggle(global["UI"].GetComponent<Toggle>(), graphic: globalText);
            }

            var windowBase = GenerateEventDialog("windowbase");
            {
                var force = GenerateUIElement("force", "Bool", windowBase.transform, 8, "Force Resolution");
                var forceText = force["UI"].transform.Find("Text").GetComponent<Text>();
                forceText.text = "Force";

                var resolution = GenerateUIElement("resolution", "Vector2", windowBase.transform, 10, "Width", "Height");

                var allow = GenerateUIElement("allow", "Bool", windowBase.transform, 12, "Allow Position Events");
                allow["UI"].transform.Find("Text").GetComponent<Text>().text = "Allow";
                var allowText = allow["UI"].transform.Find("Text").GetComponent<Text>();
                allowText.text = "Force";

                EditorThemeManager.AddToggle(force["UI"].GetComponent<Toggle>(), graphic: forceText);
                EditorThemeManager.AddInputFields(resolution["UI"], true, "Event Editor");
                EditorThemeManager.AddToggle(allow["UI"].GetComponent<Toggle>(), graphic: allowText);
            }

            var windowPositionX = GenerateEventDialog("windowpositionx");
            {
                var x = GenerateUIElement("x", "Single", windowPositionX.transform, 8, "Position X (Requires Force Resolution)");
                EditorThemeManager.AddInputFields(x["UI"], true, "Event Editor");
            }

            var windowPositionY = GenerateEventDialog("windowpositiony");
            {
                var y = GenerateUIElement("y", "Single", windowPositionY.transform, 8, "Position Y (Requires Force Resolution)");
                EditorThemeManager.AddInputFields(y["UI"], true, "Event Editor");
            }

            var playerForce = GenerateEventDialog("playerforce");
            {
                var position = GenerateUIElement("position", "Vector2", playerForce.transform, 8, "Force X", "Force Y");
                EditorThemeManager.AddInputFields(position["UI"], true, "Event Editor");
            }

            var mosaic = GenerateEventDialog("mosaic");
            {
                var amount = GenerateUIElement("amount", "Single", mosaic.transform, 8, "Amount");
                EditorThemeManager.AddInputFields(amount["UI"], true, "Event Editor");
            }

            var analogGlitch = GenerateEventDialog("analogglitch");
            {
                var enabled = GenerateUIElement("enabled", "Bool", analogGlitch.transform, 8, "Effect Enabled");
                var enabledText = enabled["UI"].transform.Find("Text").GetComponent<Text>();
                enabledText.text = "Enabled";

                var colorDrift = GenerateUIElement("colordrift", "Single", analogGlitch.transform, 10, "Color Drift");
                EditorThemeManager.AddInputFields(colorDrift["UI"], true, "Event Editor");

                var horizontalShake = GenerateUIElement("horizontalshake", "Single", analogGlitch.transform, 12, "Horizontal Shake");
                EditorThemeManager.AddInputFields(horizontalShake["UI"], true, "Event Editor");

                var scanLineJitter = GenerateUIElement("scanlinejitter", "Single", analogGlitch.transform, 14, "Scan Line Jitter");
                EditorThemeManager.AddInputFields(scanLineJitter["UI"], true, "Event Editor");

                var verticalJump = GenerateUIElement("verticaljump", "Single", analogGlitch.transform, 16, "Vertical Jump");
                EditorThemeManager.AddInputFields(verticalJump["UI"], true, "Event Editor");

                EditorThemeManager.AddToggle(enabled["UI"].GetComponent<Toggle>(), graphic: enabledText);
            }

            var digitalGlitch = GenerateEventDialog("digitalglitch");
            {
                var intensity = GenerateUIElement("intensity", "Single", digitalGlitch.transform, 8, "Intensity");
                EditorThemeManager.AddInputFields(intensity["UI"], true, "Event Editor");
            }

            #region Multi Event Keyframe Editor

            var move = EventEditor.inst.dialogRight.Find("move");
            var multiKeyframeEditor = EditorManager.inst.GetDialog("Multi Keyframe Editor").Dialog;

            multiKeyframeEditor.Find("Text").gameObject.SetActive(false);

            EditorThemeManager.AddGraphic(multiKeyframeEditor.GetComponent<Image>(), ThemeGroup.Background_1);

            var multiKeyframeEditorVLG = multiKeyframeEditor.GetComponent<VerticalLayoutGroup>();
            multiKeyframeEditorVLG.childControlWidth = false;
            multiKeyframeEditorVLG.childForceExpandWidth = false;

            var data = new GameObject("data");
            data.transform.SetParent(multiKeyframeEditor);
            data.transform.localScale = Vector3.one;
            var dataRT = data.AddComponent<RectTransform>();
            dataRT.sizeDelta = new Vector2(740f, 100f);

            var dataVLG = data.AddComponent<VerticalLayoutGroup>();
            dataVLG.childControlHeight = false;
            dataVLG.childControlWidth = true;
            dataVLG.childForceExpandHeight = false;
            dataVLG.childForceExpandWidth = true;
            dataVLG.spacing = 4f;

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Time");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var timeBase = new GameObject("time");
            timeBase.transform.SetParent(dataRT);
            timeBase.transform.localScale = Vector3.one;
            var timeBaseRT = timeBase.AddComponent<RectTransform>();
            timeBaseRT.sizeDelta = new Vector2(765f, 38f);

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(timeBaseRT, "time");
            time.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var timeStorage = time.GetComponent<InputFieldStorage>();
            time.transform.GetChild(0).name = "time";

            EditorThemeManager.AddInputField(timeStorage.inputField);

            EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Ease / Animation Type");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var curveBase = new GameObject("curves");
            curveBase.transform.SetParent(dataRT);
            curveBase.transform.localScale = Vector3.one;
            var curveBaseRT = curveBase.AddComponent<RectTransform>();
            curveBaseRT.sizeDelta = new Vector2(765f, 38f);

            var curves = move.Find("curves").gameObject.Duplicate(curveBaseRT, "curves");
            curves.transform.AsRT().anchoredPosition = new Vector2(191f, 0f);

            EditorThemeManager.AddDropdown(curves.GetComponent<Dropdown>());

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Value Index");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var valueIndexBase = new GameObject("value index");
            valueIndexBase.transform.SetParent(dataRT);
            valueIndexBase.transform.localScale = Vector3.one;
            var valueIndexBaseRT = valueIndexBase.AddComponent<RectTransform>();
            valueIndexBaseRT.sizeDelta = new Vector2(765f, 38f);

            var valueIndex = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueIndexBaseRT, "value index");
            valueIndex.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var valueIndexStorage = valueIndex.GetComponent<InputFieldStorage>();
            valueIndex.transform.GetChild(0).name = "input";

            EditorThemeManager.AddInputField(valueIndexStorage.inputField);

            EditorThemeManager.AddSelectable(valueIndexStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueIndexStorage.rightButton, ThemeGroup.Function_2, false);

            Destroy(valueIndexStorage.leftGreaterButton.gameObject);
            Destroy(valueIndexStorage.middleButton.gameObject);
            Destroy(valueIndexStorage.rightGreaterButton.gameObject);

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Value");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var valueBase = new GameObject("value");
            valueBase.transform.SetParent(dataRT);
            valueBase.transform.localScale = Vector3.one;
            var valueBaseRT = valueBase.AddComponent<RectTransform>();
            valueBaseRT.sizeDelta = new Vector2(765f, 38f);

            var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBaseRT, "value");
            value.transform.AsRT().anchoredPosition = new Vector2(8f, 32f);
            var valueStorage = value.GetComponent<InputFieldStorage>();
            value.transform.GetChild(0).name = "input";

            EditorThemeManager.AddInputField(valueStorage.inputField);

            EditorThemeManager.AddSelectable(valueStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Force Snap Time to BPM");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var snapBase = new GameObject("snap bpm");
            snapBase.transform.SetParent(dataRT);
            snapBase.transform.localScale = Vector3.one;
            var snapBaseRT = snapBase.AddComponent<RectTransform>();
            snapBaseRT.sizeDelta = new Vector2(765f, 38f);

            var snap = EditorPrefabHolder.Instance.Function1Button.Duplicate(snapBaseRT, "snap bpm");
            snap.transform.localScale = Vector3.one;
            var snapStorage = snap.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(snap.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            snapStorage.text.text = "Snap";

            var button = snapStorage.button;
            button.onClick.ClearAll();
            button.onClick.AddListener(() =>
            {
                foreach (var timelineObject in SelectedKeyframes)
                {
                    if (timelineObject.Index != 0)
                        timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);
                    RenderTimelineObject(timelineObject);
                }

                RenderEventsDialog();
                EventManager.inst.updateEvents();
                EditorManager.inst.DisplayNotification($"Snapped all keyframes time!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(snapStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(snapStorage.text, ThemeGroup.Function_1_Text);

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Align to First Selected");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var alignToFirstBase = new GameObject("align");
            alignToFirstBase.transform.SetParent(dataRT);
            alignToFirstBase.transform.localScale = Vector3.one;
            var alignToFirstBaseRT = alignToFirstBase.AddComponent<RectTransform>();
            alignToFirstBaseRT.sizeDelta = new Vector2(765f, 38f);

            var alignToFirstObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(alignToFirstBaseRT, "align");
            alignToFirstObject.transform.localScale = Vector3.one;
            var alignToFirstStorage = alignToFirstObject.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(alignToFirstObject.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            alignToFirstStorage.text.text = "Align";

            var alignToFirst = alignToFirstStorage.button;
            alignToFirst.onClick.ClearAll();
            alignToFirst.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = SelectedKeyframes.OrderBy(x => x.Time);
                var first = list.ElementAt(0);

                foreach (var timelineObject in list)
                {
                    if (timelineObject.Index != 0)
                        timelineObject.Time = first.Time;
                    RenderTimelineObject(timelineObject);
                }

                RenderEventsDialog();
                EventManager.inst.updateEvents();
                EditorManager.inst.DisplayNotification($"Aligned all keyframes to the first keyframe!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(alignToFirstStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(alignToFirstStorage.text, ThemeGroup.Function_1_Text);

            // Label
            {
                var labelBase1 = new GameObject("label base");
                labelBase1.transform.SetParent(dataRT);
                labelBase1.transform.localScale = Vector3.one;
                var labelBase1RT = labelBase1.AddComponent<RectTransform>();
                labelBase1RT.sizeDelta = new Vector2(765f, 38f);

                var l = Instantiate(uiDictionary["Label"]);
                l.name = "label";
                l.transform.SetParent(labelBase1RT);
                l.transform.localScale = Vector3.one;
                GenerateLabels(l.transform, "Paste All Keyframe Data");
                l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
            }

            var pasteAllBase = new GameObject("paste");
            pasteAllBase.transform.SetParent(dataRT);
            pasteAllBase.transform.localScale = Vector3.one;
            var pasteAllBaseRT = pasteAllBase.AddComponent<RectTransform>();
            pasteAllBaseRT.sizeDelta = new Vector2(765f, 38f);

            var pasteAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllBaseRT, "paste");
            pasteAllObject.transform.localScale = Vector3.one;
            var pasteAllStorage = pasteAllObject.GetComponent<FunctionButtonStorage>();

            UIManager.SetRectTransform(pasteAllObject.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, 32f));

            pasteAllStorage.text.text = "Paste";

            var pasteAll = pasteAllStorage.button;
            pasteAll.onClick.ClearAll();
            pasteAll.onClick.AddListener(() =>
            {
                foreach (var keyframe in SelectedKeyframes)
                {
                    if (!(copiedKeyframeDatas.Count > keyframe.Type) || copiedKeyframeDatas[keyframe.Type] == null)
                        continue;

                    var kf = keyframe.GetData<EventKeyframe>();
                    kf.curveType = copiedKeyframeDatas[keyframe.Type].curveType;
                    kf.eventValues = copiedKeyframeDatas[keyframe.Type].eventValues.Copy();
                    kf.eventRandomValues = copiedKeyframeDatas[keyframe.Type].eventRandomValues.Copy();
                    kf.random = copiedKeyframeDatas[keyframe.Type].random;
                    kf.relative = copiedKeyframeDatas[keyframe.Type].relative;
                    RenderTimelineObject(keyframe);
                }

                RenderEventsDialog();
                EventManager.inst.updateEvents();
                EditorManager.inst.DisplayNotification($"Pasted all keyframe data to current selected keyframes!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pasteAllStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(pasteAllStorage.text, ThemeGroup.Function_1_Text);

            #endregion

            // Copy / Paste
            for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
            {
                var dialog = EventEditor.inst.dialogRight.GetChild(i);

                var edit = dialog.Find("edit");
                EditorHelper.SetComplexity(edit.Find("spacer").gameObject, Complexity.Simple);

                var copy = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "copy", 5);
                var copyStorage = copy.GetComponent<FunctionButtonStorage>();
                var copyText = copyStorage.text;
                copyText.text = "Copy";
                copy.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                var paste = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "paste", 6);
                var pasteStorage = paste.GetComponent<FunctionButtonStorage>();
                var pasteText = pasteStorage.text;
                pasteText.text = "Paste";
                paste.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                EditorThemeManager.AddGraphic(copyStorage.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.AddGraphic(copyStorage.text, ThemeGroup.Copy_Text);

                EditorThemeManager.AddGraphic(pasteStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pasteStorage.text, ThemeGroup.Paste_Text);

                EditorHelper.SetComplexity(copy, Complexity.Normal);
                EditorHelper.SetComplexity(paste, Complexity.Normal);
            }
        }

        #endregion

        #region Dialogs

        public static void LogIncorrectFormat(string str) => Debug.LogError($"{EventEditor.inst.className}Event Value was not in correct format! String: {str}");

        public void OpenDialog()
        {
            if (SelectedKeyframes.Count > 1 && !SelectedKeyframes.All(x => x.Type == SelectedKeyframes.Min(y => y.Type)))
            {
                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Keyframe Editor", false);
                RenderMultiEventsDialog();
            }
            else if (SelectedKeyframes.Count > 0)
            {
                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Event Editor");

                EventEditor.inst.currentEventType = SelectedKeyframes[0].Type;
                EventEditor.inst.currentEvent = SelectedKeyframes[0].Index;

                if (EventEditor.inst.dialogRight.childCount > EventEditor.inst.currentEventType)
                {
                    Debug.Log($"{EventEditor.inst.className}Dialog: {EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType).name}");
                    for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
                        EventEditor.inst.dialogRight.GetChild(i).gameObject.SetActive(EventEditor.inst.currentEventType == i);

                    RenderEventsDialog();
                    RenderEventObjects();
                }
                else
                    Debug.LogError($"{EventEditor.inst.className}Keyframe Type {EventEditor.inst.currentEventType} does not currently exist.");
            }
            else
            {
                CheckpointEditor.inst.SetCurrentCheckpoint(0);
            }
        }

        public void RenderMultiEventsDialog()
        {
            var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor").Dialog.Find("data");
            var timeStorage = dialog.Find("time/time").GetComponent<InputFieldStorage>();
            var time = timeStorage.inputField;
            time.onValueChanged.ClearAll();
            if (time.text == "100.000")
                time.text = "10";

            timeStorage.leftGreaterButton.onClick.ClearAll();
            timeStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(time.text, out float num))
                {
                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        var eventKeyframe = kf.GetData<EventKeyframe>();
                        eventKeyframe.eventTime = Mathf.Clamp(eventKeyframe.eventTime - (num * 10f), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    }

                    UpdateEventOrder();
                    RenderEventObjects();
                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(time.text);
            });

            timeStorage.leftButton.onClick.ClearAll();
            timeStorage.leftButton.onClick.AddListener(() =>
            {
                if (float.TryParse(time.text, out float num))
                {
                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        var eventKeyframe = kf.GetData<EventKeyframe>();
                        eventKeyframe.eventTime = Mathf.Clamp(eventKeyframe.eventTime + num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    }

                    UpdateEventOrder();
                    RenderEventObjects();
                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(time.text);
            });

            timeStorage.middleButton.onClick.ClearAll();
            timeStorage.middleButton.onClick.AddListener(() =>
            {
                if (float.TryParse(time.text, out float num))
                {
                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        kf.GetData<EventKeyframe>().eventTime = num;
                    }

                    UpdateEventOrder();
                    RenderEventObjects();
                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(time.text);
            });

            timeStorage.rightButton.onClick.ClearAll();
            timeStorage.rightButton.onClick.AddListener(() =>
            {
                if (float.TryParse(time.text, out float num))
                {
                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        var eventKeyframe = kf.GetData<EventKeyframe>();
                        eventKeyframe.eventTime = Mathf.Clamp(eventKeyframe.eventTime - num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    }

                    UpdateEventOrder();
                    RenderEventObjects();
                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(time.text);
            });

            timeStorage.rightGreaterButton.onClick.ClearAll();
            timeStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(time.text, out float num))
                {
                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        var eventKeyframe = kf.GetData<EventKeyframe>();
                        eventKeyframe.eventTime = Mathf.Clamp(eventKeyframe.eventTime + (num * 10f), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    }

                    UpdateEventOrder();
                    RenderEventObjects();
                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(time.text);
            });

            TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

            var curves = dialog.Find("curves/curves").GetComponent<Dropdown>();
            curves.onValueChanged.ClearAll();
            curves.onValueChanged.AddListener(_val =>
            {
                if (!DataManager.inst.AnimationListDictionary.TryGetValue(_val, out DataManager.LSAnimation anim))
                    return;

                foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    kf.GetData<EventKeyframe>().curveType = anim;

                EventManager.inst.updateEvents();
            });

            var valueIndexStorage = dialog.Find("value index/value index").GetComponent<InputFieldStorage>();
            valueIndexStorage.inputField.onValueChanged.ClearAll();
            if (valueIndexStorage.inputField.text == "100.000")
                valueIndexStorage.inputField.text = "0";
            valueIndexStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (!int.TryParse(_val, out int n))
                    valueIndexStorage.inputField.text = "0";
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(valueIndexStorage.inputField, t: valueIndexStorage.transform);
            TriggerHelper.AddEventTriggers(valueIndexStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(valueIndexStorage.inputField));

            var valueStorage = dialog.Find("value/value").GetComponent<InputFieldStorage>();
            valueStorage.inputField.onValueChanged.ClearAll();
            if (valueStorage.inputField.text == "100.000")
                valueStorage.inputField.text = "1.0";

            valueStorage.leftGreaterButton.onClick.ClearAll();
            valueStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(valueStorage.inputField.text, out float num))
                {
                    foreach (var kf in SelectedKeyframes)
                    {
                        var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                        index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
                        kf.GetData<EventKeyframe>().eventValues[index] -= num * 10f;
                    }
                }
                else
                    LogIncorrectFormat(valueStorage.inputField.text);
            });

            valueStorage.leftButton.onClick.ClearAll();
            valueStorage.leftButton.onClick.AddListener(() =>
            {
                if (float.TryParse(valueStorage.inputField.text, out float num))
                {
                    foreach (var kf in SelectedKeyframes)
                    {
                        var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                        index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
                        kf.GetData<EventKeyframe>().eventValues[index] -= num;
                    }
                }
                else
                    LogIncorrectFormat(valueStorage.inputField.text);
            });

            valueStorage.middleButton.onClick.ClearAll();
            valueStorage.middleButton.onClick.AddListener(() =>
            {
                if (float.TryParse(valueStorage.inputField.text, out float num))
                {
                    foreach (var kf in SelectedKeyframes)
                    {
                        var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                        index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
                        kf.GetData<EventKeyframe>().eventValues[index] = num;
                    }
                }
                else
                    LogIncorrectFormat(valueStorage.inputField.text);
            });

            valueStorage.rightButton.onClick.ClearAll();
            valueStorage.rightButton.onClick.AddListener(() =>
            {
                if (float.TryParse(valueStorage.inputField.text, out float num))
                {
                    foreach (var kf in SelectedKeyframes)
                    {
                        var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                        index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
                        kf.GetData<EventKeyframe>().eventValues[index] += num;
                    }
                }
                else
                    LogIncorrectFormat(valueStorage.inputField.text);
            });

            valueStorage.rightGreaterButton.onClick.ClearAll();
            valueStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(valueStorage.inputField.text, out float num))
                {
                    foreach (var kf in SelectedKeyframes)
                    {
                        var index = Parser.TryParse(valueIndexStorage.inputField.text, 0);

                        index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
                        kf.GetData<EventKeyframe>().eventValues[index] += num * 10f;
                    }
                }
                else
                    LogIncorrectFormat(valueStorage.inputField.text);
            });

            TriggerHelper.AddEventTriggers(valueStorage.inputField.gameObject, TriggerHelper.ScrollDelta(valueStorage.inputField));
        }

        public void RenderEventsDialog()
        {
            var __instance = EventEditor.inst;
            var eventManager = EventManager.inst;
            var dialogTmp = __instance.dialogRight.GetChild(__instance.currentEventType);
            __instance.dialogLeft.Find("theme").gameObject.SetActive(false);
            var time = dialogTmp.Find("time");
            var timeTime = dialogTmp.Find("time/time").GetComponent<InputField>();

            var currentKeyframe = GameData.Current.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent] as EventKeyframe;

            timeTime.onValueChanged.ClearAll();
            timeTime.text = currentKeyframe.eventTime.ToString("f3");

            bool isNotFirst = __instance.currentEvent != 0;
            timeTime.interactable = isNotFirst;

            TriggerHelper.SetInteractable(isNotFirst,
                timeTime,
                dialogTmp.Find("time/<<").GetComponent<Button>(),
                dialogTmp.Find("time/<").GetComponent<Button>(),
                dialogTmp.Find("time/>").GetComponent<Button>(),
                dialogTmp.Find("time/>>").GetComponent<Button>());

            if (isNotFirst)
            {
                timeTime.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                        foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0 && x.Type == __instance.currentEventType))
                        {
                            kf.GetData<EventKeyframe>().eventTime = num;
                            RenderTimelineObject(kf);
                        }

                        UpdateEventOrder();
                        EventManager.inst.updateEvents();
                    }
                    else
                        LogIncorrectFormat(_val);
                });

                TriggerHelper.IncreaseDecreaseButtons(timeTime, 0.1f, 10f, t: time);
                TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(timeTime, min: 0.001f, max: AudioManager.inst.CurrentAudioSource.clip.length));
            }

            switch (__instance.currentEventType)
            {
                case 0: // Move
                    {
                        SetVector2InputField(dialogTmp, "position", 0, 1);
                        break;
                    }
                case 1: // Zoom
                    {
                        SetFloatInputField(dialogTmp, "zoom/x", 0, min: -9999f, max: 9999f);
                        break;
                    }
                case 2: // Rotate
                    {
                        SetFloatInputField(dialogTmp, "rotation/x", 0, 15f, 3f);
                        break;
                    }
                case 3: // Shake
                    {
                        // Shake Intensity
                        SetFloatInputField(dialogTmp, "shake/x", 0, min: 0f, max: 10f, allowNegative: false);

                        // Shake Intensity X / Y

                        RTEditor.SetActive(dialogTmp.Find("direction").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("notice-label").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("interpolation").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("speed").gameObject, RTEditor.ShowModdedUI);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        SetVector2InputField(dialogTmp, "direction", 1, 2, -10f, 10f);
                        SetFloatInputField(dialogTmp, "interpolation/x", 3, max: 999f, allowNegative: false);
                        SetFloatInputField(dialogTmp, "speed/x", 4, min: 0.001f, max: 9999f, allowNegative: false);
                        break;
                    }
                case 4: // Theme
                    {
                        var theme = dialogTmp.Find("theme-search").GetComponent<InputField>();

                        theme.onValueChanged.ClearAll();
                        theme.onValueChanged.AddListener(_val => { RTThemeEditor.inst.RenderThemeContent(dialogTmp, _val); });
                        RTThemeEditor.inst.RenderThemeContent(dialogTmp, theme.text);
                        __instance.RenderThemePreview(dialogTmp);
                        break;
                    }
                case 5: // Chromatic
                    {
                        SetFloatInputField(dialogTmp, "chroma/x", 0, min: 0f, max: float.PositiveInfinity, allowNegative: false);

                        break;
                    }
                case 6: // Bloom
                    {
                        //Bloom Intensity
                        SetFloatInputField(dialogTmp, "bloom/x", 0, max: 1280f, allowNegative: false);

                        RTEditor.SetActive(dialogTmp.Find("diffusion").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("threshold").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("anamorphic ratio").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("colors").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("colorshift").gameObject, RTEditor.ShowModdedUI);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        // Bloom Diffusion
                        SetFloatInputField(dialogTmp, "diffusion/x", 1, min: 1f, max: float.PositiveInfinity, allowNegative: false);

                        // Bloom Threshold
                        SetFloatInputField(dialogTmp, "threshold/x", 2, min: 0f, max: 1.4f, allowNegative: false);

                        // Bloom Anamorphic Ratio
                        SetFloatInputField(dialogTmp, "anamorphic ratio/x", 3, min: -1f, max: 1f);

                        // Bloom Color
                        SetListColor((int)currentKeyframe.eventValues[4], 4, bloomColorButtons, Color.white, Color.black);

                        // Bloom Color Shift
                        SetFloatInputField(dialogTmp, "colorshift/x", 5);
                        SetFloatInputField(dialogTmp, "colorshift/y", 6);
                        SetFloatInputField(dialogTmp, "colorshift/z", 7);
                        break;
                    }
                case 7: // Vignette
                    {
                        // Vignette Intensity
                        SetFloatInputField(dialogTmp, "intensity", 0, allowNegative: false);

                        // Vignette Smoothness
                        SetFloatInputField(dialogTmp, "smoothness", 1);

                        // Vignette Rounded
                        SetToggle(dialogTmp, "roundness/rounded", 2, 1, 0);

                        // Vignette Roundness
                        SetFloatInputField(dialogTmp, "roundness", 3, 0.01f, 10f, float.NegativeInfinity, 1.2f);

                        // Vignette Center
                        SetVector2InputField(dialogTmp, "position", 4, 5);

                        // Vignette Color

                        RTEditor.SetActive(dialogTmp.Find("colors").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("colorshift").gameObject, RTEditor.ShowModdedUI);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        SetListColor((int)currentKeyframe.eventValues[6], 6, vignetteColorButtons, Color.black, Color.black);
                        // Vignette Color Shift
                        SetFloatInputField(dialogTmp, "colorshift/x", 7);
                        SetFloatInputField(dialogTmp, "colorshift/y", 8);
                        SetFloatInputField(dialogTmp, "colorshift/z", 9);

                        break;
                    }
                case 8: // Lens
                    {
                        // Lens Intensity
                        SetFloatInputField(dialogTmp, "lens/x", 0, 1f, 10f, -100f, 100f);

                        RTEditor.SetActive(dialogTmp.Find("center").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("intensity").gameObject, RTEditor.ShowModdedUI);
                        RTEditor.SetActive(dialogTmp.Find("scale").gameObject, RTEditor.ShowModdedUI);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        // Lens Center X / Y
                        SetVector2InputField(dialogTmp, "center", 1, 2);

                        // Lens Intensity X / Y
                        SetVector2InputField(dialogTmp, "intensity", 3, 4);

                        // Lens Scale
                        SetFloatInputField(dialogTmp, "scale/x", 5, 0.1f, 10f, 0.001f, float.PositiveInfinity, allowNegative: false);
                        break;
                    }
                case 9: // Grain
                    {
                        // Grain Intensity
                        SetFloatInputField(dialogTmp, "intensity", 0, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);

                        // Grain Colored
                        SetToggle(dialogTmp, "colored", 1, 1, 0);

                        // Grain Size
                        SetFloatInputField(dialogTmp, "size", 2, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);

                        break;
                    }
                case 10: // ColorGrading
                    {
                        // ColorGrading Hueshift
                        SetFloatInputField(dialogTmp, "hueshift/x", 0, 0.1f, 10f);

                        // ColorGrading Contrast
                        SetFloatInputField(dialogTmp, "contrast/x", 1, 1f, 10f);

                        // ColorGrading Gamma
                        SetFloatInputField(dialogTmp, "gamma/x", 2);
                        SetFloatInputField(dialogTmp, "gamma/y", 3);
                        SetFloatInputField(dialogTmp, "gamma/z", 4);
                        SetFloatInputField(dialogTmp, "gamma/w", 5);

                        // ColorGrading Saturation
                        SetFloatInputField(dialogTmp, "saturation/x", 6, 1f, 10f);

                        // ColorGrading Temperature
                        SetFloatInputField(dialogTmp, "temperature/x", 7, 1f, 10f);

                        // ColorGrading Tint
                        SetFloatInputField(dialogTmp, "tint/x", 8, 1f, 10f);
                        break;
                    }
                case 11: // Ripples
                    {
                        // Ripples Strength
                        SetFloatInputField(dialogTmp, "strength/x", 0);

                        // Ripples Speed
                        SetFloatInputField(dialogTmp, "speed/x", 1);

                        // Ripples Distance
                        SetFloatInputField(dialogTmp, "distance/x", 2, 0.1f, 10f, 0.001f, float.PositiveInfinity);

                        SetVector2InputField(dialogTmp, "size", 3, 4);

                        // Ripples Mode (No separate method required atm)
                        {
                            var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[5];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[5] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }

                        break;
                    }
                case 12: // RadialBlur
                    {
                        // RadialBlur Intensity
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        // RadialBlur Iterations
                        SetIntInputField(dialogTmp, "iterations/x", 1, 1, 1, 20);

                        break;
                    }
                case 13: // ColorSplit
                    {
                        // ColorSplit Offset
                        SetFloatInputField(dialogTmp, "offset/x", 0);

                        // ColorSplit Mode (No separate method required atm)
                        {
                            var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[1];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[1] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }

                        break;
                    }
                case 14: // Cam Offset
                    {
                        SetVector2InputField(dialogTmp, "position", 0, 1);

                        break;
                    }
                case 15: // Gradient
                    {
                        // Gradient Intensity / Rotation (Had to put them together due to mode going over the timeline lol)
                        SetVector2InputField(dialogTmp, "introt", 0, 1);

                        // Gradient Color Top
                        SetListColor((int)currentKeyframe.eventValues[2], 2, gradientColor1Buttons, new Color(0f, 0.8f, 0.56f, 0.5f), Color.black);

                        // Gradient Color Bottom
                        SetListColor((int)currentKeyframe.eventValues[3], 3, gradientColor2Buttons, new Color(0.81f, 0.37f, 1f, 0.5f), Color.black);

                        // Gradient Mode (No separate method required atm)
                        {
                            var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[4];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[4] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }

                        // Gradient Top Color Shift
                        SetFloatInputField(dialogTmp, "colorshift1/x", 5);
                        SetFloatInputField(dialogTmp, "colorshift1/y", 6);
                        SetFloatInputField(dialogTmp, "colorshift1/z", 7);
                        SetFloatInputField(dialogTmp, "colorshift1/w", 8);

                        // Gradient Bottom Color Shift
                        SetFloatInputField(dialogTmp, "colorshift2/x", 9);
                        SetFloatInputField(dialogTmp, "colorshift2/y", 10);
                        SetFloatInputField(dialogTmp, "colorshift2/z", 11);
                        SetFloatInputField(dialogTmp, "colorshift2/w", 12);

                        break;
                    }
                case 16: // DoubleVision
                    {
                        // DoubleVision Intensity
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        // DoubleVision Mode (No separate method required atm)
                        {
                            var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[1];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[1] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }

                        break;
                    }
                case 17: // ScanLines
                    {
                        // ScanLines Intensity
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        // ScanLines Amount
                        SetFloatInputField(dialogTmp, "amount/x", 1);

                        // ScanLines Speed
                        SetFloatInputField(dialogTmp, "speed/x", 2);
                        break;
                    }
                case 18: // Blur
                    {
                        //Blur Amount
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        //Blur Iterations
                        SetIntInputField(dialogTmp, "iterations/x", 1, 1, 1, 12);

                        break;
                    }
                case 19: // Pixelize
                    {
                        //Pixelize
                        SetFloatInputField(dialogTmp, "amount/x", 0, 0.1f, 10f, 0f, 0.99f);

                        break;
                    }
                case 20: // BG
                    {
                        SetListColor((int)currentKeyframe.eventValues[0], 0, bgColorButtons, GameManager.inst.LiveTheme.backgroundColor, Color.black);

                        SetToggle(dialogTmp, "active", 1, 0, 1);

                        // BG Color Shift
                        SetFloatInputField(dialogTmp, "colorshift/x", 2);
                        SetFloatInputField(dialogTmp, "colorshift/y", 3);
                        SetFloatInputField(dialogTmp, "colorshift/z", 4);

                        break;
                    }
                case 21: // Invert
                    {
                        //Invert Amount
                        SetFloatInputField(dialogTmp, "amount/x", 0, 0.1f, 10f, 0f, 1f);

                        break;
                    }
                case 22: // Timeline
                    {
                        // Timeline Active
                        SetToggle(dialogTmp, "active", 0, 0, 1);

                        // Timeline Position
                        SetVector2InputField(dialogTmp, "position", 1, 2);

                        // Timeline Scale
                        SetVector2InputField(dialogTmp, "scale", 3, 4);

                        // Timeline Rotation
                        SetFloatInputField(dialogTmp, "rotation/x", 5, 15f, 3f);

                        // Timeline Color
                        SetListColor((int)currentKeyframe.eventValues[6], 6, timelineColorButtons, GameManager.inst.LiveTheme.guiColor, Color.black);

                        // Timeline Color Shift
                        SetFloatInputField(dialogTmp, "colorshift/x", 7);
                        SetFloatInputField(dialogTmp, "colorshift/y", 8);
                        SetFloatInputField(dialogTmp, "colorshift/z", 9);
                        SetFloatInputField(dialogTmp, "colorshift/w", 10);

                        break;
                    }
                case 23: // Player
                    {
                        // Player Active
                        SetToggle(dialogTmp, "active", 0, 0, 1);

                        // Player Moveable
                        SetToggle(dialogTmp, "move", 1, 0, 1);

                        // Player Position
                        SetVector2InputField(dialogTmp, "position", 2, 3);

                        // Player Rotation
                        SetFloatInputField(dialogTmp, "rotation/x", 4, 15f, 3f);

                        SetToggle(dialogTmp, "oob", 5, 1, 0);

                        break;
                    }
                case 24: // Follow Player
                    {
                        // Follow Player Active
                        SetToggle(dialogTmp, "active", 0, 1, 0);

                        // Follow Player Move
                        SetToggle(dialogTmp, "move", 1, 1, 0);

                        // Follow Player Rotate
                        SetToggle(dialogTmp, "rotate", 2, 1, 0);

                        // Follow Player Sharpness
                        SetFloatInputField(dialogTmp, "position/x", 3, 0.1f, 10f, 0.001f, 1f);

                        // Follow Player Offset
                        SetFloatInputField(dialogTmp, "position/y", 4);

                        // Follow Player Limit Left
                        SetFloatInputField(dialogTmp, "limit horizontal/x", 5);

                        // Follow Player Limit Right
                        SetFloatInputField(dialogTmp, "limit horizontal/y", 6);

                        // Follow Player Limit Up
                        SetFloatInputField(dialogTmp, "limit vertical/x", 7);

                        // Follow Player Limit Down
                        SetFloatInputField(dialogTmp, "limit vertical/y", 8);

                        // Follow Player Anchor
                        SetFloatInputField(dialogTmp, "anchor/x", 9, 0.1f, 10f);

                        break;
                    }
                case 25: // Audio
                    {
                        // Audio Pitch
                        SetFloatInputField(dialogTmp, "music/x", 0, 0.1f, 10f, 0.001f, 10f, allowNegative: false);

                        // Audio Volume
                        SetFloatInputField(dialogTmp, "music/y", 1, 0.1f, 10f, 0f, 1f, allowNegative: false);

                        break;
                    }
                case 26: // Video BG Parent
                    {
                        // Position
                        SetFloatInputField(dialogTmp, "position/x", 0);
                        SetFloatInputField(dialogTmp, "position/y", 1);
                        SetFloatInputField(dialogTmp, "position/z", 2);

                        // Scale
                        SetFloatInputField(dialogTmp, "scale/x", 3);
                        SetFloatInputField(dialogTmp, "scale/y", 4);
                        SetFloatInputField(dialogTmp, "scale/z", 5);

                        // Rotation
                        SetFloatInputField(dialogTmp, "rotation/x", 6, 5f, 3f);
                        SetFloatInputField(dialogTmp, "rotation/y", 7, 5f, 3f);
                        SetFloatInputField(dialogTmp, "rotation/z", 8, 5f, 3f);

                        break;
                    }
                case 27: // Video BG
                    {
                        // Position
                        SetFloatInputField(dialogTmp, "position/x", 0);
                        SetFloatInputField(dialogTmp, "position/y", 1);
                        SetFloatInputField(dialogTmp, "position/z", 2);

                        // Scale
                        SetFloatInputField(dialogTmp, "scale/x", 3);
                        SetFloatInputField(dialogTmp, "scale/y", 4);
                        SetFloatInputField(dialogTmp, "scale/z", 5);

                        // Rotation
                        SetFloatInputField(dialogTmp, "rotation/x", 6, 5f, 3f);
                        SetFloatInputField(dialogTmp, "rotation/y", 7, 5f, 3f);
                        SetFloatInputField(dialogTmp, "rotation/z", 8, 5f, 3f);

                        // Render Type
                        {
                            var drp = dialogTmp.Find("rendertype").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[9];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[9] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }


                        break;
                    }
                case 28: // Sharpen
                    {
                        SetFloatInputField(dialogTmp, "intensity/x", 0);
                        break;
                    }
                case 29: // Bars
                    {
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        // Direction
                        {
                            var drp = dialogTmp.Find("direction").GetComponent<Dropdown>();
                            drp.onValueChanged.ClearAll();
                            drp.value = (int)currentKeyframe.eventValues[1];
                            drp.onValueChanged.AddListener(_val =>
                            {
                                currentKeyframe.eventValues[1] = _val;
                                EventManager.inst.updateEvents();
                            });
                        }

                        break;
                    }
                case 30: // Danger
                    {
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        SetFloatInputField(dialogTmp, "size/x", 1);

                        // Danger Color
                        SetListColor((int)currentKeyframe.eventValues[2], 2, dangerColorButtons, new Color(0.66f, 0f, 0f), Color.black);

                        // Danger Color Shift
                        SetFloatInputField(dialogTmp, "colorshift/x", 3);
                        SetFloatInputField(dialogTmp, "colorshift/y", 4);
                        SetFloatInputField(dialogTmp, "colorshift/z", 5);
                        SetFloatInputField(dialogTmp, "colorshift/w", 6);

                        break;
                    }
                case 31: // 3D Rotation
                    {
                        SetFloatInputField(dialogTmp, "rotation/x", 0, 5f, 3f);
                        SetFloatInputField(dialogTmp, "rotation/y", 1, 5f, 3f);

                        break;
                    }
                case 32: // Camera Depth
                    {
                        SetFloatInputField(dialogTmp, "depth/x", 0);
                        SetFloatInputField(dialogTmp, "zoom/x", 1);
                        SetToggle(dialogTmp, "global", 2, 0, 1);

                        break;
                    }
                case 33: // Window Base
                    {
                        // Force Resolution
                        SetToggle(dialogTmp, "force", 0, 1, 0);

                        SetVector2InputField(dialogTmp, "resolution", 1, 2, max: int.MaxValue, allowNegative: false);

                        SetToggle(dialogTmp, "allow", 3, 1, 0);

                        break;
                    }
                case 34: // Window Position X
                    {
                        SetFloatInputField(dialogTmp, "x/x", 0);

                        break;
                    }
                case 35: // Window Position Y
                    {
                        SetFloatInputField(dialogTmp, "y/x", 0);

                        break;
                    }
                case 36: // Player Force
                    {
                        SetVector2InputField(dialogTmp, "position", 0, 1);
                        break;
                    }
                case 37: // Mosaic
                    {
                        SetFloatInputField(dialogTmp, "amount/x", 0);

                        break;
                    }
                case 38: // Analog Glitch
                    {
                        SetToggle(dialogTmp, "enabled", 0, 1, 0);
                        SetFloatInputField(dialogTmp, "colordrift/x", 1);
                        SetFloatInputField(dialogTmp, "horizontalshake/x", 2);
                        SetFloatInputField(dialogTmp, "scanlinejitter/x", 3);
                        SetFloatInputField(dialogTmp, "verticaljump/x", 4);

                        break;
                    }
                case 39: // Digital Glitch
                    {
                        SetFloatInputField(dialogTmp, "intensity/x", 0);

                        break;
                    }
            }

            // Curves
            var curvesDropdown = dialogTmp.transform.Find("curves").GetComponent<Dropdown>();

            dialogTmp.transform.Find("curves_label").gameObject.SetActive(isNotFirst);
            curvesDropdown.gameObject.SetActive(isNotFirst);
            if (isNotFirst)
            {
                curvesDropdown.onValueChanged.ClearAll();
                if (DataManager.inst.AnimationListDictionaryBack.TryGetValue(currentKeyframe.curveType, out int animIndex))
                    curvesDropdown.value = animIndex;

                curvesDropdown.onValueChanged.AddListener(_val =>
                {
                    if (!DataManager.inst.AnimationListDictionary.TryGetValue(_val, out DataManager.LSAnimation anim))
                        return;

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0 && x.Type == __instance.currentEventType))
                        kf.GetData<EventKeyframe>().curveType = anim;

                    RenderEventObjects();
                    eventManager.updateEvents();
                });
            }

            RenderIndexSelector(dialogTmp, __instance.currentEvent);
            var editDelete = dialogTmp.Find("edit/del").GetComponent<Button>();

            editDelete.onClick.ClearAll();
            editDelete.interactable = isNotFirst;
            editDelete.onClick.AddListener(DeleteKeyframes().Start);

            if (dialogTmp.Find("edit/copy") && dialogTmp.Find("edit/paste"))
            {
                var copy = dialogTmp.Find("edit/copy").GetComponent<Button>();
                var paste = dialogTmp.Find("edit/paste").GetComponent<Button>();

                copy.onClick.ClearAll();
                copy.onClick.AddListener(() => CopyKeyframeData(CurrentSelectedTimelineKeyframe));

                paste.onClick.ClearAll();
                paste.onClick.AddListener(() => PasteKeyframeData(__instance.currentEventType));
            }

            RenderTitle(__instance.currentEventType);
        }

        public void RenderIndexSelector(Transform dialogTmp, int currentIndex)
        {
            bool isNotFirst = currentIndex != 0;

            var editJumpLeftLarge = dialogTmp.Find("edit/<<").GetComponent<Button>();
            var editJumpLeft = dialogTmp.Find("edit/<").GetComponent<Button>();
            var editJumpRight = dialogTmp.Find("edit/>").GetComponent<Button>();
            var editJumpRightLarge = dialogTmp.Find("edit/>>").GetComponent<Button>();

            editJumpLeftLarge.interactable = isNotFirst;
            editJumpLeftLarge.onClick.ClearAll();
            editJumpLeftLarge.onClick.AddListener(() =>
            {
                EventEditor.inst.UpdateEventOrder(false);
                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, 0);
            });

            editJumpLeft.interactable = isNotFirst;
            editJumpLeft.onClick.ClearAll();
            editJumpLeft.onClick.AddListener(() =>
            {
                EventEditor.inst.UpdateEventOrder(false);
                int num = currentIndex - 1;
                if (num < 0)
                    num = 0;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
            });

            var indexText = dialogTmp.Find("edit/|/text").GetComponent<Text>();
            var allEvents = GameData.Current.eventObjects.allEvents[EventEditor.inst.currentEventType];

            indexText.text = !isNotFirst ? "S" : currentIndex == allEvents.Count ? "E" : currentIndex.ToString();

            editJumpRight.interactable = currentIndex != allEvents.Count - 1;
            editJumpRight.onClick.ClearAll();
            editJumpRight.onClick.AddListener(() =>
            {
                EventEditor.inst.UpdateEventOrder(false);
                int num = currentIndex + 1;
                if (num >= allEvents.Count)
                    num = allEvents.Count - 1;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
            });

            editJumpRightLarge.interactable = currentIndex != allEvents.Count - 1;
            editJumpRightLarge.onClick.ClearAll();
            editJumpRightLarge.onClick.AddListener(() =>
            {
                EventEditor.inst.UpdateEventOrder(false);
                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, allEvents.IndexOf(allEvents.Last()));
            });

        }

        public void CopyKeyframeData(TimelineObject currentKeyframe)
        {
            if (copiedKeyframeDatas.Count > currentKeyframe.Type)
            {
                copiedKeyframeDatas[currentKeyframe.Type] = EventKeyframe.DeepCopy(currentKeyframe.GetData<EventKeyframe>());
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
                    var kf = keyframe.GetData<EventKeyframe>();

                    kf.curveType = copiedKeyframeDatas[type].curveType;
                    kf.eventValues = copiedKeyframeDatas[type].eventValues.Copy();
                    kf.eventRandomValues = copiedKeyframeDatas[type].eventRandomValues.Copy();
                    kf.random = copiedKeyframeDatas[type].random;
                    kf.relative = copiedKeyframeDatas[type].relative;
                }

                RenderEventsDialog();
                EventManager.inst.updateEvents();
                EditorManager.inst.DisplayNotification($"Pasted {EventTypes[type]} keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
            }
            else if (copiedKeyframeDatas.Count > type)
                EditorManager.inst.DisplayNotification($"{EventTypes[type]} keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification("Keyframe type does not exist yet.", 2f, EditorManager.NotificationType.Error);
        }

        public List<EventKeyframe> copiedKeyframeDatas = new List<EventKeyframe>();

        public void SetListColor(int value, int index, List<Toggle> toggles, Color defaultColor, Color secondaryDefaultColor)
        {
            int num = 0;
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.ClearAll();

                toggle.isOn = num == value;

                toggle.image.color = num < 18 ? CoreHelper.CurrentBeatmapTheme.effectColors[num] : num == 19 ? secondaryDefaultColor : defaultColor;

                int tmpIndex = num;
                toggle.onValueChanged.AddListener(_val =>
                {
                    foreach (var kf in SelectedKeyframes.Where(x => x.Type == EventEditor.inst.currentEventType))
                        kf.GetData<EventKeyframe>().eventValues[index] = tmpIndex;

                    EventManager.inst.updateEvents();

                    SetListColor(tmpIndex, index, toggles, defaultColor, secondaryDefaultColor);
                });
                num++;
            }
        }

        public void SetToggle(Transform dialogTmp, string name, int index, int onValue, int offValue)
        {
            var __instance = EventEditor.inst;
            var currentKeyframe = GameData.Current.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

            var vignetteRounded = dialogTmp.Find(name).GetComponent<Toggle>();
            vignetteRounded.onValueChanged.ClearAll();
            vignetteRounded.isOn = currentKeyframe.eventValues[index] == onValue;
            vignetteRounded.onValueChanged.AddListener(_val =>
            {
                foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                    kf.GetData<EventKeyframe>().eventValues[index] = _val ? onValue : offValue;

                EventManager.inst.updateEvents();
            });
        }

        public void SetFloatInputField(Transform dialogTmp, string name, int index, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true)
        {
            var __instance = EventEditor.inst;

            var currentKeyframe = GameData.Current.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

            if (!dialogTmp.Find(name))
                return;

            var zoom = dialogTmp.Find($"{name}").GetComponent<InputField>();
            zoom.onEndEdit.ClearAll();
            zoom.onValueChanged.ClearAll();
            zoom.text = currentKeyframe.eventValues[index].ToString();
            zoom.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (min != 0f || max != 0f)
                        num = Mathf.Clamp(num, min, max);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                        kf.GetData<EventKeyframe>().eventValues[index] = num;

                    EventManager.inst.updateEvents();

                    if (name == "zoom/x" && num < 0f)
                        AchievementManager.inst.UnlockAchievement("editor_zoom_break");
                }
                else
                    LogIncorrectFormat(_val);
            });
            zoom.onEndEdit.AddListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.eventTime },
                    { "currentValue", currentKeyframe.eventValues[index] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.eventValues[index], variables, out float calc))
                    zoom.text = calc.ToString();
            });

            if (dialogTmp.Find($"{name}/<") && dialogTmp.Find($"{name}/>"))
            {
                var tf = dialogTmp.Find($"{name}");

                float num = 1f;

                var btR = tf.Find("<").GetComponent<Button>();
                var btL = tf.Find(">").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(zoom.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[index] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            zoom.text = result.ToString();
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(zoom.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                            {
                                kf.GetData<EventKeyframe>().eventValues[index] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                            }
                        else
                            zoom.text = result.ToString();
                    }
                });
            }

            TriggerHelper.AddEventTriggers(zoom.gameObject, TriggerHelper.ScrollDelta(zoom, increase, multiply, min, max));

            if (allowNegative && !zoom.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = zoom.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(zoom, InputFieldSwapper.Type.Num);
            }
        }

        public void SetIntInputField(Transform dialogTmp, string name, int index, int increase = 1, int min = 0, int max = 0, bool allowNegative = true)
        {
            var __instance = EventEditor.inst;

            var currentKeyframe = GameData.Current.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

            if (!dialogTmp.Find(name))
                return;

            var zoom = dialogTmp.Find($"{name}").GetComponent<InputField>();
            zoom.onValueChanged.ClearAll();
            zoom.text = currentKeyframe.eventValues[index].ToString();
            zoom.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    if (min != 0 && max != 0)
                        num = Mathf.Clamp(num, min, max);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                        kf.GetData<EventKeyframe>().eventValues[index] = num;

                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(_val);
            });

            if (dialogTmp.Find($"{name}/<") && dialogTmp.Find($"{name}/>"))
            {
                var tf = dialogTmp.Find($"{name}");

                float num = 1f;

                var btR = tf.Find("<").GetComponent<Button>();
                var btL = tf.Find(">").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(zoom.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[index] -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            zoom.text = result.ToString();
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(zoom.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[index] += Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            zoom.text = result.ToString();
                    }
                });
            }

            TriggerHelper.AddEventTriggers(zoom.gameObject, TriggerHelper.ScrollDeltaInt(zoom, increase, min, max));

            if (allowNegative && !zoom.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = zoom.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(zoom, InputFieldSwapper.Type.Num);
            }
        }

        public void SetVector2InputField(Transform dialogTmp, string name, int xindex, int yindex, float min = 0f, float max = 0f, bool allowNegative = true)
        {
            var __instance = EventEditor.inst;

            var currentKeyframe = GameData.Current.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

            if (!dialogTmp.Find(name) || !dialogTmp.Find($"{name}/x") || !dialogTmp.Find($"{name}/y"))
                return;

            var posX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
            var posY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();

            posX.onEndEdit.ClearAll();
            posX.onValueChanged.ClearAll();
            posX.text = currentKeyframe.eventValues[xindex].ToString();
            posX.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (min != 0f && max != 0f)
                        num = Mathf.Clamp(num, min, max);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                        kf.GetData<EventKeyframe>().eventValues[xindex] = num;

                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(_val);
            });
            posX.onEndEdit.AddListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.eventTime },
                    { "currentValueX", currentKeyframe.eventValues[xindex] },
                    { "currentValueY", currentKeyframe.eventValues[yindex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.eventValues[xindex], variables, out float calc))
                    posX.text = calc.ToString();
            });

            posY.onValueChanged.ClearAll();
            posY.text = currentKeyframe.eventValues[yindex].ToString();
            posY.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (min != 0f && max != 0f)
                        num = Mathf.Clamp(num, min, max);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                        kf.GetData<EventKeyframe>().eventValues[yindex] = num;

                    EventManager.inst.updateEvents();
                }
                else
                    LogIncorrectFormat(_val);
            });
            posY.onEndEdit.AddListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.eventTime },
                    { "currentValueX", currentKeyframe.eventValues[xindex] },
                    { "currentValueY", currentKeyframe.eventValues[yindex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.eventValues[yindex], variables, out float calc))
                    posY.text = calc.ToString();
            });

            if (dialogTmp.Find($"{name}/x/<") && dialogTmp.Find($"{name}/x/>"))
            {
                var tf = dialogTmp.Find($"{name}/x");

                float num = 1f;

                var btR = tf.Find("<").GetComponent<Button>();
                var btL = tf.Find(">").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(posX.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[xindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posX.text = result.ToString();
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(posX.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                            {
                                kf.GetData<EventKeyframe>().eventValues[xindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                            }
                        else
                            posX.text = result.ToString();
                    }
                });
            }

            if (dialogTmp.Find($"{name}/y/<") && dialogTmp.Find($"{name}/y/>"))
            {
                var tf = dialogTmp.Find($"{name}/y");

                float num = 1f;

                var btR = tf.Find("<").GetComponent<Button>();
                var btL = tf.Find(">").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(posY.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[yindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posY.text = result.ToString();
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(posY.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.GetData<EventKeyframe>().eventValues[yindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posY.text = result.ToString();
                    }
                });
            }

            var clampList = new List<float> { min, max };
            TriggerHelper.AddEventTriggers(posX.gameObject, TriggerHelper.ScrollDelta(posX, 0.1f, 10f, min, max, true), TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList));
            TriggerHelper.AddEventTriggers(posY.gameObject, TriggerHelper.ScrollDelta(posY, 0.1f, 10f, min, max, true), TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList));

            if (allowNegative)
            {
                if (!posX.gameObject.GetComponent<InputFieldSwapper>())
                {
                    var ifh = posX.gameObject.AddComponent<InputFieldSwapper>();
                    ifh.Init(posX, InputFieldSwapper.Type.Num);
                }
                if (!posY.gameObject.GetComponent<InputFieldSwapper>())
                {
                    var ifh = posY.gameObject.AddComponent<InputFieldSwapper>();
                    ifh.Init(posY, InputFieldSwapper.Type.Num);
                }
            }
        }

        #endregion

        #region Rendering

        public void UpdateEventOrder()
        {
            for (int i = 0; i < AllEvents.Count; i++)
            {
                GameData.Current.eventObjects.allEvents[i] = GameData.Current.eventObjects.allEvents[i].OrderBy(x => x.eventTime).ToList();
                foreach (var keyframe in RTEditor.inst.timelineKeyframes)
                {
                    if (GameData.Current.eventObjects.allEvents[i].TryFindIndex(x => ((EventKeyframe)x).id == keyframe.ID, out int index))
                        keyframe.Index = index;
                }
            }
        }

        void RenderTitle(int i)
        {
            var theme = EditorThemeManager.CurrentTheme;
            var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
            var image = title.GetChild(0).GetComponent<Image>();
            image.color = theme.ContainsGroup($"Event Color {i % EVENT_LIMIT + 1} Editor") ? theme.GetColor($"Event Color {i % EVENT_LIMIT + 1} Editor") : Color.white;
            image.color = LSColors.fadeColor(image.color, 1f);
            image.rectTransform.sizeDelta = new Vector2(17f, 0f);
            title.GetChild(1).GetComponent<Text>().text = $"- {EventTypes[i]} Editor - ";
        }

        public void RenderLayerBins()
        {
            var renderLeft = EditorConfig.Instance.EventLabelsRenderLeft.Value;
            var eventLabels = EventEditor.inst.EventLabels;

            var layer = RTEditor.inst.Layer + 1;
            int num = Mathf.Clamp(layer * EVENT_LIMIT, 0, (RTEditor.ShowModdedUI ? layer * EVENT_LIMIT : 10));

            for (int i = 0; i < AllEvents.Count; i++)
            {
                int t = i % EVENT_LIMIT;

                var text = eventLabels.transform.GetChild(t).GetChild(0).GetComponent<Text>();

                if (i < EventTypes.Length)
                {
                    if (i >= num - EVENT_LIMIT && i < num)
                        text.text = EventTypes[i];
                    else if (i < num)
                        text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NO_EVENT_LABEL;
                }
                else
                    text.text = layer == 69 ? "lol" : layer == 555 ? "Hahaha" : NO_EVENT_LABEL;

                text.alignment = renderLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

                if (!RTEditor.ShowModdedUI && RTEditor.inst.Layer > 0)
                    text.text = "No Event";
            }

            var theme = EditorThemeManager.CurrentTheme;
            for (int i = 0; i < 15; i++)
            {
                var img = EventBins[i];

                var enabled = i == 14 || i < (RTEditor.ShowModdedUI ? 14 : 10);

                img.enabled = enabled;
                EventLabels[i].enabled = enabled;

                if (enabled)
                    img.color = theme.ContainsGroup($"Event Color {i % EVENT_LIMIT + 1}") ? theme.GetColor($"Event Color {i % EVENT_LIMIT + 1}") : Color.white;
            }
        }

        public void SetEventActive(bool active)
        {
            EventEditor.inst.EventLabels.SetActive(active);
            EventEditor.inst.EventHolders.SetActive(active);
        }

        #endregion
    }
}
