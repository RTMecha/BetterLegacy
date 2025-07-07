using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core;

namespace BetterLegacy.Menus.UI.Layouts
{
    /// <summary>
    /// Represents a layout that is vertical.
    /// </summary>
    public class MenuVerticalLayout : MenuHorizontalOrVerticalLayout
    {
        /// <summary>
        /// Unity component reference.
        /// </summary>
        public VerticalLayoutGroup verticalLayout;

        public static MenuVerticalLayout Parse(JSONNode jn)
        {
            var layout = new MenuVerticalLayout();
            layout.Read(jn);
            return layout;
        }

        public override void Scroll(float value, bool additive)
        {
            var content = this.content.transform.AsRT();

            if (additive)
                value += content.anchoredPosition.y;

            value = Mathf.Clamp(value, minScroll, maxScroll);

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, value);
        }
    }
}
