using BetterLegacy.Core.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class KeyframeDialog
    {
        public GameObject GameObject { get; set; }
        public Dropdown CurvesDropdown { get; set; }
        public InputFieldStorage EventTimeField { get; set; }

        public FunctionButtonStorage CopyButton { get; set; }
        public FunctionButtonStorage PasteButton { get; set; }
        public DeleteButtonStorage DeleteButton { get; set; }

        public List<InputFieldStorage> EventValueFields { get; set; } = new List<InputFieldStorage>();
    }

}
