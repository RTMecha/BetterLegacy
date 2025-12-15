using UnityEngine;

using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for updating a UI elements' editor theme.
    /// </summary>
    public class EditorThemeObject : MonoBehaviour
    {
        [SerializeField]
        /// <summary>
        /// Element reference.
        /// </summary>
        public EditorThemeElement element;

        void OnDestroy()
        {
            element?.Clear();
            element = null;
        }
    }
}
