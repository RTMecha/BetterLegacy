using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a UI has a layer value that can be edited via an input field or a toggle list.
    /// </summary>
    public interface IEditorLayerUI
    {
        public InputField EditorLayerField { get; set; }
        public RectTransform EditorLayerTogglesParent { get; set; }
        public Toggle[] EditorLayerToggles { get; set; }
    }
}
