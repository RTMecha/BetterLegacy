using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Menus.UI.Layouts
{
    /// <summary>
    /// Represents a layout that is horizontal.
    /// </summary>
    public class MenuHorizontalLayout : MenuHorizontalOrVerticalLayout
    {
        /// <summary>
        /// Unity component reference.
        /// </summary>
        public HorizontalLayoutGroup horizontalLayout;

        public static MenuHorizontalLayout Parse(JSONNode jn)
        {
            var layout = new MenuHorizontalLayout();
            layout.Read(jn);
            return layout;
        }

        public override void Scroll(float value, bool additive)
        {
            var content = this.content.transform.AsRT();

            if (additive)
                value += content.anchoredPosition.x;

            value = Mathf.Clamp(value, minScroll, maxScroll);

            content.anchoredPosition = new Vector2(value, content.anchoredPosition.y);
        }
    }
}
