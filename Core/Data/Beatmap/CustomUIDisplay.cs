using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Allows specific editor UI to be customized per-object.
    /// </summary>
    public class CustomValueDisplay : PAObject<CustomValueDisplay>
    {
        public CustomValueDisplay() { }

        #region Values

        /// <summary>
        /// If the UI data should serialize to JSON.
        /// </summary>
        public override bool ShouldSerialize =>
            !string.IsNullOrEmpty(path) || type != UIType.InputField || !string.IsNullOrEmpty(label) || multiValue != "1" || !interactible || // base
            overrideScroll && (scrollAmount != 0.1f || scrollMultiply != 10.0f) || min != 0.0f || max != 0.0f || resetValue != 0.0f || // input field
            !options.IsEmpty() || // dropdown
            !string.IsNullOrEmpty(toggleLabel) || offValue != 0.0f || onValue != 1.0f; // toggle

        /// <summary>
        /// Represents the location of the custom UI.
        /// </summary>
        public string path = string.Empty;

        /// <summary>
        /// The type of UI to display.
        /// </summary>
        public UIType type;

        /// <summary>
        /// UI display type.
        /// </summary>
        public enum UIType
        {
            /// <summary>
            /// Displays as an <see cref="UnityEngine.UI.InputField"/>.
            /// </summary>
            InputField,
            /// <summary>
            /// Displays as a <see cref="UnityEngine.UI.Dropdown"/>.
            /// </summary>
            Dropdown,
            /// <summary>
            /// Displays as a <see cref="UnityEngine.UI.Toggle"/>.
            /// </summary>
            Toggle,
        }

        /// <summary>
        /// Label to display above the UI.
        /// </summary>
        public string label = string.Empty;

        /// <summary>
        /// Value to display with multiple selections.
        /// </summary>
        public string multiValue = "1";

        /// <summary>
        /// If the UI is interactible.
        /// </summary>
        public bool interactible = true;

        #region InputField

        /// <summary>
        /// If input field scrolling should use the custom scroll values.
        /// </summary>
        public bool overrideScroll = false;

        /// <summary>
        /// Amount to scroll.
        /// </summary>
        public float scrollAmount = 0.1f;

        /// <summary>
        /// Multiply amount for scroll.
        /// </summary>
        public float scrollMultiply = 10.0f;

        /// <summary>
        /// Minimum amount.
        /// </summary>
        public float min = 0.0f;

        /// <summary>
        /// Maximum amount.
        /// </summary>
        public float max = 0.0f;

        /// <summary>
        /// Reset value.
        /// </summary>
        public float resetValue = 0.0f;

        #endregion

        #region Dropdown

        /// <summary>
        /// List of options to apply to the dropdown.
        /// </summary>
        public List<Option> options = new List<Option>();

        #endregion

        #region Toggle

        /// <summary>
        /// Label to display on the element.
        /// </summary>
        public string toggleLabel = string.Empty;

        /// <summary>
        /// Value to set when toggle is on.
        /// </summary>
        public float offValue = 0.0f;

        /// <summary>
        /// Value to set when the toggle is off.
        /// </summary>
        public float onValue = 1.0f;

        #endregion

        #endregion

        #region Global

        public static CustomValueDisplay DefaultPositionXDisplay => new CustomValueDisplay()
        {
            path = "position/x",
            type = UIType.InputField,
        };
        
        public static CustomValueDisplay DefaultPositionYDisplay => new CustomValueDisplay()
        {
            path = "position/y",
            type = UIType.InputField,
        };
        
        public static CustomValueDisplay DefaultPositionZDisplay => new CustomValueDisplay()
        {
            path = "position/z",
            type = UIType.InputField,
        };
        
        public static CustomValueDisplay DefaultScaleXDisplay => new CustomValueDisplay()
        {
            path = "scale/x",
            type = UIType.InputField,
            resetValue = 1.0f,
        };
        
        public static CustomValueDisplay DefaultScaleYDisplay => new CustomValueDisplay()
        {
            path = "scale/y",
            type = UIType.InputField,
            resetValue = 1.0f,
        };

        public static CustomValueDisplay DefaultRotationDisplay => new CustomValueDisplay()
        {
            path = "rotation/x",
            type = UIType.InputField,
        };

        #endregion

        #region Methods

        public override void CopyData(CustomValueDisplay orig, bool newID = true)
        {
            path = orig.path;
            ApplyFrom(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            path = jn["path"] ?? string.Empty;
            if (jn["type"] != null)
                type = (UIType)jn["type"].AsInt;

            label = jn["l"] ?? string.Empty;
            multiValue = jn["multi_val"] ?? "1";
            interactible = jn["interact"] == null || jn["interact"].AsBool;

            switch (type)
            {
                case UIType.InputField: {
                        if (jn["scr_override"] != null)
                        {
                            overrideScroll = jn["scr_override"].AsBool;
                            if (jn["scr"] != null)
                                scrollAmount = jn["scr"].AsFloat;
                            if (jn["scr_multi"] != null)
                                scrollMultiply = jn["scr_multi"].AsFloat;
                        }
                        if (jn["min"] != null)
                            min = jn["min"].AsFloat;
                        if (jn["max"] != null)
                            max = jn["max"].AsFloat;
                        if (jn["reset_val"] != null)
                            resetValue = jn["reset_val"].AsFloat;

                        break;
                    }
                case UIType.Dropdown: {
                        if (jn["options"] == null)
                            break;

                        options.Clear();
                        for (int i = 0; i < jn["options"].Count; i++)
                            options.Add(Option.Parse(jn["options"][i]));

                        break;
                    }
                case UIType.Toggle: {
                        if (jn["tl"] != null)
                            toggleLabel = jn["tl"];
                        if (jn["label"] != null)
                            toggleLabel = jn["label"];
                        if (jn["off_val"] != null)
                            offValue = jn["off_val"].AsFloat;
                        if (jn["on_val"] != null)
                            onValue = jn["on_val"].AsFloat;

                        break;
                    }
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["path"] = path ?? string.Empty;
            if (type != UIType.InputField)
                jn["type"] = (int)type;

            if (!string.IsNullOrEmpty(label))
                jn["l"] = label;
            if (multiValue != "1")
                jn["multi_val"] = multiValue ?? "1";
            if (!interactible)
                jn["interact"] = interactible;

            switch (type)
            {
                case UIType.InputField: {
                        if (overrideScroll)
                        {
                            jn["scr_override"] = overrideScroll;
                            if (scrollAmount != 0.1f)
                                jn["scr"] = scrollAmount;
                            if (scrollMultiply != 10.0f)
                                jn["scr_multi"] = scrollMultiply;
                        }
                        if (min != 0.0f)
                            jn["min"] = min;
                        if (max != 0.0f)
                            jn["max"] = max;
                        if (resetValue != 0.0f)
                            jn["reset_val"] = resetValue;
                        break;
                    }
                case UIType.Dropdown: {
                        for (int i = 0; i < options.Count; i++)
                            jn["options"][i] = options[i].ToJSON();
                        break;
                    }
                case UIType.Toggle: {
                        if (!string.IsNullOrEmpty(toggleLabel))
                            jn["tl"] = toggleLabel;
                        if (offValue != 0.0f)
                            jn["off_val"] = offValue;
                        if (onValue != 1.0f)
                            jn["on_val"] = onValue;

                        break;
                    }
            }

            return jn;
        }

        /// <summary>
        /// Applies custom UI settings from another display element.
        /// </summary>
        /// <param name="other">Other element to copy from.</param>
        public void ApplyFrom(CustomValueDisplay other)
        {
            type = other.type;

            label = other.label;
            multiValue = other.multiValue;
            interactible = other.interactible;

            overrideScroll = other.overrideScroll;
            scrollAmount = other.scrollAmount;
            scrollMultiply = other.scrollMultiply;
            min = other.min;
            max = other.max;
            resetValue = other.resetValue;

            options = new List<Option>(other.options.Select(x => x.Copy()));

            toggleLabel = other.toggleLabel;
            offValue = other.offValue;
            onValue = other.onValue;
        }

        #endregion

        /// <summary>
        /// Represents an option in a dropdown.
        /// </summary>
        public class Option : PAObject<Option>
        {
            public Option() { }

            public Option(string name, float value)
            {
                this.name = name;
                this.value = value;
            }

            #region Values

            /// <summary>
            /// Name of the dropdown option to display.
            /// </summary>
            public string name = string.Empty;

            /// <summary>
            /// Value to set to the option when selected.
            /// </summary>
            public float value = 0.0f;

            #endregion

            #region Methods

            public override void CopyData(Option orig, bool newID = true)
            {
                name = orig.name;
                value = orig.value;
            }

            public override void ReadJSON(JSONNode jn)
            {
                name = jn["n"] ?? string.Empty;
                value = jn["v"].AsFloat;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["n"] = name ?? string.Empty;
                jn["v"] = value;

                return jn;
            }

            #endregion
        }
    }
}
