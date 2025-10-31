using UnityEngine;

using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component that represents a list of editor layer toggles and the editor layer input field.
    /// </summary>
    public class EditorLayerUI : MonoBehaviour
    {
        /// <summary>
        /// Editor layer UI reference.
        /// </summary>
        public IEditorLayerUI editorLayerUI;
        /// <summary>
        /// Size of each toggle.
        /// </summary>
        public float size = 30.5f;
    }
}
