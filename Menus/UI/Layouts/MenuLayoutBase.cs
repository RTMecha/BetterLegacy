using System;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Menus.UI.Layouts
{
    public abstract class MenuLayoutBase
    {
        public GameObject gameObject;

        public RectTransform content;

        public string name;

        public TextAnchor childAlignment;

        public RectValues rect = RectValues.Default;

        public bool regenerate = true;

        public bool mask;

        public bool scrollable;

        public RectValues contentRect = RectValues.FullAnchored;

        public JSONNode onScrollUpFuncJSON;
        public JSONNode onScrollDownFuncJSON;

        public Action onScrollUpFunc;
        public Action onScrollDownFunc;
    }
}
