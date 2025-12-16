using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents a dialog window in the editor used to edit a keyframe.
    /// </summary>
    public class KeyframeDialog : Exists, IIndexDialog
    {
        public KeyframeDialog() { }

        public KeyframeDialog(int type) => this.type = type;

        #region Values

        /// <summary>
        /// Name of the keyframes' type.
        /// </summary>
        public string Name => isObjectKeyframe ? KeyframeTimeline.IntToTypeName(type) : EventLibrary.displayNames[type];

        /// <summary>
        /// Game object of the keyframe editor.
        /// </summary>
        public GameObject GameObject { get; set; }

        #region Edit

        public Transform Edit { get; set; }
        public Button JumpToStartButton { get; set; }
        public Button JumpToPrevButton { get; set; }
        public Text KeyframeIndexer { get; set; }
        public Button JumpToNextButton { get; set; }
        public Button JumpToLastButton { get; set; }
        public FunctionButtonStorage CopyButton { get; set; }
        public FunctionButtonStorage PasteButton { get; set; }
        public DeleteButtonStorage DeleteButton { get; set; }

        #endregion

        #region Main Keyframe Values

        public GameObject CurvesLabel { get; set; }
        public Dropdown CurvesDropdown { get; set; }
        public InputFieldStorage EventTimeField { get; set; }
        public List<Text> EventValueLabels { get; set; }
        public Transform EventValuesParent { get; set; }
        public List<InputFieldStorage> EventValueFields { get; set; }
        public List<KeyframeElement> EventValueElements { get; set; }

        #endregion

        #region Keyframe Random Values

        public GameObject RandomEventValueLabels { get; set; }
        public GameObject RandomEventValueParent { get; set; }
        public List<InputFieldStorage> RandomEventValueFields { get; set; }
        public ToggleButtonStorage RelativeToggle { get; set; }
        public ToggleButtonStorage FleeToggle { get; set; }
        public Transform RandomTogglesParent { get; set; }
        public List<Toggle> RandomToggles { get; set; }
        public InputField RandomIntervalField { get; set; }
        public Dropdown RandomAxisDropdown { get; set; }

        #endregion

        /// <summary>
        /// The type of the keyframe. (e.g. position, scale, etc)
        /// </summary>
        public int type;

        /// <summary>
        /// If the keyframe editor is a multi keyframe editor.
        /// </summary>
        public bool isMulti;

        /// <summary>
        /// If the keyframe is from an object.
        /// </summary>
        public bool isObjectKeyframe;

        public List<string> originalLabels;

        public bool newDialog;

        public string ObjectName => type switch
        {
            EventLibrary.Indexes.MOVE => "move",
            EventLibrary.Indexes.ZOOM => "zoom",
            EventLibrary.Indexes.ROTATE => "rotate",
            EventLibrary.Indexes.SHAKE => "shake",
            EventLibrary.Indexes.THEME => "theme",
            EventLibrary.Indexes.CHROMA => "chroma",
            EventLibrary.Indexes.BLOOM => "bloom",
            EventLibrary.Indexes.VIGNETTE => "vignette",
            EventLibrary.Indexes.LENS => "lens",
            EventLibrary.Indexes.GRAIN => "grain",
            EventLibrary.Indexes.COLORGRADING => "colorgrading",
            EventLibrary.Indexes.RIPPLES => "ripples",
            EventLibrary.Indexes.RADIALBLUR => "radialblur",
            EventLibrary.Indexes.COLORSPLIT => "colorsplit",
            EventLibrary.Indexes.MOVE_OFFSET => "camoffset",
            EventLibrary.Indexes.GRADIENT => "gradient",
            EventLibrary.Indexes.DOUBLEVISION => "doublevision",
            EventLibrary.Indexes.SCANLINES => "scanlines",
            EventLibrary.Indexes.BLUR => "blur",
            EventLibrary.Indexes.PIXELIZE => "pixelize",
            EventLibrary.Indexes.BG => "bg",
            EventLibrary.Indexes.INVERT => "invert",
            EventLibrary.Indexes.TIMELINE => "timeline",
            EventLibrary.Indexes.PLAYER => "player",
            EventLibrary.Indexes.FOLLOW_PLAYER => "followplayer",
            EventLibrary.Indexes.AUDIO => "audio",
            EventLibrary.Indexes.VIDEO_PARENT => "videoparent",
            EventLibrary.Indexes.VIDEO => "video",
            EventLibrary.Indexes.SHARPEN => "sharpen",
            EventLibrary.Indexes.BARS => "bars",
            EventLibrary.Indexes.DANGER => "danger",
            EventLibrary.Indexes.DEPTH_ROTATION => "depthrotation",
            EventLibrary.Indexes.CAMERA_DEPTH => "cameradepth",
            EventLibrary.Indexes.WINDOW_BASE => "windowbase",
            EventLibrary.Indexes.WINDOW_POSITION_X => "windowpositionx",
            EventLibrary.Indexes.WINDOW_POSITION_Y => "windowpositionY",
            EventLibrary.Indexes.PLAYER_FORCE => "playerforce",
            EventLibrary.Indexes.MOSAIC => "mosaic",
            EventLibrary.Indexes.ANALOG_GLITCH => "analogglitch",
            EventLibrary.Indexes.DIGITAL_GLITCH => "digitalglitch",
            _ => string.Empty,
        };

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new object for the keyframe dialog.
        /// </summary>
        /// <param name="parent">Parent of the dialog.</param>
        public void CreateNew(Transform parent) => CreateNew(parent, ObjectName);

        /// <summary>
        /// Creates a new object for the keyframe dialog.
        /// </summary>
        /// <param name="parent">Parent of the dialog.</param>
        /// <param name="name">Name of the dialog.</param>
        public virtual void CreateNew(Transform parent, string name)
        {
            newDialog = true;
            CoreHelper.Delete(GameObject);

            GameObject = EditorPrefabHolder.Instance.EventEditor.Duplicate(parent, name);
        }

        /// <summary>
        /// Initializes the keyframe dialog.
        /// </summary>
        public virtual void Init()
        {
            if (!GameObject)
                return;

            Edit = GameObject.transform.Find("edit");
            if (!isObjectKeyframe)
                InitEdit();
            RTEditor.inst.SetupIndexer(this);
            InitTopPanel();

            if (isMulti)
                return;

            try
            {
                CurvesLabel = GameObject.transform.Find("curves_label").gameObject;
                CurvesDropdown = GameObject.transform.Find("curves").GetComponent<Dropdown>();
                EventTimeField = GameObject.transform.Find("time").gameObject.GetOrAddComponent<InputFieldStorage>();
                EventTimeField.Assign(EventTimeField.gameObject);

                GameObject.transform.GetChild(2).gameObject.SetActive(false);
                if (type != 4)
                    GameObject.transform.GetChild(7).gameObject.SetActive(false);

                if (isObjectKeyframe)
                {
                    if (GameObject.transform.TryFind(Name.ToLower(), out Transform valuesTransform))
                    {
                        EventValuesParent = valuesTransform;
                        var labels = valuesTransform.GetPreviousSibling();
                        try
                        {
                            if (labels)
                            {
                                originalLabels = new List<string>(labels.childCount);
                                EventValueLabels = new List<Text>(labels.childCount);
                                for (int i = 0; i < labels.childCount; i++)
                                {
                                    var label = labels.GetChild(i).GetComponent<Text>();
                                    originalLabels.Add(label ? label.text : string.Empty);
                                    EventValueLabels.Add(label);
                                    EditorThemeManager.ApplyLightText(label);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogException(ex);
                        }

                        EventValueFields = new List<InputFieldStorage>();
                        EventValueElements = new List<KeyframeElement>();
                        if (!isObjectKeyframe)
                            for (int i = 0; i < valuesTransform.childCount; i++)
                            {
                                var eventValueField = valuesTransform.GetChild(i).gameObject.GetOrAddComponent<InputFieldStorage>();
                                eventValueField.Assign(eventValueField.gameObject);
                                EditorThemeManager.ApplyInputField(eventValueField);
                                EventValueFields.Add(eventValueField);
                            }
                        else if (type != 3)
                            CoreHelper.DestroyChildren(EventValuesParent);
                    }

                    if (GameObject.transform.TryFind($"r_{Name.ToLower()}", out Transform randomValuesTransform))
                    {
                        RandomEventValueLabels = GameObject.transform.Find($"r_{Name.ToLower()}_label")?.gameObject;
                        try
                        {
                            if (RandomEventValueLabels)
                                for (int i = 0; i < RandomEventValueLabels.transform.childCount; i++)
                                    EditorThemeManager.ApplyLightText(RandomEventValueLabels.transform.GetChild(i).GetComponent<Text>());
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogException(ex);
                        }

                        RandomEventValueFields = new List<InputFieldStorage>();
                        RandomEventValueParent = randomValuesTransform.gameObject;
                        for (int i = 0; i < randomValuesTransform.childCount; i++)
                        {
                            var eventValueField = randomValuesTransform.GetChild(i).gameObject.GetOrAddComponent<InputFieldStorage>();
                            eventValueField.Assign(eventValueField.gameObject);
                            EditorThemeManager.ApplyInputField(eventValueField);
                            RandomEventValueFields.Add(eventValueField);
                        }
                    }
                }

                if (GameObject.transform.TryFind("relative", out Transform relativeTransform))
                    RelativeToggle = relativeTransform.GetComponent<ToggleButtonStorage>();
                
                if (GameObject.transform.TryFind("flee", out Transform fleeTransform))
                    FleeToggle = fleeTransform.GetComponent<ToggleButtonStorage>();

                if (GameObject.transform.TryFind("r_label", out Transform randomLabelTransform))
                {
                    for (int i = 0; i < randomLabelTransform.childCount; i++)
                        EditorThemeManager.ApplyLightText(randomLabelTransform.GetChild(i).GetComponent<Text>());
                }

                if (GameObject.transform.TryFind("random", out Transform randomTransform))
                {
                    RandomTogglesParent = randomTransform;
                    RandomToggles = new List<Toggle>();
                    for (int i = 0; i < randomTransform.childCount - 2; i++)
                    {
                        var toggle = randomTransform.GetChild(i).GetComponent<Toggle>();
                        toggle.group = null;
                        EditorThemeManager.ApplyToggle(toggle, ThemeGroup.Background_3);
                        EditorThemeManager.ApplyGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                        if (!toggle.GetComponent<HoverUI>())
                        {
                            var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                            hoverUI.animatePos = false;
                            hoverUI.animateSca = true;
                            hoverUI.size = 1.1f;
                        }
                        RandomToggles.Add(toggle);
                    }

                    RandomIntervalField = randomTransform.Find("interval-input").GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(RandomIntervalField);

                    if (GameObject.transform.TryFind("r_axis", out Transform rAxisTransform))
                        RandomAxisDropdown = rAxisTransform.GetComponent<Dropdown>();
                }

                var curvesLabel = CurvesLabel.transform.GetChild(0).GetComponent<Text>();
                curvesLabel.text = "Ease Type";

                EditorThemeManager.ApplyLightText(curvesLabel);
                EditorThemeManager.ApplyDropdown(CurvesDropdown);
                EditorThemeManager.ApplyInputField(EventTimeField);

                ApplyLabelThemes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to set main: {ex}");
            }
        }

        /// <summary>
        /// Renders the keyframe dialog.
        /// </summary>
        public virtual void Render()
        {
            var currentKeyframe = GetCurrentKeyframe();
            var keyframeCoord = GetSelectionCoord();

            bool isNotFirst = keyframeCoord.index != 0;

            CurvesLabel.gameObject.SetActive(isNotFirst);
            CurvesDropdown.gameObject.SetActive(isNotFirst);

            EventTimeField.inputField.onValueChanged.ClearAll();
            EventTimeField.inputField.text = currentKeyframe.time.ToString("f3");

            TriggerHelper.SetInteractable(isNotFirst,
                EventTimeField.inputField,
                EventTimeField.leftGreaterButton,
                EventTimeField.leftButton,
                EventTimeField.rightButton,
                EventTimeField.rightGreaterButton);

            if (isNotFirst)
            {
                CurvesDropdown.SetValueWithoutNotify(RTEditor.inst.GetEaseIndex(currentKeyframe.curve.ToString()));
                CurvesDropdown.onValueChanged.NewListener(_val =>
                {
                    var anim = RTEditor.inst.GetEasing(_val);
                    foreach (var kf in GetSelectedKeyframes())
                        kf.eventKeyframe.curve = anim;

                    if (isObjectKeyframe)
                    {
                        KeyframeTimeline.CurrentTimeline.RenderKeyframes(KeyframeTimeline.CurrentTimeline.CurrentObject);
                        if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                            RTLevel.Current?.UpdateObject(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), ObjectContext.KEYFRAMES);
                    }
                    else
                    {
                        RTEventEditor.inst.RenderTimelineKeyframes();
                        RTLevel.Current?.UpdateEvents();
                    }
                });
                TriggerHelper.AddEventTriggers(CurvesDropdown.gameObject, TriggerHelper.ScrollDelta(CurvesDropdown));

                EventTimeField.inputField.onValueChanged.AddListener(_val =>
                {
                    if (!float.TryParse(_val, out float num))
                        return;

                    num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in GetSelectedKeyframes())
                    {
                        kf.Time = num;
                        kf.Render();
                    }

                    if (!isObjectKeyframe)
                        RTLevel.Current?.UpdateEvents(EventEditor.inst.currentEventType);
                    else if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                        RTLevel.Current?.UpdateObject(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), ObjectContext.KEYFRAMES);
                });

                TriggerHelper.IncreaseDecreaseButtons(EventTimeField);
                TriggerHelper.AddEventTriggers(EventTimeField.gameObject, TriggerHelper.ScrollDelta(EventTimeField.inputField, min: 0.001f, max: AudioManager.inst.CurrentAudioSource.clip.length));
            }

            #region Edit

            if (JumpToStartButton)
            {
                JumpToStartButton.interactable = isNotFirst;
                JumpToStartButton.onClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                        KeyframeTimeline.CurrentTimeline.SetCurrentKeyframe(KeyframeTimeline.CurrentTimeline.CurrentObject, 0);
                    else
                    {
                        RTLevel.Current?.UpdateEvents(EventEditor.inst.currentEventType);
                        EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, 0);
                    }
                });
            }

            if (JumpToPrevButton)
            {
                JumpToPrevButton.interactable = isNotFirst;
                JumpToPrevButton.onClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                    {
                        var num = KeyframeTimeline.CurrentTimeline.currentKeyframeIndex - 1;
                        if (num < 0)
                            num = 0;
                        KeyframeTimeline.CurrentTimeline.SetCurrentKeyframe(KeyframeTimeline.CurrentTimeline.CurrentObject, num);
                    }
                    else
                    {
                        RTLevel.Current?.UpdateEvents(EventEditor.inst.currentEventType);
                        int num = EventEditor.inst.currentEvent - 1;
                        if (num < 0)
                            num = 0;

                        EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                    }
                });
            }

            var events = isObjectKeyframe ? KeyframeTimeline.CurrentTimeline.CurrentObject.Events[keyframeCoord.type] : GameData.Current.events[keyframeCoord.type];

            if (KeyframeIndexer)
                KeyframeIndexer.text = !isNotFirst ? "S" : keyframeCoord.index == events.Count - 1 ? "E" : keyframeCoord.index.ToString();

            if (JumpToNextButton)
            {
                JumpToNextButton.interactable = keyframeCoord.index != events.Count - 1;
                JumpToNextButton.onClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                    {
                        var num = KeyframeTimeline.CurrentTimeline.currentKeyframeIndex + 1;
                        if (num >= events.Count)
                            num = events.Count - 1;

                        KeyframeTimeline.CurrentTimeline.SetCurrentKeyframe(KeyframeTimeline.CurrentTimeline.CurrentObject, num);
                    }
                    else
                    {
                        RTLevel.Current?.UpdateEvents(EventEditor.inst.currentEventType);
                        int num = EventEditor.inst.currentEvent + 1;
                        if (num >= events.Count)
                            num = events.Count - 1;

                        EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                    }
                });
            }

            if (JumpToLastButton)
            {
                JumpToLastButton.interactable = keyframeCoord.index != events.Count - 1;
                JumpToLastButton.onClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                        KeyframeTimeline.CurrentTimeline.SetCurrentKeyframe(KeyframeTimeline.CurrentTimeline.CurrentObject, events.Count - 1);
                    else
                    {
                        RTLevel.Current?.UpdateEvents(EventEditor.inst.currentEventType);
                        EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, events.Count - 1);
                    }
                });
            }

            DeleteButton.Interactable = isNotFirst;
            DeleteButton.OnClick.NewListener(() =>
            {
                if (isObjectKeyframe)
                    CoroutineHelper.StartCoroutine(KeyframeTimeline.CurrentTimeline.DeleteKeyframes(KeyframeTimeline.CurrentTimeline.CurrentObject));
                else
                    RTEventEditor.inst.DeleteKeyframes();
            });

            if (CopyButton && PasteButton)
            {
                CopyButton.OnClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                        KeyframeTimeline.CurrentTimeline.CopyData(KeyframeTimeline.CurrentTimeline.currentKeyframeType, currentKeyframe);
                    else
                        RTEventEditor.inst.CopyKeyframeData(RTEventEditor.inst.CurrentSelectedKeyframe?.timelineKeyframe);
                });
                PasteButton.OnClick.NewListener(() =>
                {
                    if (isObjectKeyframe)
                        KeyframeTimeline.CurrentTimeline.PasteKeyframeData(KeyframeTimeline.CurrentTimeline.currentKeyframeType, GetSelectedKeyframes(), KeyframeTimeline.CurrentTimeline.CurrentObject);
                    else
                        RTEventEditor.inst.PasteKeyframeData(EventEditor.inst.currentEventType);
                });
            }

            #endregion
        }

        /// <summary>
        /// Sets the Keyframe Dialogs' active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        public void InitCustomUI(params CustomValueDisplay[] displays)
        {
            CoreHelper.DestroyChildren(EventValuesParent);
            EventValueElements.Clear();
            EventValueFields.Clear();

            for (int i = 0; i < displays.Length; i++)
            {
                var name = i switch
                {
                    0 => "x",
                    1 => "y",
                    2 => "z",
                    _ => string.Empty,
                };

                var display = displays[i];
                KeyframeElement element = display.type switch
                {
                    CustomValueDisplay.UIType.InputField => new KeyframeInputField(display.path),
                    CustomValueDisplay.UIType.Dropdown => new KeyframeDropdown(display.path),
                    CustomValueDisplay.UIType.Toggle => new KeyframeToggle(display.path),
                    _ => null,
                };
                element?.Init(this, EventValuesParent, name, display);
                EventValueElements.Add(element);
                EventValueFields.Add(element is KeyframeInputField inputField ? inputField.Field : null);
            }
        }

        public void InitCustomUI(CustomValueDisplay display)
        {
            int index = EventValueElements.Count;
            if (EventValueElements.TryFindIndex(x => x.path == display.path, out index))
            {
                CoreHelper.Delete(EventValueElements[index].GameObject);
                EventValueElements.RemoveAt(index);
                if (EventValueFields.InRange(index))
                    EventValueFields.RemoveAt(index);
            }

            var name = display.path.Split('/').Last();
            if (string.IsNullOrEmpty(name))
                return;

            KeyframeElement element = display.type switch
            {
                CustomValueDisplay.UIType.InputField => new KeyframeInputField(display.path),
                CustomValueDisplay.UIType.Dropdown => new KeyframeDropdown(display.path),
                CustomValueDisplay.UIType.Toggle => new KeyframeToggle(display.path),
                _ => null,
            };
            if (element)
            {
                element.Init(this, EventValuesParent, name, display);
                element.GameObject.transform.SetSiblingIndex(index);
            }
            if (!EventValueElements.IsEmpty())
            {
                EventValueElements.Insert(index, element);
                EventValueFields.Insert(index, element is KeyframeInputField inputField ? inputField.Field : null);
            }
            else
            {
                EventValueElements.Add(element);
                EventValueFields.Add(element is KeyframeInputField inputField ? inputField.Field : null);
            }
        }

        public void InitTopPanel()
        {
            try
            {
                var topPanel = GameObject.transform.GetChild(0);
                var bg = topPanel.GetChild(0).GetComponent<Image>();
                var title = topPanel.GetChild(1).GetComponent<Text>();
                bg.gameObject.GetOrAddComponent<ContrastColors>().Init(title, bg);

                if (isObjectKeyframe)
                    EditorThemeManager.ApplyGraphic(bg, EditorTheme.GetGroup($"Object Keyframe Color {type + 1}"));
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public void InitEdit()
        {
            if (!Edit)
                return;

            EditorHelper.SetComplexity(Edit.Find("spacer")?.gameObject, Complexity.Simple);

            if (!Edit.Find("copy"))
            {
                var copy = EditorPrefabHolder.Instance.Function1Button.Duplicate(Edit, "copy", 5);
                var copyStorage = copy.GetComponent<FunctionButtonStorage>();
                copyStorage.Text = "Copy";
                copy.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                EditorThemeManager.ApplyGraphic(copyStorage.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.ApplyGraphic(copyStorage.label, ThemeGroup.Copy_Text);

                EditorHelper.SetComplexity(copy, Complexity.Normal);
            }

            if (!Edit.Find("paste"))
            {
                var paste = EditorPrefabHolder.Instance.Function1Button.Duplicate(Edit, "paste", 6);
                var pasteStorage = paste.GetComponent<FunctionButtonStorage>();
                pasteStorage.Text = "Paste";
                paste.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                EditorThemeManager.ApplyGraphic(pasteStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(pasteStorage.label, ThemeGroup.Paste_Text);

                EditorHelper.SetComplexity(paste, Complexity.Normal);
            }
        }

        public void ApplyLabelThemes()
        {
            if (!GameObject)
                return;

            for (int j = 0; j < GameObject.transform.childCount; j++)
            {
                var label = GameObject.transform.GetChild(j);
                if (label.name != "label")
                    continue;

                for (int k = 0; k < label.childCount; k++)
                    EditorThemeManager.ApplyLightText(label.GetChild(k).GetComponent<Text>());
            }
        }

        public EventKeyframe GetCurrentKeyframe() => isObjectKeyframe ? EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().events[KeyframeTimeline.CurrentTimeline.currentKeyframeType][KeyframeTimeline.CurrentTimeline.currentKeyframeIndex] : RTEventEditor.inst.CurrentSelectedKeyframe;

        public IEnumerable<TimelineKeyframe> GetSelectedKeyframes() => isObjectKeyframe ?
            EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes?.Where(x => x.Selected && x.Type == type) :
            EditorTimeline.inst.timelineKeyframes.Where(x => x.Selected && x.Type == type);

        /// <summary>
        /// Gets a keyframe coordinate of the currently selected keyframe.
        /// </summary>
        /// <returns>Returns a <see cref="KeyframeCoord"/>.</returns>
        public KeyframeCoord GetSelectionCoord() => isObjectKeyframe ? KeyframeTimeline.CurrentTimeline.GetSelectionCoord() : RTEventEditor.inst.GetSelectionCoord();

        public void SetKeyframeValue(int index, string input)
        {
            if (RTMath.TryParse(input, 0f, out float value))
                SetKeyframeValue(index, value);
        }

        public void SetKeyframeValue(int type, int index, string input)
        {
            if (RTMath.TryParse(input, 0f, out float value))
                SetKeyframeValue(type, index, value);
        }

        public void SetKeyframeValue(int index, float value) => SetKeyframeValue(isObjectKeyframe ? KeyframeTimeline.CurrentTimeline.currentKeyframeType : EventEditor.inst.currentEventType, index, value);

        public void SetKeyframeValue(int type, int index, float value)
        {
            foreach (var timelineKeyframe in GetSelectedKeyframes())
                timelineKeyframe.eventKeyframe.values[index] = value;

            if (isObjectKeyframe)
                RTLevel.Current?.UpdateObject(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), ObjectContext.KEYFRAMES);
            else
                RTLevel.Current?.UpdateEvents(type);
        }
        
        public void SetListColor(int value, int index, List<Toggle> toggles, Color defaultColor, Color secondaryDefaultColor, int opacityIndex = -1, int hueIndex = -1, int satIndex = -1, int valIndex = -1)
        {
            int num = 0;
            foreach (var toggle in toggles)
            {
                int tmpIndex = num;
                var color = num < 18 ? CoreHelper.CurrentBeatmapTheme.effectColors[num] : num == 19 ? secondaryDefaultColor : defaultColor;

                if (EditorConfig.Instance.ShowModifiedColors.Value && hueIndex >= 0 && satIndex >= 0 && valIndex >= 0)
                {
                    float hueNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, hueIndex, RTLevel.Current.FixedTime);
                    float satNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, satIndex, RTLevel.Current.FixedTime);
                    float valNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, valIndex, RTLevel.Current.FixedTime);

                    toggle.image.color = RTColors.ChangeColorHSV(color, hueNum, satNum, valNum);
                }
                else
                    toggle.image.color = color;

                toggle.SetIsOnWithoutNotify(num == value);
                toggle.onValueChanged.NewListener(_val =>
                {
                    SetKeyframeValue(index, tmpIndex);
                    SetListColor(tmpIndex, index, toggles, defaultColor, secondaryDefaultColor, opacityIndex, hueIndex, satIndex, valIndex);
                });

                EditorContextMenu.AddContextMenu(toggle.gameObject,
                    new ButtonElement("Reset Value", () =>
                    {
                        int value = (int)EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[index];
                        SetKeyframeValue(index, value);
                        SetListColor(value, index, toggles, defaultColor, secondaryDefaultColor, opacityIndex, hueIndex, satIndex, valIndex);
                    }),
                    ButtonElement.ToggleButton("Show Modified Colors", () => EditorConfig.Instance.ShowModifiedColors.Value, () => EditorConfig.Instance.ShowModifiedColors.Value = !EditorConfig.Instance.ShowModifiedColors.Value),
                    new ButtonElement("Copy Hex Color", () => LSText.CopyToClipboard(RTColors.ColorToHexOptional(color))),
                    new ButtonElement("Copy Modified Hex Color", () =>
                    {
                        float hueNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, hueIndex, RTLevel.Current.FixedTime);
                        float satNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, satIndex, RTLevel.Current.FixedTime);
                        float valNum = RTLevel.Current.eventEngine.Interpolate(EventEditor.inst.currentEventType, valIndex, RTLevel.Current.FixedTime);

                        LSText.CopyToClipboard(RTColors.ColorToHexOptional(RTColors.ChangeColorHSV(color, hueNum, satNum, valNum)));
                    }, shouldGenerate: () => hueIndex >= 0 && satIndex >= 0 && valIndex >= 0));

                num++;
            }
        }

        public void SetToggle(Toggle toggle, int index, int onValue, int offValue)
        {
            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            toggle.SetIsOnWithoutNotify(currentKeyframe.values[index] == onValue);
            toggle.onValueChanged.NewListener(_val => SetKeyframeValue(index, _val ? onValue : offValue));

            EditorContextMenu.AddContextMenu(toggle.gameObject,
                new ButtonElement("Reset Value", () => toggle.isOn = EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[index] == onValue));
        }

        public void SetFloatInputField(InputFieldStorage inputFieldStorage, int index, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true, Action<float> onValueChanged = null)
        {
            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            if (!inputFieldStorage)
                return;

            inputFieldStorage.SetTextWithoutNotify(currentKeyframe.values[index].ToString());
            inputFieldStorage.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                if (min != 0f || max != 0f)
                    num = Mathf.Clamp(num, min, max);

                SetKeyframeValue(index, num);

                onValueChanged?.Invoke(num);
            });
            inputFieldStorage.OnEndEdit.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.time },
                    { "currentValue", currentKeyframe.values[index] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[index], variables, out float calc))
                    inputFieldStorage.Text = calc.ToString();
            });

            if (inputFieldStorage.leftButton && inputFieldStorage.rightButton)
            {
                float num = 1f;

                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(inputFieldStorage.Text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[index] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            inputFieldStorage.Text = result.ToString();
                    }
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(inputFieldStorage.Text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[index] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            inputFieldStorage.Text = result.ToString();
                    }
                });
            }

            TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField, increase, multiply, min, max));

            if (allowNegative)
                TriggerHelper.InversableField(inputFieldStorage.inputField);

            EditorContextMenu.AddContextMenu(inputFieldStorage.inputField.gameObject,
                new ButtonElement("Reset Value", () => inputFieldStorage.Text = EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[index].ToString()));
        }

        public void SetIntInputField(InputFieldStorage inputFieldStorage, int index, int increase = 1, int min = 0, int max = 0, bool allowNegative = true)
        {
            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            if (!inputFieldStorage)
                return;

            inputFieldStorage.SetTextWithoutNotify(currentKeyframe.values[index].ToString());
            inputFieldStorage.OnValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int num))
                    return;

                if (min != 0 && max != 0)
                    num = Mathf.Clamp(num, min, max);

                SetKeyframeValue(index, num);
            });

            if (inputFieldStorage.leftButton && inputFieldStorage.rightButton)
            {
                float num = 1f;

                inputFieldStorage.leftButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(inputFieldStorage.Text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[index] -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            inputFieldStorage.Text = result.ToString();
                    }
                });
                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(inputFieldStorage.Text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[index] += Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            inputFieldStorage.Text = result.ToString();
                    }
                });
            }

            TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField, increase, min, max));

            if (allowNegative)
                TriggerHelper.InversableField(inputFieldStorage.inputField);

            EditorContextMenu.AddContextMenu(inputFieldStorage.inputField.gameObject,
                new ButtonElement("Reset Value", () => inputFieldStorage.Text = EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[index].ToString()));
        }

        public void SetVector2InputField(Vector2InputFieldStorage vector2Field, int xindex, int yindex, float min = 0f, float max = 0f, bool allowNegative = true)
        {
            var currentKeyframe = RTEventEditor.inst.CurrentSelectedKeyframe;

            var posX = vector2Field.x.inputField;
            var posY = vector2Field.y.inputField;

            vector2Field.x.SetTextWithoutNotify(currentKeyframe.values[xindex].ToString());
            vector2Field.x.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                if (min != 0f && max != 0f)
                    num = Mathf.Clamp(num, min, max);

                SetKeyframeValue(xindex, num);
            });
            vector2Field.x.OnEndEdit.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.time },
                    { "currentValueX", currentKeyframe.values[xindex] },
                    { "currentValueY", currentKeyframe.values[yindex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[xindex], variables, out float calc))
                    posX.text = calc.ToString();
            });

            vector2Field.y.SetTextWithoutNotify(currentKeyframe.values[yindex].ToString());
            vector2Field.y.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                if (min != 0f && max != 0f)
                    num = Mathf.Clamp(num, min, max);

                SetKeyframeValue(yindex, num);
            });
            vector2Field.y.OnEndEdit.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", currentKeyframe.time },
                    { "currentValueX", currentKeyframe.values[xindex] },
                    { "currentValueY", currentKeyframe.values[yindex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[yindex], variables, out float calc))
                    posY.text = calc.ToString();
            });

            if (vector2Field.x.leftButton && vector2Field.x.rightButton)
            {
                float num = 1f;
                vector2Field.x.leftButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(posX.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[xindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posX.text = result.ToString();
                    }
                });
                vector2Field.x.rightButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(posX.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[xindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posX.text = result.ToString();
                    }
                });
            }

            if (vector2Field.y.leftButton && vector2Field.y.rightButton)
            {
                float num = 1f;
                vector2Field.y.leftButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(posY.text, out float result))
                    {
                        result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[yindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posY.text = result.ToString();
                    }
                });
                vector2Field.y.rightButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(posY.text, out float result))
                    {
                        result += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        var list = GetSelectedKeyframes();

                        if (list.Count() > 1)
                            foreach (var kf in list)
                                kf.eventKeyframe.values[yindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
                        else
                            posY.text = result.ToString();
                    }
                });
            }

            var clampList = new List<float> { min, max };
            TriggerHelper.AddEventTriggers(posX.gameObject,
                TriggerHelper.ScrollDelta(posX, 0.1f, 10f, min, max, true),
                TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList));
            TriggerHelper.AddEventTriggers(posY.gameObject,
                TriggerHelper.ScrollDelta(posY, 0.1f, 10f, min, max, true),
                TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList));

            if (allowNegative)
            {
                TriggerHelper.InversableField(posX);
                TriggerHelper.InversableField(posY);
            }

            EditorContextMenu.AddContextMenu(posX.gameObject,
                new ButtonElement("Reset Value", () => posX.text = EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[xindex].ToString()));
            EditorContextMenu.AddContextMenu(posY.gameObject,
                new ButtonElement("Reset Value", () => posY.text = EventLibrary.cachedDefaultKeyframes[EventEditor.inst.currentEventType].values[yindex].ToString()));
        }

        public override string ToString() => GameObject?.name;

        #endregion
    }

    public abstract class KeyframeElement : Exists
    {
        public KeyframeElement(string path) => this.path = path;

        #region Values

        /// <summary>
        /// Parent keyframe dialog.
        /// </summary>
        public KeyframeDialog Dialog { get; set; }

        /// <summary>
        /// Game object of the element.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Custom value display.
        /// </summary>
        public CustomValueDisplay Display { get; set; }

        /// <summary>
        /// Path of the element.
        /// </summary>
        public string path;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the keyframe element.
        /// </summary>
        /// <param name="dialog">Parent keyframe dialog.</param>
        /// <param name="parent">Transform to parent the element to.</param>
        /// <param name="name">Name of the element.</param>
        /// <param name="display">Custom UI display.</param>
        public abstract void Init(KeyframeDialog dialog, Transform parent, string name, CustomValueDisplay display);

        /// <summary>
        /// Renders the keyframe element.
        /// </summary>
        /// <param name="type">
        /// Type of the keyframe.
        /// <br>0 - Position</br>
        /// <br>1 - Scale</br>
        /// <br>2 - Rotation</br>
        /// <br>3 - Color</br>
        /// </param>
        /// <param name="valueIndex">
        /// Index of the keyframe value.
        /// <br>0 - X</br>
        /// <br>1 - Y</br>
        /// <br>2 - Z</br>
        /// </param>
        /// <param name="selected">Collection of selected keyframes.</param>
        /// <param name="firstKF">First selected keyframe.</param>
        /// <param name="animatable">Animatable object.</param>
        public abstract void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable);

        /// <summary>
        /// OVerwrites the display element.
        /// </summary>
        /// <param name="animatable">Animatable object.</param>
        public void UpdateDisplay(IAnimatable animatable)
        {
            animatable.EditorData.displays.OverwriteAdd((x, index) => x.path == path, Display);
            RenderDialog(animatable);
        }

        /// <summary>
        /// Renders the parent keyframe dialog.
        /// </summary>
        /// <param name="animatable">Animatable object.</param>
        public void RenderDialog(IAnimatable animatable)
        {
            if (animatable is BeatmapObject beatmapObject)
                ObjectEditor.inst.RenderDialog(beatmapObject);
            if (animatable is PAAnimation animation)
                AnimationEditor.inst.RenderDialog(animation, AnimationEditor.inst.currentOnReturn);
        }

        #endregion
    }

    public class KeyframeInputField : KeyframeElement
    {
        public KeyframeInputField(string path) : base(path) { }

        #region Values

        /// <summary>
        /// Field element.
        /// </summary>
        public InputFieldStorage Field { get; set; }

        /// <summary>
        /// Minimum limit.
        /// </summary>
        public Func<float> getMin;

        /// <summary>
        /// Maximum limit.
        /// </summary>
        public Func<float> getMax;

        /// <summary>
        /// Value to reset to when "Reset Value" button is clicked.
        /// </summary>
        public Func<float> getResetValue;

        /// <summary>
        /// Value to display when multiple keyframes are used.
        /// </summary>
        public Func<string> getMultiValue;

        #endregion

        #region Methods

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomValueDisplay display)
        {
            Display = display;
            Dialog = dialog;
            GameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            Field = GameObject.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(Field.leftGreaterButton);
            CoreHelper.Delete(Field.rightGreaterButton);
            EditorThemeManager.ApplyInputField(Field);

            getMin = () => display.min;
            getMax = () => display.max;
            getResetValue = () => display.resetValue;
            getMultiValue = () => display.multiValue;
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            if (Dialog.EventValueLabels.TryGetAt(valueIndex, out Text label) && label)
                label.text = !string.IsNullOrEmpty(Display.label) ? Display.label : Dialog.originalLabels[valueIndex];

            var isSingle = selected.Count() == 1;

            TriggerHelper.InversableField(Field);

            Field.SetInteractible(Display.interactible);

            if (!Field.eventTrigger)
                Field.eventTrigger = Field.gameObject.AddComponent<EventTrigger>();

            Field.eventTrigger.triggers.Clear();

            EditorContextMenu.AddContextMenu(Field.inputField.gameObject,
                new ButtonElement("Reset Value", () =>
                {
                    Field.Text = getResetValue?.Invoke().ToString() ?? type switch
                    {
                        0 => "0",
                        1 => "1",
                        2 => "0",
                        _ => string.Empty,
                    };
                }),
                new ButtonElement(Display.interactible ? "Lock Value" : "Unlock Value", () =>
                {
                    Display.interactible = !Display.interactible;
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"{(Display.interactible ? "Unlocked" : "Locked")} editor.", 2f, EditorManager.NotificationType.Success);
                }),
                new SpacerElement(),
                new ButtonElement("Set Label", () => RTEditor.inst.ShowNameEditor("Set label", "Label", Display.label, "Set", () =>
                {
                    Display.label = RTEditor.inst.folderCreatorName.text;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Max", () => RTEditor.inst.ShowNameEditor("Set maximum value", "Max", Display.max.ToString(), "Set", () =>
                {
                    if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                        return;

                    Display.max = max;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Min", () => RTEditor.inst.ShowNameEditor("Set minimum value", "Min", Display.min.ToString(), "Set", () =>
                {
                    if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float min))
                        return;

                    Display.min = min;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Reset", () => RTEditor.inst.ShowNameEditor("Set reset value", "Reset", Display.resetValue.ToString(), "Set", () =>
                {
                    if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float resetValue))
                        return;

                    Display.resetValue = resetValue;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Current Reset", () =>
                {
                    if (!float.TryParse(Field.Text, out float resetValue))
                        return;

                    Display.resetValue = resetValue;
                    UpdateDisplay(animatable);
                }),
                new ButtonElement("Set Scroll Amount", () => RTEditor.inst.ShowNameEditor("Set scroll amount", "Amount", Display.scrollAmount.ToString(), "Set", () =>
                {
                    if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                        return;

                    Display.scrollAmount = max;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Scroll Multiply", () => RTEditor.inst.ShowNameEditor("Set scroll multiply", "Multiply", Display.scrollMultiply.ToString(), "Set", () =>
                {
                    if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                        return;

                    Display.scrollMultiply = max;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement($"Override Scroll [{(Display.overrideScroll ? "On" : "Off")}]", () =>
                {
                    Display.overrideScroll = !Display.overrideScroll;
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification(Display.overrideScroll ? "Custom scroll is now used." : "Regular scroll is now used.", 2f, EditorManager.NotificationType.Success);
                }),
                new SpacerElement(),
                new ButtonElement("Change to Dropdown", () =>
                {
                    Display.type = CustomValueDisplay.UIType.Dropdown;
                    UpdateDisplay(animatable);
                }),
                new ButtonElement("Change to Toggle", () =>
                {
                    Display.type = CustomValueDisplay.UIType.Toggle;
                    UpdateDisplay(animatable);
                }),
                new SpacerElement(),
                new ButtonElement("Copy UI", () =>
                {
                    ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                    EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste UI", () =>
                {
                    if (!ObjectEditor.inst.copiedUIDisplay)
                    {
                        EditorManager.inst.DisplayNotification($"No copied UI yet!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    Display.ApplyFrom(ObjectEditor.inst.copiedUIDisplay);
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"Paste UI settings.", 2f, EditorManager.NotificationType.Success);
                }));

            var amount = Display.overrideScroll ? Display.scrollAmount : type switch
            {
                0 => EditorConfig.Instance.ObjectPositionScroll.Value,
                1 => EditorConfig.Instance.ObjectScaleScroll.Value,
                2 => EditorConfig.Instance.ObjectRotationScroll.Value,
                _ => 0.1f,
            };
            var multiply = Display.overrideScroll ? Display.scrollMultiply : type switch
            {
                0 => EditorConfig.Instance.ObjectPositionScrollMultiply.Value,
                1 => EditorConfig.Instance.ObjectScaleScrollMultiply.Value,
                2 => EditorConfig.Instance.ObjectRotationScrollMultiply.Value,
                _ => 0.1f,
            };
            var min = getMin?.Invoke() ?? 0f;
            var max = getMax?.Invoke() ?? 0f;

            var multi = Dialog.EventValueFields.Count > 1 && Dialog.EventValueFields[0] && Dialog.EventValueFields[1];
            Field.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(Field.inputField, amount, multiply, min, max, multi: multi));
            if (multi)
                Field.eventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(Dialog.EventValueFields[0].inputField, Dialog.EventValueFields[1].inputField, amount, multiply));

            Field.inputField.characterValidation = InputField.CharacterValidation.None;
            Field.inputField.contentType = InputField.ContentType.Standard;
            Field.inputField.keyboardType = TouchScreenKeyboardType.Default;

            Field.SetTextWithoutNotify(isSingle ? firstKF.eventKeyframe.values[valueIndex].ToString() : getMultiValue?.Invoke() ?? (type == 2 ? "15" : "1"));
            Field.OnValueChanged.NewListener(_val =>
            {
                if (isSingle && float.TryParse(_val, out float num))
                {
                    num = RTMath.ClampZero(num, min, max);
                    firstKF.eventKeyframe.values[valueIndex] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (animatable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }
            });
            Field.OnEndEdit.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                var variables = new Dictionary<string, float>
                {
                    { "eventTime", firstKF.eventKeyframe.time },
                    { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                    Field.Text = RTMath.ClampZero(calc, min, max).ToString();
            });

            Field.leftButton.gameObject.SetActive(isSingle);
            Field.rightButton.gameObject.SetActive(isSingle);
            if (isSingle)
                TriggerHelper.IncreaseDecreaseButtons(Field, amount, multiply, min, max);

            if (Field.addButton)
            {
                Field.addButton.gameObject.SetActive(!isSingle);
                Field.addButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(Field.Text, out float x))
                    {
                        foreach (var keyframe in selected)
                            keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(keyframe.eventKeyframe.values[valueIndex] + x, min, max);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (animatable is BeatmapObject beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    else
                    {
                        var variables = new Dictionary<string, float>
                        {
                            { "eventTime", firstKF.eventKeyframe.time },
                            { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                        };

                        if (RTMath.TryParse(Field.Text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(keyframe.eventKeyframe.values[valueIndex] + calc, min, max);
                    }
                });
            }
            if (Field.subButton)
            {
                Field.subButton.gameObject.SetActive(!isSingle);
                Field.subButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(Field.Text, out float x))
                    {
                        foreach (var keyframe in selected)
                            keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(keyframe.eventKeyframe.values[valueIndex] - x, min, max);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (animatable is BeatmapObject beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    else
                    {
                        var variables = new Dictionary<string, float>
                        {
                            { "eventTime", firstKF.eventKeyframe.time },
                            { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                        };

                        if (RTMath.TryParse(Field.Text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(keyframe.eventKeyframe.values[valueIndex] - calc, min, max);
                    }
                });
            }
            if (Field.middleButton)
            {
                Field.middleButton.gameObject.SetActive(!isSingle);
                Field.middleButton.onClick.NewListener(() =>
                {
                    if (float.TryParse(Field.Text, out float x))
                    {
                        foreach (var keyframe in selected)
                            keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(x, min, max);

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (animatable is BeatmapObject beatmapObject)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                    }
                    else
                    {
                        var variables = new Dictionary<string, float>
                        {
                            { "eventTime", firstKF.eventKeyframe.time },
                            { "currentValue", firstKF.eventKeyframe.values[valueIndex] }
                        };

                        if (RTMath.TryParse(Field.Text, firstKF.eventKeyframe.values[valueIndex], variables, out float calc))
                            foreach (var keyframe in selected)
                                keyframe.eventKeyframe.values[valueIndex] = RTMath.ClampZero(calc, min, max);
                    }
                });
            }

            Field.GetComponent<HorizontalLayoutGroup>().spacing = isSingle ? 8f : 0f;
        }

        #endregion
    }

    public class KeyframeDropdown : KeyframeElement
    {
        public KeyframeDropdown(string path) : base(path) { }

        #region Values

        /// <summary>
        /// Dropdown element.
        /// </summary>
        public Dropdown Dropdown { get; set; }

        /// <summary>
        /// Button that applies the selected value to all selected keyframes.
        /// </summary>
        public Button Apply { get; set; }

        /// <summary>
        /// List of options for the dropdown.
        /// </summary>
        public Func<List<Dropdown.OptionData>> getOptions;

        /// <summary>
        /// Value selector.
        /// </summary>
        public Func<int, float> getValue;

        /// <summary>
        /// Value to display when multiple keyframes are used.
        /// </summary>
        public Func<string> getMultiValue;

        #endregion

        #region Methods

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomValueDisplay display)
        {
            Display = display;
            Dialog = dialog;
            GameObject = Creator.NewUIObject(name, parent);

            var layout = GameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;

            var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(GameObject.transform, "dropdown");
            Dropdown = dropdown.GetComponent<Dropdown>();
            EditorThemeManager.ApplyDropdown(Dropdown);

            var dropdownLayoutElement = dropdown.GetOrAddComponent<LayoutElement>();
            dropdownLayoutElement.minWidth = -1f;
            dropdownLayoutElement.preferredWidth = 10000f;

            var apply = EditorPrefabHolder.Instance.Function1Button.Duplicate(GameObject.transform, "apply");
            var applyStorage = apply.GetComponent<FunctionButtonStorage>();
            Apply = applyStorage.button;
            applyStorage.Text = "Apply";
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);

            getOptions = () => display.options.IsEmpty() ? new List<Dropdown.OptionData>() : display.options.Select(x => new Dropdown.OptionData(x.name)).ToList();
            getValue = _val => display.options.IsEmpty() ? 0f : display.options.GetAt(_val).value;
            getMultiValue = () => display.multiValue;
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            if (Dialog.EventValueLabels.TryGetAt(valueIndex, out Text label) && label)
                label.text = !string.IsNullOrEmpty(Display.label) ? Display.label : Dialog.originalLabels[valueIndex];

            var isSingle = selected.Count() == 1;

            Dropdown.interactable = Display.interactible;

            EditorContextMenu.AddContextMenu(Dropdown.gameObject,
                new ButtonElement("Reset Value", () => Dropdown.value = 0),
                new ButtonElement(Display.interactible ? "Lock Value" : "Unlock Value", () =>
                {
                    Display.interactible = !Display.interactible;
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"{(Display.interactible ? "Unlocked" : "Locked")} editor.", 2f, EditorManager.NotificationType.Success);
                }),
                new SpacerElement(),
                new ButtonElement("Set Label", () => RTEditor.inst.ShowNameEditor("Set label", "Label", Display.label, "Set", () =>
                {
                    Display.label = RTEditor.inst.folderCreatorName.text;
                    UpdateDisplay(animatable);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Add Entry", () => RTEditor.inst.ShowNameEditor("Add Dropdown Option", "Entry Name", "Value", "Next", () =>
                {
                    var name = RTEditor.inst.folderCreatorName.text;

                    RTEditor.inst.ShowNameEditor("Add Dropdown Option", "Entry Value", "0", "Add", () =>
                    {
                        if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float value))
                            return;

                        Display.options.Add(new CustomValueDisplay.Option(name, value));
                        UpdateDisplay(animatable);
                        RTEditor.inst.HideNameEditor();
                    });
                })),
                new ButtonElement("Remove Entry", () =>
                {
                    if (Display.options.IsEmpty())
                        return;

                    Display.options.RemoveAt(Display.options.Count - 1);
                    UpdateDisplay(animatable);
                }),
                new ButtonElement("Clear Entries", () =>
                {
                    Display.options.Clear();
                    UpdateDisplay(animatable);
                }),
                new SpacerElement(),
                new ButtonElement("Change to Input Field", () =>
                {
                    Display.type = CustomValueDisplay.UIType.InputField;
                    UpdateDisplay(animatable);
                }),
                new ButtonElement("Change to Toggle", () =>
                {
                    Display.type = CustomValueDisplay.UIType.Toggle;
                    UpdateDisplay(animatable);
                }),
                new SpacerElement(),
                new ButtonElement("Copy UI", () =>
                {
                    ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                    EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste UI", () =>
                {
                    if (!ObjectEditor.inst.copiedUIDisplay)
                    {
                        EditorManager.inst.DisplayNotification($"No copied UI yet!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    Display.ApplyFrom(ObjectEditor.inst.copiedUIDisplay);
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"Paste UI settings.", 2f, EditorManager.NotificationType.Success);
                }));

            if (getOptions != null)
                Dropdown.options = getOptions.Invoke();
            Dropdown.SetValueWithoutNotify(isSingle ? (int)firstKF.eventKeyframe.values[valueIndex] : Parser.TryParse(getMultiValue?.Invoke(), 0));
            Dropdown.onValueChanged.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                firstKF.eventKeyframe.values[valueIndex] = getValue?.Invoke(_val) ?? 0f;
                if (animatable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });

            TriggerHelper.AddEventTriggers(Dropdown.gameObject, TriggerHelper.ScrollDelta(Dropdown));

            Apply.gameObject.SetActive(!isSingle);
            Apply.onClick.NewListener(() =>
            {
                if (isSingle)
                    return;

                var value = getValue?.Invoke(Dropdown.value) ?? 0f;
                foreach (var keyframe in selected)
                    keyframe.eventKeyframe.values[valueIndex] = value;
                if (animatable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });
        }

        #endregion
    }

    public class KeyframeToggle : KeyframeElement
    {
        public KeyframeToggle(string path) : base(path) { }

        #region Values

        /// <summary>
        /// Toggle element.
        /// </summary>
        public Toggle Toggle { get; set; }

        /// <summary>
        /// Button that applies the selected value to all selected keyframes.
        /// </summary>
        public Button Apply { get; set; }

        /// <summary>
        /// Value to set when toggle is on.
        /// </summary>
        public Func<float> getOnValue;

        /// <summary>
        /// Value to set when toggle is off.
        /// </summary>
        public Func<float> getOffValue;

        /// <summary>
        /// Value to display when multiple keyframes are used.
        /// </summary>
        public Func<string> getMultiValue;

        #endregion

        #region Methods

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomValueDisplay display)
        {
            Display = display;
            Dialog = dialog;
            GameObject = Creator.NewUIObject(name, parent);

            var layout = GameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;

            var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(GameObject.transform, "toggle");
            var toggleStorage = toggle.GetComponent<ToggleButtonStorage>();
            Toggle = toggleStorage.toggle;
            toggleStorage.label.text = !string.IsNullOrEmpty(display.toggleLabel) ? display.toggleLabel : "On";
            EditorThemeManager.ApplyToggle(toggleStorage.toggle, graphic: toggleStorage.label);

            var toggleLayoutElement = toggle.GetOrAddComponent<LayoutElement>();
            toggleLayoutElement.preferredWidth = 10000f;

            var apply = EditorPrefabHolder.Instance.Function1Button.Duplicate(GameObject.transform, "apply");
            apply.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var applyStorage = apply.GetComponent<FunctionButtonStorage>();
            Apply = applyStorage.button;
            applyStorage.Text = "Apply";
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);

            getOnValue = () => display.onValue;
            getOffValue = () => display.offValue;
            getMultiValue = () => display.multiValue;
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            if (Dialog.EventValueLabels.TryGetAt(valueIndex, out Text label) && label)
                label.text = !string.IsNullOrEmpty(Display.label) ? Display.label : Dialog.originalLabels[valueIndex];

            var isSingle = selected.Count() == 1;
            var offValue = getOffValue?.Invoke() ?? 0f;
            var onValue = getOnValue?.Invoke() ?? 1f;

            Toggle.interactable = Display.interactible;

            EditorContextMenu.AddContextMenu(Toggle.gameObject,
                new ButtonElement("Reset Value", () =>
                {
                    Toggle.isOn = false;
                }),
                new ButtonElement(Display.interactible ? "Lock Value" : "Unlock Value", () =>
                {
                    Display.interactible = !Display.interactible;
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"{(Display.interactible ? "Unlocked" : "Locked")} editor.", 2f, EditorManager.NotificationType.Success);
                }),
                new SpacerElement(),
                new ButtonElement("Set Label", () =>
                {
                    RTEditor.inst.ShowNameEditor("Set label", "Label", Display.label, "Set", () =>
                    {
                        Display.label = RTEditor.inst.folderCreatorName.text;
                        UpdateDisplay(animatable);
                        RTEditor.inst.HideNameEditor();
                    });
                }),
                new ButtonElement("Set On Value", () =>
                {
                    RTEditor.inst.ShowNameEditor("Set on value", "On", Display.onValue.ToString(), "Set", () =>
                    {
                        if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                            return;

                        Display.onValue = max;
                        UpdateDisplay(animatable);
                        RTEditor.inst.HideNameEditor();
                    });
                }),
                new ButtonElement("Set Off Value", () =>
                {
                    RTEditor.inst.ShowNameEditor("Set off value", "Off", Display.offValue.ToString(), "Set", () =>
                    {
                        if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                            return;

                        Display.offValue = max;
                        UpdateDisplay(animatable);
                        RTEditor.inst.HideNameEditor();
                    });
                }),
                new ButtonElement("Set Toggle Label", () =>
                {
                    RTEditor.inst.ShowNameEditor("Set label", "Label", !string.IsNullOrEmpty(Display.toggleLabel) ? Display.toggleLabel : "On", "Set", () =>
                    {
                        Display.toggleLabel = RTEditor.inst.folderCreatorName.text;
                        UpdateDisplay(animatable);
                        RTEditor.inst.HideNameEditor();
                    });
                }),
                new SpacerElement(),
                new ButtonElement("Change to Input Field", () =>
                {
                    Display.type = CustomValueDisplay.UIType.InputField;
                    UpdateDisplay(animatable);
                }),
                new ButtonElement("Change to Dropdown", () =>
                {
                    Display.type = CustomValueDisplay.UIType.Dropdown;
                    UpdateDisplay(animatable);
                }),
                new SpacerElement(),
                new ButtonElement("Copy UI", () =>
                {
                    ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                    EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste UI", () =>
                {
                    if (!ObjectEditor.inst.copiedUIDisplay)
                    {
                        EditorManager.inst.DisplayNotification($"No copied UI yet!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    Display.ApplyFrom(ObjectEditor.inst.copiedUIDisplay);
                    UpdateDisplay(animatable);
                    EditorManager.inst.DisplayNotification($"Paste UI settings.", 2f, EditorManager.NotificationType.Success);
                }));

            Toggle.SetIsOnWithoutNotify(isSingle ? firstKF.eventKeyframe.values[valueIndex] == onValue : Parser.TryParse(getMultiValue?.Invoke(), false));
            Toggle.onValueChanged.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                firstKF.eventKeyframe.values[valueIndex] = _val ? onValue : offValue;
                if (animatable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });

            Apply.gameObject.SetActive(!isSingle);
            Apply.onClick.NewListener(() =>
            {
                if (isSingle)
                    return;

                var value = Toggle.isOn ? onValue : offValue;
                foreach (var keyframe in selected)
                    keyframe.eventKeyframe.values[valueIndex] = value;
                if (animatable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });
        }

        #endregion
    }
}
