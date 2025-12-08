using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a Dialog contains a prefab reference editor & display
    /// </summary>
    public interface IPrefabableDialog
    {
        public GameObject PrefabName { get; set; }
        public Text PrefabNameText { get; set; }
        public GameObject CollapsePrefabLabel { get; set; }
        public FunctionButtonStorage CollapsePrefabButton { get; set; }
        public GameObject AssignPrefabLabel { get; set; }
        public FunctionButtonStorage AssignPrefabButton { get; set; }
        public FunctionButtonStorage RemovePrefabButton { get; set; }
    }
}
