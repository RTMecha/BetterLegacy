using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Indicates a UI has a layer value that can be edited via an input field or a toggle list.
    /// </summary>
    public interface IEditorLayerUI
    {
        /// <summary>
        /// Input field for practically unlimited editor layers.
        /// </summary>
        public InputField EditorLayerField { get; set; }
        /// <summary>
        /// Toggles parent.
        /// </summary>
        public RectTransform EditorLayerTogglesParent { get; set; }
        /// <summary>
        /// Array of editor layer toggles.
        /// </summary>
        public Toggle[] EditorLayerToggles { get; set; }
    }
}
