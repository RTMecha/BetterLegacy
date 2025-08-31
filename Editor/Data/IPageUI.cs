using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data
{
    public interface IPageUI
    {
        public InputFieldStorage PageField { get; set; }

        public int Page { get; set; }

        public int MaxPageCount { get; }
    }
}
