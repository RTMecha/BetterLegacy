using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling input field elements in the interface. Based on <see cref="MenuImage"/>.
    /// </summary>
    public class MenuInputField : MenuImage
    {
        #region Public Fields

        /// <summary>
        /// The input field component of the element.
        /// </summary>
        public InputField inputField;

        /// <summary>
        /// Text to display when the InputField is empty.
        /// </summary>
        public string placeholder;

        /// <summary>
        /// The text that has been written to the InputField.
        /// </summary>
        public string text;

        /// <summary>
        /// Function to run when the user is typing in the InputField.
        /// </summary>
        public Action<string> valueChangedFunc;

        /// <summary>
        /// Function to run when the user is finished typing.
        /// </summary>
        public Action<string> endEditFunc;

        /// <summary>
        /// Function JSON to parse whenever the element is typed in.
        /// </summary>
        public SimpleJSON.JSONNode valueChangedFuncJSON;

        /// <summary>
        /// Function JSON to parse when the user is finished typing.
        /// </summary>
        public SimpleJSON.JSONNode endEditFuncJSON;

        /// <summary>
        /// Alignment of the main text.
        /// </summary>
        public TextAnchor textAnchor = TextAnchor.MiddleLeft;

        /// <summary>
        /// Alignement of the placeholder text.
        /// </summary>
        public TextAnchor placeholderAnchor = TextAnchor.MiddleLeft;

        /// <summary>
        /// Font size of the main text.
        /// </summary>
        public int textFontSize = 20;

        /// <summary>
        /// Font size of the placeholder text.
        /// </summary>
        public int placeholderFontSize = 20;

        /// <summary>
        /// Triggers for use when scrolling on the InputField.
        /// </summary>
        public UnityEngine.EventSystems.EventTrigger.Entry[] triggers;

        /// <summary>
        /// Theme color slot for the text to use.
        /// </summary>
        public int textColor;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float textHue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float textSat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float textVal;

        /// <summary>
        /// If the current text color should use <see cref="overrideTextColor"/> instead of a color slot based on <see cref="textColor"/>.
        /// </summary>
        public bool useOverrideTextColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overrideTextColor;

        /// <summary>
        /// Theme color slot for the placeholder to use.
        /// </summary>
        public int placeholderColor;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float placeholderHue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float placeholderSat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float placeholderVal;

        /// <summary>
        /// If the current placeholder color should use <see cref="overridePlaceholderColor"/> instead of a color slot based on <see cref="placeholderColor"/>.
        /// </summary>
        public bool useOverridePlaceholderColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overridePlaceholderColor;

        #endregion

        #region Methods

        /// <summary>
        /// Writes the InputField text to a function.
        /// </summary>
        /// <param name="text">Text to pass.</param>
        public void Write(string text)
        {
            valueChangedFunc?.Invoke(text);
            this.text = text;
            if (valueChangedFuncJSON == null)
                return;
            valueChangedFuncJSON["text"] = text;
            ParseFunction(valueChangedFuncJSON);
        }

        /// <summary>
        /// Runs when InputField editing has ended and writes the text to a function.
        /// </summary>
        /// <param name="text">Text to pass.</param>
        public void Finish(string text)
        {
            endEditFunc?.Invoke(text);
            this.text = text;
            if (endEditFuncJSON == null)
                return;

            endEditFuncJSON["text"] = text;
            ParseFunction(endEditFuncJSON);
        }

        #endregion
    }
}
