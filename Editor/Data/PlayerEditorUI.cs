using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
