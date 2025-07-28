using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
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
        public List<InputFieldStorage> EventValueFields { get; set; }

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
}
