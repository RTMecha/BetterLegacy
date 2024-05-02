using System;
using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Editor
{
    /// <summary>
    /// Class for feature documentation. This will be used to teach users how the editor works, including mod features.
    /// </summary>
    public class Document
    {
        public Document(GameObject gameObject, string name, string description)
        {
            PopupButton = gameObject;
            Name = name;
            Description = description;
        }

        public GameObject PopupButton { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<Element> elements = new List<Element>();

        /// <summary>
        /// Class of a document element. This is used to generate a specific UI element on a scrollable list for the documentation dialog.
        /// </summary>
        public class Element
        {
            public Element(object data, Type type)
            {
                Data = data;
                this.type = type;
            }

            public object Data { get; set; }

            public Action Function { get; set; }

            public Type type;

            public enum Type
            {
                Text,
                Image
            }
        }
    }
}
