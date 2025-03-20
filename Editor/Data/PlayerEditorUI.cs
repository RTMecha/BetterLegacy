using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class PlayerEditorUI
    {
        public string Name { get; set; }
        public GameObject GameObject { get; set; }
        public PlayerEditor.Tab Tab { get; set; }
        public ValueType ValueType { get; set; }
        public int Index { get; set; }

        public object Reference { get; set; }

        public bool updatedShapes = false;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

        public override string ToString() => $"{Tab} - {Name}";
    }
}
