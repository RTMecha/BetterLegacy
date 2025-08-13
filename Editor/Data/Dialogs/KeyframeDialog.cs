﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        #region Properties

        /// <summary>
        /// Name of the keyframes' type.
        /// </summary>
        public string Name => isObjectKeyframe ? KeyframeTimeline.IntToTypeName(type) : RTEventEditor.EventTypes[type];

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
        public List<Toggle> RandomToggles { get; set; }
        public InputField RandomIntervalField { get; set; }
        public Dropdown RandomAxisDropdown { get; set; }

        #endregion

        #endregion

        #region Fields

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

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the keyframe dialog.
        /// </summary>
        public virtual void Init()
        {
            if (!GameObject)
                return;

            Edit = GameObject.transform.Find("edit");
            RTEditor.inst.SetupIndexer(this);

            if (isMulti)
                return;

            try
            {
                CurvesLabel = GameObject.transform.Find("curves_label").gameObject;
                CurvesDropdown = GameObject.transform.Find("curves").GetComponent<Dropdown>();
                EventTimeField = GameObject.transform.Find("time").gameObject.AddComponent<InputFieldStorage>();
                EventTimeField.Assign(EventTimeField.gameObject);

                if (isObjectKeyframe)
                {
                    if (GameObject.transform.TryFind(Name.ToLower(), out Transform valuesTransform))
                    {
                        EventValuesParent = valuesTransform;
                        EventValueFields = new List<InputFieldStorage>();
                        EventValueElements = new List<KeyframeElement>();
                        if (!isObjectKeyframe)
                            for (int i = 0; i < valuesTransform.childCount; i++)
                            {
                                var eventValueField = valuesTransform.GetChild(i).gameObject.GetOrAddComponent<InputFieldStorage>();
                                eventValueField.Assign(eventValueField.gameObject);
                                EditorThemeManager.AddSelectable(eventValueField.middleButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.AddSelectable(eventValueField.subButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.AddSelectable(eventValueField.addButton, ThemeGroup.Function_2, false);
                                EventValueFields.Add(eventValueField);
                            }
                        else if (type != 3)
                            CoreHelper.DestroyChildren(EventValuesParent);
                    }

                    if (GameObject.transform.TryFind($"r_{Name.ToLower()}", out Transform randomValuesTransform))
                    {
                        RandomEventValueLabels = GameObject.transform.Find($"r_{Name.ToLower()}_label").gameObject;
                        RandomEventValueFields = new List<InputFieldStorage>();
                        RandomEventValueParent = randomValuesTransform.gameObject;
                        for (int i = 0; i < randomValuesTransform.childCount; i++)
                        {
                            var eventValueField = randomValuesTransform.GetChild(i).gameObject.GetOrAddComponent<InputFieldStorage>();
                            eventValueField.Assign(eventValueField.gameObject);
                            EditorThemeManager.AddSelectable(eventValueField.middleButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(eventValueField.subButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(eventValueField.addButton, ThemeGroup.Function_2, false);
                            RandomEventValueFields.Add(eventValueField);
                        }
                    }
                }

                if (GameObject.transform.TryFind("relative", out Transform relativeTransform))
                    RelativeToggle = relativeTransform.GetComponent<ToggleButtonStorage>();
                
                if (GameObject.transform.TryFind("flee", out Transform fleeTransform))
                    FleeToggle = fleeTransform.GetComponent<ToggleButtonStorage>();

                if (GameObject.transform.TryFind("random", out Transform randomTransform))
                {
                    RandomToggles = new List<Toggle>();
                    for (int i = 0; i < randomTransform.childCount - 2; i++)
                    {
                        var toggle = randomTransform.GetChild(i).GetComponent<Toggle>();
                        RandomToggles.Add(toggle);

                        if (!toggle.GetComponent<HoverUI>())
                        {
                            var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                            hoverUI.animatePos = false;
                            hoverUI.animateSca = true;
                            hoverUI.size = 1.1f;
                        }
                    }

                    RandomIntervalField = randomTransform.Find("interval-input").GetComponent<InputField>();

                    if (GameObject.transform.TryFind("r_axis", out Transform rAxisTransform))
                        RandomAxisDropdown = rAxisTransform.GetComponent<Dropdown>();
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to set main: {ex}");
            }
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

        public void InitCustomUI(params CustomUIDisplay[] displays)
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
                    CustomUIDisplay.UIType.InputField => new KeyframeInputField(display.path),
                    CustomUIDisplay.UIType.Dropdown => new KeyframeDropdown(display.path),
                    CustomUIDisplay.UIType.Toggle => new KeyframeToggle(display.path),
                    _ => null,
                };
                element?.Init(this, EventValuesParent, name, display);
                EventValueElements.Add(element);
                EventValueFields.Add(element is KeyframeInputField inputField ? inputField.Field : null);
            }
        }

        public void InitCustomUI(CustomUIDisplay display)
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
                CustomUIDisplay.UIType.InputField => new KeyframeInputField(display.path),
                CustomUIDisplay.UIType.Dropdown => new KeyframeDropdown(display.path),
                CustomUIDisplay.UIType.Toggle => new KeyframeToggle(display.path),
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

        public override string ToString() => GameObject?.name;

        #endregion
    }

    public abstract class KeyframeElement : Exists
    {
        public KeyframeElement(string path) => this.path = path;

        public KeyframeDialog Dialog { get; set; }

        public GameObject GameObject { get; set; }

        public CustomUIDisplay Display { get; set; }

        public string path;

        public abstract void Init(KeyframeDialog dialog, Transform parent, string name, CustomUIDisplay display);

        public abstract void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable);

        public void UpdateDisplay(IAnimatable animatable)
        {
            animatable.EditorData.displays.OverwriteAdd((x, index) => x.path == path, Display);
            RenderDialog(animatable);
        }

        public void RenderDialog(IAnimatable animatable)
        {
            if (animatable is BeatmapObject beatmapObject)
                ObjectEditor.inst.RenderDialog(beatmapObject);
            if (animatable is PAAnimation animation)
                AnimationEditor.inst.RenderDialog(animation, AnimationEditor.inst.currentOnReturn);
        }
    }

    public class KeyframeInputField : KeyframeElement
    {
        public KeyframeInputField(string path) : base(path) { }

        public InputFieldStorage Field { get; set; }

        public Func<float> getMin;
        public Func<float> getMax;
        public Func<float> getResetValue;
        public Func<string> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomUIDisplay display)
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
            var isSingle = selected.Count() == 1;

            TriggerHelper.InversableField(Field);

            if (!Field.eventTrigger)
                Field.eventTrigger = Field.gameObject.AddComponent<EventTrigger>();

            Field.eventTrigger.triggers.Clear();

            var contextMenu = Field.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Reset Value", () =>
                    {
                        Field.Text = getResetValue?.Invoke().ToString() ?? type switch
                        {
                            0 => "0",
                            1 => "1",
                            2 => "0",
                            _ => string.Empty,
                        };
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Set Max", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Set maximum value", "Max", Display.max.ToString(), "Set", () =>
                        {
                            if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float max))
                                return;

                            Display.max = max;
                            UpdateDisplay(animatable);
                            RTEditor.inst.HideNameEditor();
                        });
                    }),
                    new ButtonFunction("Set Min", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Set minimum value", "Min", Display.min.ToString(), "Set", () =>
                        {
                            if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float min))
                                return;

                            Display.min = min;
                            UpdateDisplay(animatable);
                            RTEditor.inst.HideNameEditor();
                        });
                    }),
                    new ButtonFunction("Set Reset Value", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Set reset value", "Reset", Display.resetValue.ToString(), "Set", () =>
                        {
                            if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float resetValue))
                                return;

                            Display.resetValue = resetValue;
                            UpdateDisplay(animatable);
                            RTEditor.inst.HideNameEditor();
                        });
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Change to Dropdown", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.Dropdown;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction("Change to Toggle", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.Toggle;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy UI", () =>
                    {
                        ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                        EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste UI", () =>
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
            };

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
    }

    public class KeyframeDropdown : KeyframeElement
    {
        public KeyframeDropdown(string path) : base(path) { }

        public Dropdown Dropdown { get; set; }

        public Button Apply { get; set; }

        public Func<List<Dropdown.OptionData>> getOptions;
        public Func<int, float> getValue;
        public Func<string> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomUIDisplay display)
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
            applyStorage.label.text = "Apply";
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);

            getOptions = () => display.options.IsEmpty() ? new List<Dropdown.OptionData>() : display.options.Select(x => new Dropdown.OptionData(x.name)).ToList();
            getValue = _val => display.options.IsEmpty() ? 0f : display.options.GetAt(_val).value;
            getMultiValue = () => display.multiValue;
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            var isSingle = selected.Count() == 1;

            var contextMenu = Dropdown.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Reset Value", () =>
                    {
                        Dropdown.value = 0;
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Add Entry", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Add Dropdown Option", "Entry Name", "Value", "Next", () =>
                        {
                            var name = RTEditor.inst.folderCreatorName.text;

                            RTEditor.inst.ShowNameEditor("Add Dropdown Option", "Entry Value", "0", "Add", () =>
                            {
                                if (!float.TryParse(RTEditor.inst.folderCreatorName.text, out float value))
                                    return;

                                Display.options.Add(new CustomUIDisplay.Option(name, value));
                                UpdateDisplay(animatable);
                                RTEditor.inst.HideNameEditor();
                            });
                        });
                    }),
                    new ButtonFunction("Remove Entry", () =>
                    {
                        if (Display.options.IsEmpty())
                            return;

                        Display.options.RemoveAt(Display.options.Count - 1);
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction("Clear Entries", () =>
                    {
                        Display.options.Clear();
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Change to Input Field", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.InputField;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction("Change to Toggle", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.Toggle;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy UI", () =>
                    {
                        ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                        EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste UI", () =>
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
            };

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
    }

    public class KeyframeToggle : KeyframeElement
    {
        public KeyframeToggle(string path) : base(path) { }

        public Toggle Toggle { get; set; }

        public Button Apply { get; set; }

        public Func<float> getOnValue;
        public Func<float> getOffValue;
        public Func<string> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name, CustomUIDisplay display)
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
            toggleStorage.label.text = !string.IsNullOrEmpty(display.label) ? display.label : "On";
            EditorThemeManager.ApplyToggle(toggleStorage.toggle, graphic: toggleStorage.label);

            var toggleLayoutElement = toggle.GetOrAddComponent<LayoutElement>();
            toggleLayoutElement.preferredWidth = 10000f;

            var apply = EditorPrefabHolder.Instance.Function1Button.Duplicate(GameObject.transform, "apply");
            apply.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var applyStorage = apply.GetComponent<FunctionButtonStorage>();
            Apply = applyStorage.button;
            applyStorage.label.text = "Apply";
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);

            getOnValue = () => display.onValue;
            getOffValue = () => display.offValue;
            getMultiValue = () => display.multiValue;
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, IAnimatable animatable)
        {
            var isSingle = selected.Count() == 1;
            var offValue = getOffValue?.Invoke() ?? 0f;
            var onValue = getOnValue?.Invoke() ?? 1f;

            var contextMenu = Toggle.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Reset Value", () =>
                    {
                        Toggle.isOn = false;
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Set On Value", () =>
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
                    new ButtonFunction("Set Off Value", () =>
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
                    new ButtonFunction("Set Label", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Set label", "Label", !string.IsNullOrEmpty(Display.label) ? Display.label : "On", "Set", () =>
                        {
                            Display.label = RTEditor.inst.folderCreatorName.text;
                            UpdateDisplay(animatable);
                            RTEditor.inst.HideNameEditor();
                        });
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Change to Input Field", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.InputField;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction("Change to Dropdown", () =>
                    {
                        Display.type = CustomUIDisplay.UIType.Dropdown;
                        UpdateDisplay(animatable);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy UI", () =>
                    {
                        ObjectEditor.inst.copiedUIDisplay = Display.Copy();
                        EditorManager.inst.DisplayNotification($"Copied UI settings.", 2f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste UI", () =>
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
            };

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
    }
}
