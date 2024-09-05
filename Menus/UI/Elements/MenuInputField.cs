using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuInputField : MenuImage
    {
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
        public SimpleJSON.JSONNode valueChangedFuncJSON;
        public SimpleJSON.JSONNode endEditFuncJSON;

        public TextAnchor textAnchor = TextAnchor.MiddleLeft;
        public TextAnchor placeholderAnchor = TextAnchor.MiddleLeft;
        public int textFontSize = 20;
        public int placeholderFontSize = 20;

        public UnityEngine.EventSystems.EventTrigger.Entry[] triggers;

        public bool useOverrideTextColor;
        public Color overrideTextColor;
        public int textColor;
        public float textHue;
        public float textSat;
        public float textVal;

        public bool useOverridePlaceholderColor;
        public Color overridePlaceholderColor;
        public int placeholderColor;
        public float placeholderHue;
        public float placeholderSat;
        public float placeholderVal;

        public void Write(string text)
        {
            valueChangedFunc?.Invoke(text);
            this.text = text;
            if (valueChangedFuncJSON == null)
                return;
            valueChangedFuncJSON["text"] = text;
            ParseFunction(valueChangedFuncJSON);
        }

        public void Finish(string text)
        {
            endEditFunc?.Invoke(text);
            this.text = text;
            if (endEditFuncJSON == null)
                return;

            endEditFuncJSON["text"] = text;
            ParseFunction(endEditFuncJSON);
        }
    }
}
