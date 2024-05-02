using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Components.Editor
{
    /// <summary>
    /// Fallback component in case the current method is too slow.
    /// </summary>
    public class EditorThemeElement : MonoBehaviour
    {
        public string id;

        public EditorThemeManager.Element Element { get; set; }

        public void Init(EditorThemeManager.Element element, string id)
        {
            Element = element;
            Element.ApplyTheme(EditorThemeManager.CurrentTheme);
            this.id = id;
        }

        void OnDestroy()
        {
            if (!string.IsNullOrEmpty(id) && EditorThemeManager.TemporaryEditorGUIElements.ContainsKey(id))
                EditorThemeManager.TemporaryEditorGUIElements.Remove(id);
        }
    }
}
