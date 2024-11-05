using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Editor
{
    /// <summary>
    /// Class for feature documentation. This will be used to teach users how the editor works, including mod features.
    /// </summary>
    public class EditorDocument
    {
        public EditorDocument(GameObject gameObject, string name, string description)
        {
            PopupButton = gameObject;
            Name = name;
            Description = description;
        }

        public GameObject PopupButton { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<Element> elements = new List<Element>();

        public enum SupportType
        {
            VANILLA,
            PATCHED,
            MODDED,
            ALPHA,
            LEGACY
        }

        public static EditorDocument Parse(JSONNode jn)
        {
            var document = new EditorDocument(null, jn["name"], jn["desc"]);

            for (int i = 0; i < jn["elements"].Count; i++)
                document.elements.Add(Element.Parse(jn["elements"][i]));

            return document;
        }

        /// <summary>
        /// Class of a document element. This is used to generate a specific UI element on a scrollable list for the documentation dialog.
        /// </summary>
        public class Element
        {
            public Element(string data, Type type)
            {
                Data = data;
                this.type = type;
            }
            
            public Element(string data, Type type, float height)
            {
                Data = data;
                this.type = type;
                Autosize = false;
                Height = height;
            }
            
            public Element(string data, Type type, Action function)
            {
                Data = data;
                this.type = type;
                Function = function;
            }

            public bool Autosize { get; set; } = true;
            public float Height { get; set; } = 22f;

            public string Data { get; set; }

            public Action Function { get; set; }

            public Type type;

            public enum Type
            {
                Text,
                Image
            }

            public enum FunctionType
            {
                OpenLink,
                OpenFile,
                OpenFolder
            }

            public static Element Parse(JSONNode jn)
            {
                var element = new Element(jn["text"], (Type)jn["type"].AsInt);

                if (jn["height"] != null)
                {
                    element.Height = jn["height"].AsFloat;
                    element.Autosize = false;
                }

                if (jn["func_type"] != null)
                {
                    var data = jn["func_data"];
                    element.Function = (FunctionType)jn["func_type"].AsInt switch
                    {
                        FunctionType.OpenLink => () => Application.OpenURL(data),
                        FunctionType.OpenFile => () => RTFile.OpenInFileBrowser.OpenFile(FontManager.TextTranslater.ReplaceProperties(data)),
                        FunctionType.OpenFolder => () => RTFile.OpenInFileBrowser.OpenFile(FontManager.TextTranslater.ReplaceProperties(data)),
                        _ => () => CoreHelper.Log($"Func: {data}"),
                    };
                }

                return element;
            }
        }
    }
}
