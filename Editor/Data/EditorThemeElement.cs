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
        public EditorThemeElement(ThemeGroup group, GameObject gameObject, List<Component> components, bool canSetRounded = false, int rounded = 0, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W, bool isSelectable = false)
        {
            themeGroup = group;
            this.gameObject = gameObject;
            this.components = components.ToArray(); // replaced List with Array
            this.canSetRounded = canSetRounded;
            this.rounded = rounded;
            this.roundedSide = roundedSide;
            this.isSelectable = isSelectable;
        }

        public EditorThemeElement(ThemeGroup group, GameObject gameObject, Component[] components, bool canSetRounded = false, int rounded = 0, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W, bool isSelectable = false)
        {
            themeGroup = group;
            this.gameObject = gameObject;
            this.components = components;
            this.canSetRounded = canSetRounded;
            this.rounded = rounded;
            this.roundedSide = roundedSide;
            this.isSelectable = isSelectable;
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

        public void Clear()
        {
            gameObject = null;
            for (int i = 0; i < components.Length; i++)
                components[i] = null;
            components = null;
        }

        public void ApplyTheme(EditorTheme theme)
        {
            SetRounded();
            EditorThemeManager.ApplyTheme(theme, themeGroup, isSelectable, components);
        }

        public void SetRounded() => EditorThemeManager.SetRounded(canSetRounded, rounded, roundedSide, components);

        public override string ToString() => gameObject ? gameObject.name : "null";
    }
}
