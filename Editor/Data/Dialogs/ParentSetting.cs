using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ParentSetting
    {
        public Transform row;
        public Text label;
        public Toggle activeToggle;
        public InputField offsetField;
        public Toggle additiveToggle;
        public InputField parallaxField;
    }
}
