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

        /// <summary>
        /// Game object reference.
        /// </summary>
        [SerializeField]
        public GameObject gameObject;

        /// <summary>
        /// Array of components.
        /// </summary>
        [SerializeField]
        public Component[] components;

        /// <summary>
        /// Theme group that should be applied to the components.
        /// </summary>
        [SerializeField]
        public ThemeGroup themeGroup = ThemeGroup.Null;

        /// <summary>
        /// If the element is a selectable.
        /// </summary>
        [SerializeField]
        public bool isSelectable = false;

        /// <summary>
        /// If the element can be rounded.
        /// </summary>
        [SerializeField]
        public bool canSetRounded = false;

        /// <summary>
        /// Rounded amount to set if <see cref="canSetRounded"/> is set to true.
        /// </summary>
        [SerializeField]
        public int rounded;

        /// <summary>
        /// Rounded side to set if <see cref="canSetRounded"/> is set to true.
        /// </summary>
        [SerializeField]
        public SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W;

        /// <summary>
        /// Opacity of the element.
        /// </summary>
        [SerializeField]
        public float opacity = 1f;

        /// <summary>
        /// Clears the element.
        /// </summary>
        public void Clear()
        {
            gameObject = null;
            for (int i = 0; i < components.Length; i++)
                components[i] = null;
            components = null;
        }

        /// <summary>
        /// Applies an editor theme to the element.
        /// </summary>
        /// <param name="editorTheme">Editor theme to apply.</param>
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

        /// <summary>
        /// Updates the rounded state of the element.
        /// </summary>
        public void SetRounded() => EditorThemeManager.SetRounded(canSetRounded, rounded, roundedSide, components);

        public override string ToString() => gameObject ? gameObject.name : "null";
    }
}
