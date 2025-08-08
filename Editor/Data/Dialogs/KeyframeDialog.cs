using System;
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
        public string Name => isObjectKeyframe ? ObjectEditor.IntToTypeName(type) : RTEventEditor.EventTypes[type];

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
                        for (int i = 0; i < valuesTransform.childCount; i++)
                        {
                            var eventValueField = valuesTransform.GetChild(i).gameObject.GetOrAddComponent<InputFieldStorage>();
                            eventValueField.Assign(eventValueField.gameObject);
                            EditorThemeManager.AddSelectable(eventValueField.middleButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(eventValueField.subButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(eventValueField.addButton, ThemeGroup.Function_2, false);
                            EventValueFields.Add(eventValueField);
                        }
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

        public override string ToString() => GameObject?.name;

        #endregion
    }

    public abstract class KeyframeElement : Exists
    {
        public KeyframeDialog Dialog { get; set; }

        public abstract void Init(KeyframeDialog dialog, Transform parent, string name);

        public abstract void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject);
    }

    public class KeyframeInputField : KeyframeElement
    {
        public InputFieldStorage Field { get; set; }

        public Func<float> getScrollAmount;
        public Func<float> getScrollMultiply;
        public Func<float> getMin;
        public Func<float> getMax;
        public Func<float> getResetValue;
        public Func<string> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name)
        {
            Dialog = dialog;
            var field = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            Field = field.GetComponent<InputFieldStorage>();
            EditorThemeManager.ApplyInputField(Field);
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
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
                    }));
            };

            var amount = getScrollAmount?.Invoke() ?? type switch
            {
                0 => EditorConfig.Instance.ObjectPositionScroll.Value,
                1 => EditorConfig.Instance.ObjectScaleScroll.Value,
                2 => EditorConfig.Instance.ObjectRotationScroll.Value,
                _ => 0.1f,
            };
            var multiply = getScrollMultiply?.Invoke() ?? type switch
            {
                0 => EditorConfig.Instance.ObjectPositionScrollMultiply.Value,
                1 => EditorConfig.Instance.ObjectScaleScrollMultiply.Value,
                2 => EditorConfig.Instance.ObjectRotationScrollMultiply.Value,
                _ => 0.1f,
            };
            var min = getMin?.Invoke() ?? 0f;
            var max = getMax?.Invoke() ?? 0f;

            var multi = Dialog.EventValueFields.Count > 1 && Dialog.EventValueFields[1];
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
                TriggerHelper.IncreaseDecreaseButtons(Field, amount, multiply);

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
        public Dropdown Dropdown { get; set; }

        public Button Apply { get; set; }

        public Func<List<Dropdown.OptionData>> getOptions;
        public Func<int> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name)
        {
            Dialog = dialog;
            var subParent = Creator.NewUIObject(name, parent);

            var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(subParent.transform, "dropdown");
            Dropdown = dropdown.GetComponent<Dropdown>();
            EditorThemeManager.ApplyDropdown(Dropdown);

            var apply = EditorPrefabHolder.Instance.Function1Button.Duplicate(subParent.transform, "apply");
            var applyStorage = apply.GetComponent<FunctionButtonStorage>();
            Apply = applyStorage.button;
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            var isSingle = selected.Count() == 1;

            if (getOptions != null)
                Dropdown.options = getOptions.Invoke();
            Dropdown.SetValueWithoutNotify(isSingle ? (int)firstKF.eventKeyframe.values[valueIndex] : getMultiValue?.Invoke() ?? 0);
            Dropdown.onValueChanged.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                firstKF.eventKeyframe.values[valueIndex] = _val;
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });

            TriggerHelper.AddEventTriggers(Dropdown.gameObject, TriggerHelper.ScrollDelta(Dropdown));

            Apply.gameObject.SetActive(!isSingle);
            Apply.onClick.NewListener(() =>
            {
                if (isSingle)
                    return;

                foreach (var keyframe in selected)
                    keyframe.eventKeyframe.values[valueIndex] = Dropdown.value;
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });
        }
    }

    public class KeyframeToggle : KeyframeElement
    {
        public Toggle Toggle { get; set; }

        public Button Apply { get; set; }

        public Func<float> getOnValue;
        public Func<float> getOffValue;
        public Func<bool> getMultiValue;

        public override void Init(KeyframeDialog dialog, Transform parent, string name)
        {
            Dialog = dialog;
            var subParent = Creator.NewUIObject(name, parent);

            var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(subParent.transform, "toggle");
            var toggleStorage = toggle.GetComponent<ToggleButtonStorage>();
            Toggle = toggleStorage.toggle;
            EditorThemeManager.ApplyToggle(toggleStorage.toggle, graphic: toggleStorage.label);

            var apply = EditorPrefabHolder.Instance.Function1Button.Duplicate(subParent.transform, "apply");
            var applyStorage = apply.GetComponent<FunctionButtonStorage>();
            Apply = applyStorage.button;
            EditorThemeManager.ApplyGraphic(applyStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(applyStorage.label, ThemeGroup.Function_1_Text);
        }

        public override void Render(int type, int valueIndex, IEnumerable<TimelineKeyframe> selected, TimelineKeyframe firstKF, BeatmapObject beatmapObject)
        {
            var isSingle = selected.Count() == 1;
            var offValue = getOffValue?.Invoke() ?? 0f;
            var onValue = getOnValue?.Invoke() ?? 1f;

            Toggle.SetIsOnWithoutNotify(isSingle ? firstKF.eventKeyframe.values[valueIndex] == onValue : getMultiValue?.Invoke() ?? false);
            Toggle.onValueChanged.NewListener(_val =>
            {
                if (!isSingle)
                    return;

                firstKF.eventKeyframe.values[valueIndex] = _val ? onValue : offValue;
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
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            });
        }
    }
}
