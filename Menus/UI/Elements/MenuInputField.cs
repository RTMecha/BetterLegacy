using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.UI;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuInputField : MenuImage
    {
        /// <summary>
        /// The input field component of the element.
        /// </summary>
        public InputField inputField;

        public string defaultText;

        public Action<string> valueChangedFunc;
        public Action<string> endEditFunc;
        public SimpleJSON.JSONNode valueChangedFuncJSON;
        public SimpleJSON.JSONNode endEditFuncJSON;

        public void Write(string text)
        {
            valueChangedFunc?.Invoke(text);
            if (valueChangedFuncJSON == null)
                return;

            valueChangedFuncJSON["text"] = text;
            ParseFunction(valueChangedFuncJSON);
        }

        public void Finish(string text)
        {
            endEditFunc?.Invoke(text);
            if (endEditFuncJSON == null)
                return;

            endEditFuncJSON["text"] = text;
            ParseFunction(endEditFuncJSON);
        }
    }
}
