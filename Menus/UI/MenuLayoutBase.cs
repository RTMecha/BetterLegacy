using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace BetterLegacy.Menus.UI
{
    public abstract class MenuLayoutBase
    {
        public GameObject gameObject;

        public string name;

        public TextAnchor childAlignment;

        public JSONNode rectJSON;
    }
}
