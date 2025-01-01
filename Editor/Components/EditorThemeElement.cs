using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component used for cases where an elemnent needs to be removed from the Editor Theme element dictionary when the object is destroyed.
    /// </summary>
    public class EditorThemeElement : MonoBehaviour
    {
        /// <summary>
        /// ID reference.
        /// </summary>
        public string id;

        /// <summary>
        /// Element reference.
        /// </summary>
        public EditorThemeManager.Element Element { get; set; }

        /// <summary>
        /// Sets the components' element and applies a theme.
        /// </summary>
        /// <param name="element">Element to assign.</param>
        /// <param name="id">ID reference.</param>
        public void Init(EditorThemeManager.Element element, string id)
        {
            Element = element;
            Element.ApplyTheme(EditorThemeManager.CurrentTheme);
            this.id = id;
        }

        void OnDestroy()
        {
            if (!string.IsNullOrEmpty(id)) // remove from temporary editor gui elements.
                EditorThemeManager.TemporaryEditorGUIElements.Remove(id);
        }
    }
}
