using UnityEngine.UI;

using SimpleJSON;
using UnityEngine;
using BetterLegacy.Core.Data;
using BetterLegacy.Core;

namespace BetterLegacy.Menus.UI.Layouts
{
    public class MenuHorizontalLayout : MenuHorizontalOrVerticalLayout
    {
        public HorizontalLayoutGroup horizontalLayout;

        public static MenuHorizontalLayout Parse(JSONNode jn) => new MenuHorizontalLayout
        {
            name = jn["name"],
            childControlHeight = jn["control_height"].AsBool,
            childControlWidth = jn["control_width"].AsBool,
            childForceExpandHeight = jn["force_expand_height"].AsBool,
            childForceExpandWidth = jn["force_expand_width"].AsBool,
            childScaleHeight = jn["scale_height"].AsBool,
            childScaleWidth = jn["scale_width"].AsBool,
            spacing = jn["spacing"].AsFloat,
            childAlignment = (TextAnchor)jn["align"].AsInt,

            rect = RectValues.TryParse(jn["rect"], RectValues.Default),
            mask = jn["mask"].AsBool,
            regenerate = jn["regenerate"] == null || jn["regenerate"].AsBool,

            scrollable = jn["scrollable"].AsBool,
            minScroll = jn["min_scroll"] == null ? -100f : jn["min_scroll"].AsFloat,
            maxScroll = jn["max_scroll"] == null ? 100f : jn["max_scroll"].AsFloat,
        };

        public void Scroll(float value, bool additive)
        {
            var content = this.content.transform.AsRT();

            if (additive)
                value += content.anchoredPosition.x;

            value = Mathf.Clamp(value, minScroll, maxScroll);

            content.anchoredPosition = new Vector2(value, content.anchoredPosition.y);
        }
    }
}
