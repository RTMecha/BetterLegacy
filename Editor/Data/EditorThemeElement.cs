using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Holds data for how a UI element should display in the editor. Controls editor theme colors and rounded setting.
    /// </summary>
    public class EditorThemeElement
    {
        public EditorThemeElement(ThemeGroup group, GameObject gameObject, Component[] components, bool canSetRounded = false, int rounded = 0, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W, bool isSelectable = false, float opacity = 1f)
        {
            themeGroup = group;
            this.gameObject = gameObject;
            this.components = components;
            this.canSetRounded = canSetRounded;
            this.rounded = rounded;
            this.roundedSide = roundedSide;
            this.isSelectable = isSelectable;
            this.opacity = opacity;
        }

        [SerializeField]
        public GameObject gameObject;

        [SerializeField]
        public Component[] components;

        [SerializeField]
        public ThemeGroup themeGroup = ThemeGroup.Null;

        [SerializeField]
        public bool isSelectable = false;

        [SerializeField]
        public bool canSetRounded = false;

        [SerializeField]
        public int rounded;

        [SerializeField]
        public SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W;

        [SerializeField]
        public float opacity = 1f;

        public void Clear()
        {
            gameObject = null;
            for (int i = 0; i < components.Length; i++)
                components[i] = null;
            components = null;
        }

        public void ApplyTheme(EditorTheme editorTheme)
        {
            SetRounded();

            try
            {
                if (themeGroup == ThemeGroup.Null)
                    return;

                if (!editorTheme.ColorGroups.TryGetValue(themeGroup, out Color color))
                    return;

                color.a *= opacity;

                if (!isSelectable)
                    EditorThemeManager.SetColor(color, components);
                else if (EditorThemeManager.selectableThemeGroups.TryGetValue(themeGroup, out SelectableThemeGroup selectableThemeGroup))
                    EditorThemeManager.SetColor(color, selectableThemeGroup.ToColorBlock(editorTheme), components);
                else
                    EditorThemeManager.SetColor(color, EditorThemeManager.DefaultColorBlock, components);
            }
            catch
            {

            }
        }

        public void SetRounded() => EditorThemeManager.SetRounded(canSetRounded, rounded, roundedSide, components);

        public override string ToString() => gameObject ? gameObject.name : "null";
    }
}
